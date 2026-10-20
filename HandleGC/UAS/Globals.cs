using System.Runtime.InteropServices;

namespace HandleGC.UAS;

[StructLayout(LayoutKind.Sequential)]
internal struct Globals
{
    internal ulong globalHeapSize;
    internal ulong allocationHeapSize;
    internal HardwareThread hardwareThread1;
    internal HardwareThread hardwareThread2;
}