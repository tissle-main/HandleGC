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
        UpperAddressSpace.HandleTable[obj->HandleIndex].Address = destination;
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
                MoveObject(header, allocationAddress);
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
        Handle* address = UpperAddressSpace.HandleTable;
        for(uint chunkIndex = 0; chunkIndex < chunkEndIndex; chunkIndex++)
        {
            if(UpperAddressSpace.Globals->hardwareThread1.handleTableChunk.Index == chunkIndex)
            {
                continue;
            }
            uint baseCellIndex = chunkIndex * HandleTableChunk.CellCount;
            HandleTableChunk chunk = new(address + baseCellIndex, baseCellIndex);         
            if(chunk.Scan())
            {
                HandleTableBitmap.MarkFree(chunkIndex);
            }
        }
    }
}