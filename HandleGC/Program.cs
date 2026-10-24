using HandleGC.UAS;
using System.Diagnostics;
using HandleGC.UAS.Handles;

UpperAddressSpace.Init();
unsafe
{
    Run(&UpperAddressSpace.Globals->hardwareThread1);
    Run(&UpperAddressSpace.Globals->hardwareThread2);

    static void Run(HardwareThread* pth)
    {
        Thread thread = new(pthObj =>
        {
            HardwareThread* pth = (HardwareThread*)(ulong)pthObj;
            for(int index = 0; true; index++)
            {
                long timestamp = Stopwatch.GetTimestamp();
                uint handleIndex = HandleAllocator.Allocate(pth, (byte*)index, out bool success);
                Console.WriteLine("HandleIndex: {0}", handleIndex);
                if(Random.Shared.NextDouble() > 0.25)
                {
                    HandleAllocator.Free(handleIndex);
                }
            }
        });
        thread.Start((ulong)pth);
    }
}