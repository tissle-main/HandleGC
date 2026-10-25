using HandleGC.UAS;
using System.Runtime.CompilerServices;

namespace HandleGC.LAS.Allocations;

/// <summary>
/// Provides way to get free allocation regions
/// </summary>
internal static unsafe class AllocationHeap
{
    internal const ulong AllocationRegionSize = LowerAddressSpace.MemoryPageSize * 250;

    internal static ulong PageCount
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => UpperAddressSpace.Globals->allocationHeapSize / AllocationRegionSize;
    }

    /// <summary>
    /// Finds free region and reserves it. If no free regions available, allocates a new one
    /// </summary>
    /// <param name="success">Contains true if region was reserved successfully. Otherwise, contains false</param>
    /// <returns></returns>
    internal static AllocationRegion TakeFreeRegion(out bool success)
    {
        success = true;
        byte* endAddress = LowerAddressSpace.EndAddress;
        for(ulong index = 1, pageCount = PageCount; index <= pageCount; index++)
        {
            //Finds free region. Reserves it with CAS
            AllocationRegionHeader* header = (AllocationRegionHeader*)(endAddress - index * AllocationRegionSize);
            if(header->regionState is not AllocationRegionState.Free)
            {
                continue;
            }
            if(Interlocked.CompareExchange(ref header->regionState, AllocationRegionState.Active, AllocationRegionState.Free) is AllocationRegionState.Free)
            {
                return new AllocationRegion(header);
            }
        }

        //Other threads can also try to allocate new region, so we using lock to ensure single access.
        WaitLock: while(UpperAddressSpace.Globals->allocationHeapLock)
        {
            Thread.SpinWait(100);
        }
        if(Interlocked.CompareExchange(ref UpperAddressSpace.Globals->allocationHeapLock, true, false))
        {
            goto WaitLock;
        }

        //Allocate new region
        //TODO: make access to the "LowerAddressSpace.FreeSpace" sync
        if(LowerAddressSpace.FreeSpace < AllocationRegionSize)
        {
            UpperAddressSpace.Globals->allocationHeapLock = false;
            success = false;
            return default;
        }
        UpperAddressSpace.Globals->allocationHeapSize += AllocationRegionSize;
        AllocationRegionHeader* newHeader = (AllocationRegionHeader*)(endAddress - UpperAddressSpace.Globals->allocationHeapSize);
        UpperAddressSpace.Globals->allocationHeapLock = false;

        newHeader->regionState = AllocationRegionState.Active;
        newHeader->allocatedSize = AllocationRegionHeader.Size;
        return new AllocationRegion(newHeader);
    }
}