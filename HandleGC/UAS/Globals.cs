using HandleGC.LAS.Allocations;
using HandleGC.UAS.Handles;
using System.Runtime.InteropServices;

namespace HandleGC.UAS;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct Globals
{
    internal HardwareThread hardwareThread1;
    internal HardwareThread hardwareThread2;
    internal ulong globalHeapSize;
    internal ulong allocationHeapSize;
    internal bool allocationHeapLock;

    internal void Init()
    {
        hardwareThread1.handleTableChunk = new HandleTableChunk(UpperAddressSpace.HandleTable, 0);
        HandleTableBitmap.MarkOccupied(0);
        hardwareThread1.allocationRegion = AllocationHeap.TakeFreeRegion(out bool success);

        hardwareThread2.handleTableChunk = new HandleTableChunk(UpperAddressSpace.HandleTable + HandleTableChunk.HandleCount, HandleTableChunk.HandleCount);
        HandleTableBitmap.MarkOccupied(1);
        hardwareThread1.allocationRegion = AllocationHeap.TakeFreeRegion(out success);
    }
}