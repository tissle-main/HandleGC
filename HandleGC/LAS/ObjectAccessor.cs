using HandleGC.UAS.Handles;

namespace HandleGC.LAS;

internal static unsafe class ObjectAccessor
{
    internal static void GetObject(Handle* handle, delegate*<byte*, void> action)
    {
        if(!handle->RSFlag)
        {
            action(handle->Address);
        }
        else
        {
            while(handle->GCLocked) ;
            if(!handle->RSFlag)
            {
                action(handle->Address);
            }
            else
            {
                Handle expected;
                Handle origin;
                do
                {
                    expected = *handle;
                    Handle changed = expected;
                    changed.UsageCount++;
                    origin = Interlocked.CompareExchange(ref *handle, changed, expected);
                }
                while(expected.RawValue != origin.RawValue);                
                action(handle->Address);
                do
                {
                    expected = *handle;
                    Handle changed = expected;
                    changed.UsageCount--;
                    origin = Interlocked.CompareExchange(ref *handle, changed, expected);
                }
                while(expected.RawValue != origin.RawValue);
            }
        }
    }
    internal static void DeleteObject(Handle* handle)
    {
        ObjectHeader* header = (ObjectHeader*)handle->Address;
        header->Alive = false;
        HandleAllocator.Free(header->HandleIndex); 
    }
}