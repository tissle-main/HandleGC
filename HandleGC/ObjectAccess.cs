namespace HandleGC;

using System.Runtime.CompilerServices;

public unsafe ref struct ObjectAccess
{
    private readonly HandleEntryRef _entry;
    private readonly nint _address;

    private bool _released;

    internal ObjectAccess(
        HandleEntryRef entry,
        nint address)
    {
        _entry = entry;
        _address = address;
        _released = false;
    }

    public nint Address
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            EnsureValid();
            return _address;
        }
    }

    public ObjectHeader* Header
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            EnsureValid();
            return (ObjectHeader*)_address;
        }
    }

    public byte* Data
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            EnsureValid();
            return (byte*)_address + ObjectHeader.Size;
        }
    }

    public void Dispose()
    {
        if(_released)
            return;

        _entry.Release();
        _released = true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureValid()
    {
        if(_released)
            throw new ObjectDisposedException(
                nameof(ObjectAccess));
    }
}