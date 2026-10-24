using HandleGC.UAS.Handles;
using HandleGC.LAS.Allocations;
using System.Runtime.InteropServices;

namespace HandleGC.UAS;

[StructLayout(LayoutKind.Sequential, Size = 64)]
internal unsafe struct HardwareThread
{
    internal HandleTableChunk handleTableChunk;
    internal AllocationRegion allocationRegion;
    internal bool safePointReached;

    internal static Thread Run(HardwareThread* th)
    {
        Thread thread = new(a =>
        {
            HardwareThread* ht = (HardwareThread*)(ulong)a!;
            while(true)
            {
                Thread.Sleep(1);
                ht->safePointReached = true;
            }
        });
        thread.Start((ulong)th);
        return thread;
    }
}