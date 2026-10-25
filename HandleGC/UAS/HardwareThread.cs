using HandleGC.LAS;
using HandleGC.UAS.Handles;
using HandleGC.LAS.Allocations;
using System.Runtime.InteropServices;
using HandleGC.LAS.GarbageCollection;
using System.Runtime.CompilerServices;

namespace HandleGC.UAS;

[StructLayout(LayoutKind.Sequential, Size = 64)]
internal unsafe struct HardwareThread
{
    internal HandleTableChunk handleTableChunk;
    internal AllocationRegion allocationRegion;
    internal bool safePointReached;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal ulong* GCNew(uint size, out bool success)
    {
        fixed(AllocationRegion* region = &allocationRegion)
        fixed(HandleTableChunk* chunk = &handleTableChunk)
        {
            return ObjectAllocator.Allocate(region, chunk, size, out success);
        }
    }
    internal static void RunThread(HardwareThread* hthread)
    {
        Thread th = new(obj =>
        {
            HardwareThread* hthread = (HardwareThread*)(ulong)obj;

            while(true)
            {
                ulong* handle = hthread->GCNew((uint)Random.Shared.Next(0, 1024), out bool success);
                if(Random.Shared.NextDouble() > 0.5)
                {
                    ObjectAccessor.AcquireObject(handle, &ObjectAction);
                }
                if(Random.Shared.NextDouble() > 0.5)
                {
                    ObjectAccessor.DeleteObject(handle);
                }
                hthread->safePointReached = true;
            }
        });
        th.Start((ulong)hthread);

        static void ObjectAction(byte* obj)
        {

        }
    }
    internal static void RunGCThread(HardwareThread* hthread)
    {
        Thread th = new(obj =>
        {
            HardwareThread* hthread = (HardwareThread*)(ulong)obj;

            for(int index = 1; true; index++)
            {
                ulong* handle = hthread->GCNew((uint)Random.Shared.Next(0, 1024), out bool success);
                if(Random.Shared.NextDouble() > 0.5)
                {
                    ObjectAccessor.AcquireObject(handle, &ObjectAction);
                }
                if(Random.Shared.NextDouble() > 0.5)
                {
                    ObjectAccessor.DeleteObject(handle);
                }
                if(index % 1000 == 0)
                {
                    GarbageCollector.CollectAllocationHeap();
                }
                if(index % 2500 == 0)
                {
                    GarbageCollector.CollectHandleTable();
                }
                if(index % 5000 == 0)
                {
                    GarbageCollector.CollectGlobalHeap();
                }
                if(index >= 5000)
                {
                    index = 1;
                }
                if(index % 10 == 0)
                {
                    Console.WriteLine("GlobalHeapSize: {0}", UpperAddressSpace.Globals->globalHeapSize);
                    Console.WriteLine("AllocationRegions: {0}", AllocationHeap.PageCount);
                }

            }
        });
        th.Start((ulong)hthread);

        static void ObjectAction(byte* obj)
        {

        }
    }
}