using HandleGC.UAS.Handles;
using System.Runtime.InteropServices;

namespace HandleGC.UAS;

[StructLayout(LayoutKind.Sequential)]
internal struct HardwareThread
{
    internal HandleTableChunk handleTableChunk;
}