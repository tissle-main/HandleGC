using System.Runtime.InteropServices;

namespace HandleGC.LAS.Allocations;

[StructLayout(LayoutKind.Sequential)]
internal struct AllocationRegionHeader
{
    #region Static
    internal const uint Size = 8;
    #endregion

    #region Instance
    internal AllocationRegionState regionState;
    internal uint allocatedSize;
    #endregion
}