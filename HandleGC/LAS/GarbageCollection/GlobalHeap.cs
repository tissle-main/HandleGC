using HandleGC.UAS;

namespace HandleGC.LAS.GarbageCollection;

internal static unsafe class GlobalHeap
{
    internal static byte* HeadAddress
    {
        get => LowerAddressSpace.StartAddress + UpperAddressSpace.Globals->globalHeapSize;
    }

    internal static bool Allocate(ObjectHeader* obj)
    {
        if(LowerAddressSpace.FreeSpace < obj->Length)
        {
            return false;
        }
        GarbageCollector.MoveObject(obj, HeadAddress);
        UpperAddressSpace.Globals->globalHeapSize += obj->Length;
        return true;
    }
}