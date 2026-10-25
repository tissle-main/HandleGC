using HandleGC.UAS.Handles;
using System.Runtime.CompilerServices;

namespace HandleGC.LAS;

internal static unsafe class ObjectAccessor
{
    internal static void AcquireObject(ulong* handle, delegate*<byte*, void> action)
    {
        ulong handleLocal = *handle;
        if(!Handle.GetRSFlag(handleLocal))
        {
            action(Handle.GetObjectAddress(handleLocal));
        }
        else
        {
            ulong newHandle;
            do
            {
                handleLocal = *handle;
                if(Handle.GetGCLocked(handleLocal))
                {
                    while(Handle.GetGCLocked(*handle))
                    {
                        Thread.SpinWait(10);
                    }
                    action(Handle.GetObjectAddress(*handle));
                    return;
                }
                newHandle = Handle.SetUsageCount(handleLocal, Handle.GetUsageCount(handleLocal) + 1);
            }
            while(Interlocked.CompareExchange(ref *handle, newHandle, handleLocal) != handleLocal);
            action(Handle.GetObjectAddress(handleLocal));
            do
            {
                handleLocal = *handle;
                newHandle = Handle.SetUsageCount(handleLocal, Handle.GetUsageCount(handleLocal) - 1);
            }
            while(Interlocked.CompareExchange(ref *handle, newHandle, handleLocal) != handleLocal);
        }
    }
    internal static void DeleteObject(ulong* handle)
    {
        if(!Handle.GetRSFlag(*handle))
        {
            Delete(handle);
        }
        else
        {
            ulong newHandle;
            ulong handleLocal;
            do
            {
                handleLocal = *handle;
                if(Handle.GetGCLocked(handleLocal))
                {
                    while(Handle.GetGCLocked(*handle))
                    {
                        Thread.SpinWait(10);
                    }
                    Delete(handle);
                    return;
                }
                newHandle = Handle.SetUsageCount(handleLocal, Handle.GetUsageCount(handleLocal) + 1);
            }
            while(Interlocked.CompareExchange(ref *handle, newHandle, handleLocal) != handleLocal);
            Delete(handle);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Delete(ulong* handle)
        {
            ObjectHeader* header = (ObjectHeader*)Handle.GetObjectAddress(*handle);
            header->Alive = false;
            *handle = 0;
        }
    }
}