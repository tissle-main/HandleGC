using HandleGC.LAS;
using HandleGC.UAS;

LowerAddressSpace.Init();
UpperAddressSpace.Init();
unsafe
{
    UpperAddressSpace.Globals->RunAll();
}