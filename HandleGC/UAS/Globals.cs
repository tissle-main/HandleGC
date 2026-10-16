using System.Runtime.InteropServices;

namespace HandleGC.UAS;

[StructLayout(LayoutKind.Sequential)]
internal struct Globals
{
    internal HardwareThread hardwareThread1;
}