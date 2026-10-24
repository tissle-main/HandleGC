using HandleGC.UAS;

namespace HandleGC.LAS.Allocations;

internal static unsafe class AllocationHeap
{
    internal const uint RegionSize = (uint)LowerAddressSpace.MemoryPageSize * 250;

    internal static ulong RegionCount
    {
        get => UpperAddressSpace.Globals->allocationHeapSize / RegionSize;
    }

    private static readonly Lock Sync = new();

    private static AllocationRegionHeader* Allocate()
    {
        if(LowerAddressSpace.FreeSpace < RegionSize)
        {
            return null;
        }
        UpperAddressSpace.Globals->allocationHeapSize += RegionSize;
        var header = (AllocationRegionHeader*)(LowerAddressSpace.EndAddress - UpperAddressSpace.Globals->allocationHeapSize);
        header->regionState = AllocationRegionState.Free;
        header->allocatedSize = 0;
        return header;
     }
    internal static AllocationRegionHeader* RentRegion()
    {
        lock(Sync)
        {
            byte* regionAddress = LowerAddressSpace.EndAddress;
            for(ulong regionIndex = 1, regionCount = RegionCount; regionIndex <= regionCount; regionIndex++)
            {
                AllocationRegionHeader* header = (AllocationRegionHeader*)(regionAddress - RegionSize * regionIndex);
                if(header->regionState is AllocationRegionState.Free)
                {
                    header->regionState = AllocationRegionState.Active;
                    header->allocatedSize = 0;
                    return header;
                }
            }

            AllocationRegionHeader* allocated = Allocate();
            if(allocated is not null)
                allocated->regionState = AllocationRegionState.Active;
            return allocated;
        }
    }
}
