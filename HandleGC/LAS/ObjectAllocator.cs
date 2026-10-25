using HandleGC.UAS;
using HandleGC.UAS.Handles;
using HandleGC.LAS.Allocations;

namespace HandleGC.LAS;

internal static unsafe class ObjectAllocator
{
    internal static ulong* Allocate(AllocationRegion* allocationRegion, HandleTableChunk* handleTableChunk, uint size, out bool success)
    {
        while(true)
        {
            uint allocationSize = allocationRegion->regionHeader->allocatedSize;
            uint objectSize = ObjectHeader.Size + size;
            if(AllocationHeap.AllocationRegionSize - allocationSize >= objectSize)
            {
                byte* objAddress = (byte*)allocationRegion + allocationSize;
                ObjectHeader* header = (ObjectHeader*)objAddress;
                header->Alive = true;
                header->Length = objectSize;
                header->HandleIndex = HandleAllocator.Allocate(handleTableChunk, objAddress, out success);
                if(!success)
                {
                    return null;
                }
                allocationRegion->regionHeader->allocatedSize += objectSize;
                return UpperAddressSpace.HandleTable + header->HandleIndex;
            }

            allocationRegion->regionHeader->regionState = AllocationRegionState.Retired;
            *allocationRegion = AllocationHeap.TakeFreeRegion(out success);
            if(!success)
            {
                throw new OutOfMemoryException("No enough memory to allocate new region");
            }
        }
    }
}