using HandleGC.UAS.Handles;
using System.Runtime.InteropServices;

namespace HandleGC.UAS;

internal static unsafe class UpperAddressSpace
{
    internal const ulong MemoryPageSize = 4096;
    internal const ulong Size = MemoryPageSize * 25000;
    internal const ulong GlobalsSize = MemoryPageSize * 1;
    internal const ulong HandleTableBitmapSize = MemoryPageSize * 1;
    internal const ulong HandleTableSize = HandleTableBitmapSize * MemoryPageSize;
    internal const ulong UnmanagedHeapSize = Size - GlobalsSize - HandleTableBitmapSize - HandleTableSize;

    internal static byte* StartAddress { get; private set; }
    internal static Globals* Globals { get; private set; }
    internal static byte* HandleTableBitmap { get; private set; }
    internal static Handle* HandleTable { get; private set; }
    internal static byte* UnmanagedHeap { get; private set; }

    internal static void Init()
    {
        //StartAddress = 
        Globals = (Globals*)StartAddress;
        HandleTableBitmap = (byte*)Globals + GlobalsSize;
        HandleTable = (Handle*)(HandleTableBitmap + HandleTableBitmapSize);
        UnmanagedHeap = (byte*)HandleTable + HandleTableSize;

        NativeMemory.Clear(StartAddress, (nuint)Size);
        NativeMemory.Fill(HandleTableBitmap, (nuint)HandleTableBitmapSize, byte.MaxValue);
    }
}