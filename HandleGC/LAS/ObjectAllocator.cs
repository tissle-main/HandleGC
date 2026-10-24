using HandleGC.UAS;
using HandleGC.UAS.Handles;
using HandleGC.LAS.Allocations;

namespace HandleGC.LAS;

internal static unsafe class ObjectAllocator
{
    internal static ulong* Allocate(HardwareThread* thread, uint length)
    {
        AllocationRegion region = thread->allocationRegion;
        uint allocationSize = checked(length + (uint)sizeof(ObjectHeader));
        if(region.regionHeader is null || region.FreeSpace < allocationSize)
        {
            if(region.regionHeader is not null)
                region.regionHeader->regionState = AllocationRegionState.Retired;

            AllocationRegionHeader* regionHeader = AllocationHeap.RentRegion();
            if(regionHeader is null)
                return null;
            region = new AllocationRegion(regionHeader);
            thread->allocationRegion = region;
        }
        byte* address = region.HeadAddress;
        ObjectHeader* header = (ObjectHeader*)address;
        header->Alive = true;
        header->TypeAddress = null;
        header->Length = allocationSize;
        header->HandleIndex = HandleAllocator.Allocate(thread, address, out bool success);
        if(!success)
        {
            return null;
        }
        region.regionHeader->allocatedSize += allocationSize;
        return UpperAddressSpace.HandleTable + header->HandleIndex;
    }
}
