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

    internal void Init()
    {
        hardwareThread1.handleTableChunk = new(UpperAddressSpace.HandleTable, 0);
        hardwareThread2.handleTableChunk = new(UpperAddressSpace.HandleTable + HandleTableChunk.HandleCount, HandleTableChunk.HandleCount);
    }
}