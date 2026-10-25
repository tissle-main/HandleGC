using HandleGC.UAS;
using System.Numerics;
using HandleGC.UAS.Handles;
using HandleGC.LAS.Allocations;

namespace HandleGC.LAS.GarbageCollection;

internal static unsafe class GarbageCollector
{
    internal const uint MarkObjectCount = 5;

    internal static void CollectGlobalHeap()
    {
        byte* startAddress = LowerAddressSpace.StartAddress;
        byte* endAddress = GlobalHeap.TopAddress;
        byte* allocationAddress = startAddress;
        while(allocationAddress == startAddress && startAddress < endAddress)
        {
            ObjectHeader* header = (ObjectHeader*)startAddress;
            startAddress += header->Length;
            if(header->Alive)
            {
                allocationAddress = startAddress;
            }
        }   
        while(startAddress < endAddress)
        {
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
            UpperAddressSpace.Globals->WaitForSafePoints();
            while(startAddress < endAddress)
            {
                ObjectHeader* header = (ObjectHeader*)startAddress;
                uint length = header->Length;      
                if(!header->Alive)
                {
                    startAddress += length;
                    continue;
                }

                ulong* handle = UpperAddressSpace.HandleTable + header->HandleIndex;
                if(!Handle.GetRSFlag(*handle))
                {
                    break;
                }
                startAddress += length;

                *handle = Handle.SetGCLocked(*handle, true);
                while(Handle.GetUsageCount(*handle) > 0)
                {
                    Thread.SpinWait(10);
                }

                Buffer.MemoryCopy(header, allocationAddress, length, length);
                *handle = Handle.SetObjectAddress(0, allocationAddress);

                allocationAddress += length;
            }
        }
        UpperAddressSpace.Globals->globalHeapSize = (ulong)(allocationAddress - startAddress);
    }
    internal static void CollectAllocationRegion(AllocationRegionHeader* allocationRegion)
    {
        byte* startAddress = (byte*)allocationRegion + AllocationRegionHeader.Size;
        byte* endAddress = startAddress + allocationRegion->allocatedSize - AllocationRegionHeader.Size;
        byte* allocationAddress = GlobalHeap.TopAddress;
        while(startAddress < endAddress)
        {
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
            UpperAddressSpace.Globals->WaitForSafePoints();
            while(startAddress < endAddress)
            {
                ObjectHeader* header = (ObjectHeader*)startAddress;
                uint length = header->Length;
                if(!header->Alive)
                {
                    startAddress += length;
                    continue;
                }

                ulong* handle = UpperAddressSpace.HandleTable + header->HandleIndex;
                if(!Handle.GetRSFlag(*handle))
                {
                    break;
                }
                startAddress += length;

                *handle = Handle.SetGCLocked(*handle, true);
                while(Handle.GetUsageCount(*handle) > 0)
                {
                    Thread.SpinWait(10);
                }

                Buffer.MemoryCopy(header, allocationAddress, length, length);
                *handle = Handle.SetObjectAddress(0, allocationAddress);

                allocationAddress += length;
                UpperAddressSpace.Globals->globalHeapSize += length;
            }
        }
        allocationRegion->regionState = AllocationRegionState.Free;
        allocationRegion->allocatedSize = AllocationRegionHeader.Size;
        if(LowerAddressSpace.EndAddress - AllocationHeap.PageCount * AllocationHeap.AllocationRegionSize != allocationRegion)
        {
            return;
        }

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
    internal static void CollectHandleTable()
    {
        ulong* words = (ulong*)UpperAddressSpace.HandleTableBitmap;
        for(uint wordIndex = 0; wordIndex < HandleTableBitmap.WordCount; wordIndex++)
        {
            ulong word = words[wordIndex];
            if(word == ulong.MaxValue)
            {
                continue;
            }

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