using System.Runtime.InteropServices;

namespace HandleGC.UAS.Heap;

[StructLayout(LayoutKind.Sequential)]
internal unsafe readonly struct MemoryBlock(byte* thisAddress)
{
    internal byte* Address
    {
        get => thisAddress;
    }
    internal MemoryBlockHeader* Header
    {
        get => (MemoryBlockHeader*)thisAddress;
    }
    internal byte* Data
    {
        get => thisAddress + MemoryBlockHeader.Size;
    }
}