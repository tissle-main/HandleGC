using HandleGC.LAS;
using HandleGC.UAS.Handles;
using HandleGC.LAS.Allocations;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace HandleGC.UAS;

[StructLayout(LayoutKind.Sequential, Size = 64)]
internal unsafe struct HardwareThread
{
    internal HandleTableChunk handleTableChunk;
    internal AllocationRegion allocationRegion;
    internal bool safePointReached;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ulong* GCNew(uint size, out bool success)
    {
        fixed(AllocationRegion* region = &allocationRegion)
        fixed(HandleTableChunk* chunk = &handleTableChunk)
        {
            return ObjectAllocator.Allocate(region, chunk, size, out success);
        }
    }
}