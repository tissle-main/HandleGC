using HandleGC.UAS;
using HandleGC.UAS.Handles;
using HandleGC.LAS.Allocations;

namespace HandleGC.LAS.GarbageCollection;

internal static unsafe class GarbageCollector
{
    internal static void MoveObject(ObjectHeader* obj, byte* destination)
    {
        Buffer.MemoryCopy(obj, destination, obj->Length, obj->Length);
        obj = (ObjectHeader*)destination;
        HandleTable.SetObjectAddress(obj->HandleIndex, destination);
    }
    internal static void MoveObjectSync(ObjectHeader* obj, byte* destination)
    {
        uint handleIndex = obj->HandleIndex;
        HandleTable.SetRSFlag(handleIndex, true);
        UpperAddressSpace.Globals->hardwareThread1.safePointReached = false;
        UpperAddressSpace.Globals->hardwareThread2.safePointReached = false;
        while(!UpperAddressSpace.Globals->hardwareThread1.safePointReached) ;
        while(!UpperAddressSpace.Globals->hardwareThread2.safePointReached) ;
        HandleTable.SetGCLocked(handleIndex, true);
        while(HandleTable.GetUsageCount(handleIndex) > 0)
            Thread.SpinWait(1);
        MoveObject(obj, destination);
        HandleTable.SetGCLocked(handleIndex, false);
        HandleTable.SetRSFlag(handleIndex, false);
    }
    internal static void CollectGlobalHeap()
    {
        byte* address = LowerAddressSpace.StartAddress;
        byte* endAddress = GlobalHeap.HeadAddress;
        byte* allocationAddress = address;
        while(address < endAddress)
        {
            ObjectHeader* header = (ObjectHeader*)address;
            if(allocationAddress != address && header->Alive)
            {
                MoveObjectSync(header, allocationAddress);
            }
            if(header->Alive)
            {
                allocationAddress += header->Length;
            }
            address += header->Length;
        }
        UpperAddressSpace.Globals->globalHeapSize = (ulong)(allocationAddress - LowerAddressSpace.StartAddress);
    }
    internal static bool CollectAllocationRegion(AllocationRegionHeader* region)
    {
        byte* address = (byte*)region + sizeof(AllocationRegionHeader);
        byte* endAddress = address + region->allocatedSize;
        while(address < endAddress)
        {
            ObjectHeader* header = (ObjectHeader*)address;
            if(header->Alive && !GlobalHeap.Allocate(header))
            {
                return false;
            }
            address += header->Length;
        }
        region->regionState = AllocationRegionState.Free;
        return true;
    }
    internal static void CollectHandleTable()
    {
        uint chunkEndIndex = (uint)HandleTableBitmap.ChunkCount;
        for(uint chunkIndex = 0; chunkIndex < chunkEndIndex; chunkIndex++)
        {
            if(UpperAddressSpace.Globals->hardwareThread1.handleTableChunk.ChunkIndex == chunkIndex)
            {
                continue;
            }
            uint baseCellIndex = chunkIndex * HandleTableChunk.HandleCount;
            HandleTableChunk chunk = new(UpperAddressSpace.HandleTable + baseCellIndex, baseCellIndex);
            if(chunk.Scan())
            {
                HandleTableBitmap.MarkFree(chunkIndex);
            }
        }
    }
}
