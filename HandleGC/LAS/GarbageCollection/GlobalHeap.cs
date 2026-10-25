using HandleGC.UAS;
using System.Runtime.CompilerServices;

namespace HandleGC.LAS.GarbageCollection;

internal static unsafe class GlobalHeap
{
    internal static byte* TopAddress
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => LowerAddressSpace.StartAddress + UpperAddressSpace.Globals->globalHeapSize;
    }
}