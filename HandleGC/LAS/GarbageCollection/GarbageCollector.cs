using HandleGC.UAS;
using System.Numerics;
using HandleGC.UAS.Handles;
using HandleGC.LAS.Allocations;

namespace HandleGC.LAS.GarbageCollection;

/// <summary>
/// Provide methods to clear heaps and handle table
/// </summary>
internal static unsafe class GarbageCollector
{
    internal const uint MarkObjectCount = 5;

    /// <summary>
    /// Clears global heap by placing all alive objects sequentially from left
    /// </summary>
    internal static void CollectGlobalHeap()
    {
        byte* startAddress = LowerAddressSpace.StartAddress;
        byte* endAddress = GlobalHeap.TopAddress;
        byte* allocationAddress = startAddress;
        while(allocationAddress == startAddress && startAddress < endAddress)
        {
            //Skip alive objects from start of the heap
            ObjectHeader* header = (ObjectHeader*)startAddress;
            startAddress += header->Length;
            if(header->Alive)
            {
                allocationAddress = startAddress;
            }
        }   
        while(startAddress < endAddress)
        {
            //Set "RSFlag" to a few forward objects, forcing other threads to access objects through atomic counter
            uint markObject = 0;
            byte* markAddress = startAddress;
            while(markObject < MarkObjectCount && markAddress < endAddress)
            {
                ObjectHeader* header = (ObjectHeader*)markAddress;
                if(header->Alive)
                {
                    HandleTable.SetRSFlag(header->HandleIndex, true);
                    markObject += 1;
                }
                markAddress += header->Length;
            }

            //Wait for threads to reach safe points, ensuring that no threads contain direct address of the object and using slow path
            UpperAddressSpace.Globals->WaitForSafePoints();
            while(startAddress < endAddress)
            {
                ObjectHeader* header = (ObjectHeader*)startAddress;
                uint length = header->Length;      
                if(!header->Alive)
                {
                    //Skip the object if it has already been deleted
                    startAddress += length;
                    continue;
                }

                ulong* handle = UpperAddressSpace.HandleTable + header->HandleIndex;
                if(!Handle.GetRSFlag(*handle))
                {
                    //If object is not marked with "RSFlag", we stop moving and mark again another group of the objects with "RSFlag"
                    break;
                }
                startAddress += length;

                //Lock the object with "GCLocked" flag. New threads can`t acquire the object no more. Waiting for other threads to finish work
                *handle = Handle.SetGCLocked(*handle, true);
                while(Handle.GetUsageCount(*handle) > 0)
                {
                    Thread.SpinWait(10);
                }

                //No mutators here. Can safely move the object
                Buffer.MemoryCopy(header, allocationAddress, length, length);

                //Assign new address to the handle and reset all flags, opening the fast path
                *handle = Handle.SetObjectAddress(0, allocationAddress);

                allocationAddress += length;
            }
        }
        UpperAddressSpace.Globals->globalHeapSize = (ulong)(allocationAddress - startAddress);
    }

