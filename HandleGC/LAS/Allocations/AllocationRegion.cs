using System.Text;
using System.Runtime.InteropServices;

namespace HandleGC.LAS.Allocations;

[StructLayout(LayoutKind.Sequential)]
internal unsafe struct AllocationRegion(AllocationRegionHeader* regionHeader)
{
    #region Instance
    internal AllocationRegionHeader* regionHeader = regionHeader;
    #endregion

    #region Base
    public override string ToString()
    {
        StringBuilder builder = new($"[{regionHeader->regionState}|{regionHeader->allocatedSize}]\r\n");
        byte* startAddress = (byte*)regionHeader + AllocationRegionHeader.Size;
        byte* endAddress = startAddress + regionHeader->allocatedSize - AllocationRegionHeader.Size;
        while(startAddress < endAddress)
        {
            ObjectHeader* header = (ObjectHeader*)startAddress;
            startAddress += header->Length;

            builder.AppendLine(header->ToString());
        }
        return builder.ToString();
    }
    #endregion
}