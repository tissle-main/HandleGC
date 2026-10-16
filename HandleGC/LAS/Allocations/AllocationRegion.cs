using System.Runtime.InteropServices;

namespace HandleGC.LAS.Allocations;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct AllocationRegion(AllocationRegionHeader* regionHeader)
{
    internal AllocationRegionHeader* regionHeader = regionHeader;

    internal readonly ulong FreeSpace
    {
        get => AllocationHeap.RegionSize - regionHeader->allocatedSize - (ulong)sizeof(AllocationRegion);
    }
    internal readonly byte* HeadAddress
    {
        get => (byte*)regionHeader + sizeof(AllocationRegionHeader) + regionHeader->allocatedSize;
    }
}