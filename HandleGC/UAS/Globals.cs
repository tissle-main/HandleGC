using HandleGC.UAS.Handles;
using HandleGC.LAS.Allocations;
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
        hardwareThread2.allocationRegion = AllocationHeap.TakeFreeRegion(out success);
    }
    internal void RunAll()
    {
        fixed(HardwareThread* p1 = &hardwareThread1)
        {
            HardwareThread.RunGCThread(p1);
        }
        fixed(HardwareThread* p2 = &hardwareThread2)
        {
            HardwareThread.RunThread(p2);
        }
    }
    internal void WaitForSafePoints()
    {
        hardwareThread2.safePointReached = false;
        while(!hardwareThread2.safePointReached)
        {
            Thread.SpinWait(10);
        }
    }
    internal readonly bool ContainsHandleTableChunk(uint chunkIndex)
    {
        return hardwareThread1.handleTableChunk.ChunkIndex == chunkIndex || hardwareThread2.handleTableChunk.ChunkIndex == chunkIndex;
    }
}