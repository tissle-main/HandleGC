using HandleGC.UAS;
using HandleGC.UAS.Handles;

namespace HandleGC.LAS;

internal static unsafe class ObjectAllocator
{
    internal static byte* Allocate(HardwareThread* thread, uint length)
    {
        AllocationRegion region = thread->allocationRegion;
        if(region.FreeSpace < length + (ulong)sizeof(ObjectHeader))
        {
            region.regionHeader->regionState = AllocationRegionState.Retired;
            thread->allocationRegion = new AllocationRegion(AllocationHeap.FindFirstFreeRegion());
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
        return address;
    }
}