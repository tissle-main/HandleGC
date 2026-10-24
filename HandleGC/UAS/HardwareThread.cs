using HandleGC.UAS.Handles;
using System.Runtime.InteropServices;

namespace HandleGC.UAS;

[StructLayout(LayoutKind.Sequential, Size = 64)]
internal unsafe struct HardwareThread
{
    internal HandleTableChunk handleTableChunk;
}