using HandleGC.LAS;
using HandleGC.UAS;
using HandleGC.LAS.Allocations;
using HandleGC.LAS.GarbageCollection;

LowerAddressSpace.Init();
UpperAddressSpace.Init();
unsafe
{
    for(int index = 0; index < 110; index++)
    {
        ulong* handle = UpperAddressSpace.Globals->hardwareThread1.GCNew(10000, out bool success);
        Console.WriteLine("AllocationRegions: {0}", AllocationHeap.PageCount);
        Console.WriteLine("AllocationCount: {0}", UpperAddressSpace.Globals->hardwareThread1.allocationRegion.regionHeader->allocatedSize);
        if(Random.Shared.NextSingle() > 0.5)
        {
            ObjectAccessor.DeleteObject(handle);
        }
        Thread.Sleep(1);
    }
    GarbageCollector.CollectAllocationHeap();
}


//unsafe
//{
//    Run(&UpperAddressSpace.Globals->hardwareThread1);
//    Run(&UpperAddressSpace.Globals->hardwareThread2);

//    static void Run(HardwareThread* pth)
//    {
//        Thread thread = new(pthObj =>
//        {
//            HardwareThread* pth = (HardwareThread*)(ulong)pthObj;
//            for(int index = 0; true; index++)
//            {
//                long timestamp = Stopwatch.GetTimestamp();
//                uint handleIndex = HandleAllocator.Allocate(pth, (byte*)index, out bool success);
//                Console.WriteLine("HandleIndex: {0}", handleIndex);
//                if(Random.Shared.NextDouble() > 0.25)
//                {
//                    HandleAllocator.Free(handleIndex);
//                }
//            }
//        });
//        thread.Start((ulong)pth);
//    }
//}