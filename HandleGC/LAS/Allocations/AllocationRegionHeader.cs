using System.Runtime.InteropServices;

namespace HandleGC.LAS.Allocations;

[StructLayout(LayoutKind.Sequential)]
internal struct AllocationRegionHeader
{
    public AllocationRegionState regionState;
    public uint allocatedSize;
}