    /// <summary>
    /// Frees allocation region by moving all alive objects to the global heap
    /// </summary>
    /// <param name="allocationRegion">Retired allocation region</param>
    internal static void CollectAllocationRegion(AllocationRegionHeader* allocationRegion)
    {
        byte* startAddress = (byte*)allocationRegion + AllocationRegionHeader.Size;
        byte* endAddress = startAddress + allocationRegion->allocatedSize - AllocationRegionHeader.Size;
        byte* allocationAddress = GlobalHeap.TopAddress;
        while(startAddress < endAddress)
        {
            //Set "RSFlag" to a few forward objects, forcing other threads to access objects through atomic counter
            uint markObject = 0;
            byte* markAddress = startAddress;
            while(markObject < MarkObjectCount && markAddress < endAddress)
            {
                ObjectHeader* header = (ObjectHeader*)markAddress;
                if(header->Alive)
                {
                    HandleTable.SetRSFlag(header->HandleIndex, true);
                    markObject += 1;
                }
                markAddress += header->Length;
            }
            
            //Wait for threads to reach safe points, ensuring that no threads contain direct address of the object and using slow path
            UpperAddressSpace.Globals->WaitForSafePoints();
            while(startAddress < endAddress)
            {
                ObjectHeader* header = (ObjectHeader*)startAddress;
                uint length = header->Length;
                if(!header->Alive)
                {
                    //Skip the object if it has already been deleted
                    startAddress += length;
                    continue;
                }

                ulong* handle = UpperAddressSpace.HandleTable + header->HandleIndex;
                if(!Handle.GetRSFlag(*handle))
                {
                    //If object is not marked with "RSFlag", we stop moving and mark again another group of the objects with "RSFlag"
                    break;
                }
                startAddress += length;

                //Lock the object with "GCLocked" flag. New threads can`t acquire the object no more. Waiting for other threads to finish work
                *handle = Handle.SetGCLocked(*handle, true);
                while(Handle.GetUsageCount(*handle) > 0)
                {
                    Thread.SpinWait(10);
                }

                //No mutators here. Can safely move the object
                Buffer.MemoryCopy(header, allocationAddress, length, length);

                //Assign new address to the handle and reset all flags, opening the fast path
                *handle = Handle.SetObjectAddress(0, allocationAddress);

                allocationAddress += length;

                //TODO: provide some way to sync access to the "LowerAddressSpace.FreeSpace"
                UpperAddressSpace.Globals->globalHeapSize += length;
            }
        }

        //Mark region as free
        allocationRegion->regionState = AllocationRegionState.Free;
        allocationRegion->allocatedSize = AllocationRegionHeader.Size;
        if(LowerAddressSpace.EndAddress - AllocationHeap.PageCount * AllocationHeap.AllocationRegionSize != allocationRegion)
        {
            //If region is not the last in the heap, do nothing
            return;
        }

        //If region is the last in the heap, remove it with the lock
        WaitLock: while(UpperAddressSpace.Globals->allocationHeapLock)
        {
            Thread.SpinWait(100);
        }
        if(Interlocked.CompareExchange(ref UpperAddressSpace.Globals->allocationHeapLock, true, false))
        {
            goto WaitLock;
        }      
        if(LowerAddressSpace.EndAddress - AllocationHeap.PageCount * AllocationHeap.AllocationRegionSize == allocationRegion)
        {
            UpperAddressSpace.Globals->allocationHeapSize -= AllocationHeap.AllocationRegionSize;
        }
        UpperAddressSpace.Globals->allocationHeapLock = false;
    }

    /// <summary>
    /// Collect all retired allocation regions
    /// </summary>
    internal static void CollectAllocationHeap()
    {
        byte* endAddress = LowerAddressSpace.EndAddress;
        for(ulong pageIndex = 1, pageCount = AllocationHeap.PageCount; pageIndex <= pageCount; pageIndex++)
        {
            AllocationRegionHeader* header = (AllocationRegionHeader*)(endAddress - pageIndex * AllocationHeap.AllocationRegionSize);
            if(header->regionState is AllocationRegionState.Retired)
            {
                CollectAllocationRegion(header);
            }
        }
    }

    /// <summary>
    /// Sync handle table bitmap with handle table state
    /// </summary>
    internal static void CollectHandleTable()
    {
        ulong* words = (ulong*)UpperAddressSpace.HandleTableBitmap;
        for(uint wordIndex = 0; wordIndex < HandleTableBitmap.WordCount; wordIndex++)
        {
            //TODO: finish this method
            ulong word = words[wordIndex];
            if(word == ulong.MaxValue)
            {
                continue;
            }

            //Get full chunk. Scan it. If chunk have free handles and not belong to any threads, mark it as free
            uint localChunkIndex = (uint)BitOperations.TrailingZeroCount(~word);
            uint globalChunkIndex = wordIndex * HandleTableBitmap.ChunksPerWord + localChunkIndex;
            uint globalHandleIndex = globalChunkIndex * HandleTableChunk.HandleCount;
            HandleTableChunk chunk = new(UpperAddressSpace.HandleTable + globalHandleIndex, globalHandleIndex);
            if(UpperAddressSpace.Globals->ContainsHandleTableChunk(globalChunkIndex))
            {
                continue;
            }
            if(chunk.Scan())
            {
                HandleTableBitmap.MarkFree(globalChunkIndex);
            }
        }
    }
}