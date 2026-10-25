using HandleGC.UAS;
using HandleGC.Imports;
using System.Runtime.CompilerServices;

namespace HandleGC.LAS;

internal static unsafe class LowerAddressSpace
{
    internal const ulong MemoryPageSize = 4096;
    internal const ulong Size = MemoryPageSize * 25000;
    internal const uint ObjectHeaderSize = 16;

    internal static byte* StartAddress { get; private set; }
    internal static ulong FreeSpace
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Globals* globals = UpperAddressSpace.Globals;
            return Size - globals->globalHeapSize - globals->allocationHeapSize;
        }
    }
    internal static byte* EndAddress
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => StartAddress + Size;
    }

    internal static void Init()
    {
        StartAddress = WinImports.ReserveAndCommit(Size);
    }
}