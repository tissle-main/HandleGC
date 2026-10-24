using HandleGC.UAS.Handles;

namespace HandleGC.LAS;

internal static unsafe class ObjectAccessor
{
    internal static void GetObject(ulong* handle, delegate*<byte*, void> action)
    {
        while(true)
        {
            ulong current = Volatile.Read(ref *handle);
            if(!Handle.GetRSFlag(current))
            {
                action(Handle.GetObjectAddress(current));
                return;
            }

            if(Handle.GetGCLocked(current))
            {
                Thread.SpinWait(1);
                continue;
            }

            ulong usage = Handle.GetUsageCount(current);
            if(usage == 0x7FFF)
                throw new InvalidOperationException("Handle usage count overflow.");

            ulong acquired = Handle.SetUsageCount(current, usage + 1);
            if(Interlocked.CompareExchange(ref *handle, acquired, current) != current)
                continue;

            try { action(Handle.GetObjectAddress(current)); }
            finally { Release(handle); }
            return;
        }
    }

    internal static void DeleteObject(ulong* handle)
    {
        byte* address = Handle.GetObjectAddress(Volatile.Read(ref *handle));
        if(address is null)
            return;
        ObjectHeader* header = (ObjectHeader*)address;
        header->Alive = false;
        HandleAllocator.Free(header->HandleIndex);
    }

    private static void Release(ulong* handle)
    {
        while(true)
        {
            ulong current = Volatile.Read(ref *handle);
            ulong usage = Handle.GetUsageCount(current);
            if(usage == 0)
                throw new InvalidOperationException("Handle usage count underflow.");
            ulong released = Handle.SetUsageCount(current, usage - 1);
            if(Interlocked.CompareExchange(ref *handle, released, current) == current)
                return;
        }
    }
}
