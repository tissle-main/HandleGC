using System.Runtime.InteropServices;

namespace HandleGC.LAS.Allocations;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct AllocationRegion(AllocationRegionHeader* regionHeader)
{
    internal AllocationRegionHeader* regionHeader = regionHeader;
}