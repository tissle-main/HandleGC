using HandleGC.UAS.Handles;
using HandleGC.LAS.Allocations;
using System.Runtime.InteropServices;

namespace HandleGC.UAS;

[StructLayout(LayoutKind.Sequential, Size = 64)]
internal struct HardwareThread
{
    internal HandleTableChunk handleTableChunk;
    internal AllocationRegion allocationRegion;
    internal bool safePointReached;
}