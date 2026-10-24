using HandleGC.UAS;
using HandleGC.LAS;
using HandleGC.UAS.Heap;

unsafe
{
    UpperAddressSpace.Init();
    LowerAddressSpace.Init();
    MemoryBlock unmanagedBlock = UnmanagedHeap.Allocate(1);
    UnmanagedHeap.Free(unmanagedBlock);
    ulong* handle = ObjectAllocator.Allocate(&UpperAddressSpace.Globals->hardwareThread1, 64);
    if(handle is null)
        throw new OutOfMemoryException("Unable to allocate the startup smoke-test object.");
    ObjectAccessor.DeleteObject(handle);
}
