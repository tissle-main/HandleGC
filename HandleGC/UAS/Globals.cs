using HandleGC.UAS.Handles;
using System.Runtime.InteropServices;

namespace HandleGC.UAS;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct Globals
{
    internal HardwareThread hardwareThread1;
    internal HardwareThread hardwareThread2;

    internal void Init()
    {
        hardwareThread1.handleTableChunk = new HandleTableChunk(UpperAddressSpace.HandleTable, 0);
        HandleTableBitmap.MarkOccupied(0);

        hardwareThread2.handleTableChunk = new HandleTableChunk(UpperAddressSpace.HandleTable + HandleTableChunk.HandleCount, HandleTableChunk.HandleCount);
        HandleTableBitmap.MarkOccupied(1);
    }
}