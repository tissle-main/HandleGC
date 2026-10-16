using HandleGC.UAS;
using HandleGC.UAS.Handles;
using HandleGC.LAS.Allocations;

namespace HandleGC.LAS;

internal static unsafe class ObjectAllocator
{
    internal static Handle* Allocate(HardwareThread* thread, uint length)
    {
        AllocationRegion region = thread->allocationRegion;
        if(region.FreeSpace < length + (ulong)sizeof(ObjectHeader))
        {
            region.regionHeader->regionState = AllocationRegionState.Retired;
            thread->allocationRegion = new AllocationRegion(AllocationHeap.FindFirstFreeRegion());
            thread->allocationRegion.regionHeader->regionState = AllocationRegionState.Active;
        }
        byte* address = region.HeadAddress;
        ObjectHeader* header = (ObjectHeader*)address;
        header->Alive = true;
        header->TypeAddress = null;
        header->Length = length + (uint)sizeof(ObjectHeader);
        header->HandleIndex = HandleAllocator.Allocate(thread, address, out bool success);
        if(!success)
        {
            return null;
        }
        region.regionHeader->allocatedSize += length + (uint)sizeof(ObjectHeader);
        return UpperAddressSpace.HandleTable + header->HandleIndex;
    }
}