using HandleGC.UAS;
using HandleGC.UAS.Handles;

unsafe
{
    UpperAddressSpace.Init();
    HandleAllocator.Allocate(&UpperAddressSpace.Globals->hardwareThread1, (byte*)0x011, out bool success);
}