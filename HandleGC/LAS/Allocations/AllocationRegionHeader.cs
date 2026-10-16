using System.Runtime.InteropServices;

namespace HandleGC.LAS;

[StructLayout(LayoutKind.Sequential)]
internal struct AllocationRegionHeader
{
    public AllocationRegionState regionState;
    public uint allocatedSize;
}