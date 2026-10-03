namespace HandleGC;

public readonly struct VirtualRegion
{
    public nint Start { get; }

    public nuint Size { get; }

    public nint End =>
        Start + checked((nint)Size);

    public VirtualRegion(
        nint start,
        nuint size)
    {
        if(start == 0)
            throw new ArgumentException(
                "Region start cannot be zero.",
                nameof(start));

        if(size == 0)
            throw new ArgumentOutOfRangeException(
                nameof(size));

        Start = start;
        Size = size;
    }

    public bool Contains(nint address)
    {
        return address >= Start &&
               address < End;
    }

    public bool Contains(
        nint address,
        nuint size)
    {
        if(address < Start)
            return false;

        nuint offset =
            checked((nuint)(address - Start));

        return offset <= Size &&
               size <= Size - offset;
    }

    public override string ToString()
    {
        return
            $"[0x{Start:X}, 0x{End:X}) " +
            $"Size={Size}";
    }
}