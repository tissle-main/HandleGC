using HandleGC.UAS;
using HandleGC.Imports;
using System.Runtime.InteropServices;

namespace HandleGC.LAS;

internal static unsafe class LowerAddressSpace
{
    internal const ulong MemoryPageSize = 4096;
    internal const ulong Size = MemoryPageSize * 25000;

    internal static byte* StartAddress { get; private set; }
    internal static byte* EndAddress
    {
        get => StartAddress + Size;
    }
    internal static ulong FreeSpace
    {
        get => Size - UpperAddressSpace.Globals->globalHeapSize - UpperAddressSpace.Globals->allocationHeapSize;
    }

    internal static void Init()
    {
        StartAddress = WinImports.ReserveAndCommit(Size);
        NativeMemory.Clear(StartAddress, (nuint)Size);
    }
}