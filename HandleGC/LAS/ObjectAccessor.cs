using HandleGC.UAS.Handles;
using System.Runtime.CompilerServices;

namespace HandleGC.LAS;

/// <summary>
/// Provides low-level operations for acquiring and deleting objects using raw handles
/// </summary>
/// <remarks>Intended for internal use with unsafe code and direct memory manipulation. Not thread-safe unless used as designed</remarks>
internal static unsafe class ObjectAccessor
{
    /// <summary>
    /// Safely retrieves an address of the object that handle points to with shift to the data part
    /// </summary>
    /// <param name="handle">Handle of the object</param>
    /// <param name="action">Any logic with data part of the object</param>
    /// <remarks>
    /// Method is thread-safe
    /// </remarks>
    internal static void AcquireObject(ulong* handle, delegate*<byte*, void> action)
    {
        ulong handleLocal = *handle;
        if(!Handle.GetRSFlag(handleLocal))
        {
            //"Fast path"
            //If "Require Sync" flag is not set, then GC is not planning to move this object, so we can access it directly
            action(Handle.GetObjectAddress(handleLocal) + ObjectHeader.Size);
        }
        else
        {
            ulong newHandle;
            do
            {
                handleLocal = *handle;
                if(Handle.GetGCLocked(handleLocal))
                {
                    //If "GCLocked" is set, then GC is moving this object, so we need to wait untill it done
                    while(Handle.GetGCLocked(*handle))
                    {
                        Thread.SpinWait(10);
                    }

                    //After moving, fast path is available again
                    action(Handle.GetObjectAddress(*handle) + ObjectHeader.Size);
                    return;
                }

                //If "RSFlag" is set, but "GCLocked" is not - GC is planning to move this object. We need to access it through atomic counter
                newHandle = Handle.SetUsageCount(handleLocal, Handle.GetUsageCount(handleLocal) + 1);
            }
            while(Interlocked.CompareExchange(ref *handle, newHandle, handleLocal) != handleLocal);
            action(Handle.GetObjectAddress(handleLocal) + ObjectHeader.Size);
            do
            {
                //Release through atomic counter
                handleLocal = *handle;
                newHandle = Handle.SetUsageCount(handleLocal, Handle.GetUsageCount(handleLocal) - 1);
            }
            while(Interlocked.CompareExchange(ref *handle, newHandle, handleLocal) != handleLocal);
        }
    }

    /// <summary>
    /// Releases provided handle and marks underlying object as dead
    /// </summary>
    /// <param name="handle">Handle of the object</param>
    /// <remarks>
    /// Method is thread-safe, but handle must have no other mutators by the call of this method
    /// </remarks>
    internal static void DeleteObject(ulong* handle)
    {
        if(!Handle.GetRSFlag(*handle))
        {
            //"Fast path"
            //If "Require Sync" flag is not set, then GC is not planning to move this object, so we can delete it
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
                    //If "GCLocked" is set, then GC is moving this object, so we need to wait untill it done
                    while(Handle.GetGCLocked(*handle))
                    {
                        Thread.SpinWait(10);
                    }

                    //After moving, fast path is available again
                    Delete(handle);
                    return;
                }

                //If "RSFlag" is set, but "GCLocked" is not - GC is planning to move this object. We need to delete it through atomic counter
                newHandle = Handle.SetUsageCount(handleLocal, Handle.GetUsageCount(handleLocal) + 1);
            }
            while(Interlocked.CompareExchange(ref *handle, newHandle, handleLocal) != handleLocal);
            Delete(handle);

            //Release through atomic counter is not required, because handle has already been deleted 
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