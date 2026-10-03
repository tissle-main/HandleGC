using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace HandleGC;

public sealed class UpperAddressSpace : IDisposable
{
    private const uint MEM_COMMIT = 0x1000;
    private const uint MEM_RESERVE = 0x2000;
    private const uint MEM_DECOMMIT = 0x4000;
    private const uint MEM_RELEASE = 0x8000;

    private const uint PAGE_NOACCESS = 0x01;
    private const uint PAGE_READWRITE = 0x04;

    private nint _baseAddress;
    private readonly nuint _size;
    private readonly nuint _pageSize;

    private bool _disposed;

    public nint BaseAddress => _baseAddress;

    public nuint Size => _size;

    public nint EndAddress =>
        _baseAddress + checked((nint)_size);

    public nuint PageSize => _pageSize;

    public UpperAddressSpace(
        nuint size,
        nint preferredBase = 0)
    {
        if(size == 0)
            throw new ArgumentOutOfRangeException(
                nameof(size));

        _pageSize = GetSystemPageSize();

        size = AlignUp(size, _pageSize);

        nint address = VirtualAlloc(
            preferredBase,
            size,
            MEM_RESERVE,
            PAGE_NOACCESS);

        if(address == 0)
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "Failed to reserve UpperAddressSpace.");
        }

        if(preferredBase != 0 &&
            address != preferredBase)
        {
            VirtualFree(
                address,
                0,
                MEM_RELEASE);

            throw new InvalidOperationException(
                $"Unable to reserve UpperAddressSpace " +
                $"at requested address 0x{preferredBase:X}.");
        }

        _baseAddress = address;
        _size = size;
    }

    public void Commit(
        nint address,
        nuint size)
    {
        ThrowIfDisposed();

        ValidateRange(address, size);

        size = AlignUp(size, _pageSize);

        nint result = VirtualAlloc(
            address,
            size,
            MEM_COMMIT,
            PAGE_READWRITE);

        if(result == 0)
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "Failed to commit UpperAddressSpace range.");
        }
    }

    public void Decommit(
        nint address,
        nuint size)
    {
        ThrowIfDisposed();

        ValidateRange(address, size);

        size = AlignUp(size, _pageSize);

        if(!VirtualFree(
                address,
                size,
                MEM_DECOMMIT))
        {
            throw new Win32Exception(
                Marshal.GetLastWin32Error(),
                "Failed to decommit UpperAddressSpace range.");
        }
    }

    public VirtualRegion CreateRegion(
        nint start,
        nuint size)
    {
        ThrowIfDisposed();

        ValidateRange(start, size);

        return new VirtualRegion(start, size);
    }

    private void ValidateRange(
        nint address,
        nuint size)
    {
        if(size == 0)
            throw new ArgumentOutOfRangeException(
                nameof(size));

        if(address < _baseAddress)
        {
            throw new ArgumentOutOfRangeException(
                nameof(address));
        }

        nuint offset =
            checked((nuint)(address - _baseAddress));

        if(offset > _size)
        {
            throw new ArgumentOutOfRangeException(
                nameof(address));
        }

        nuint alignedSize =
            AlignUp(size, _pageSize);

        if(alignedSize > _size - offset)
        {
            throw new ArgumentOutOfRangeException(
                nameof(size),
                "Range exceeds UpperAddressSpace.");
        }
    }

    private static nuint AlignUp(
        nuint value,
        nuint alignment)
    {
        nuint remainder = value % alignment;

        if(remainder == 0)
            return value;

        return checked(
            value + alignment - remainder);
    }

    private static nuint GetSystemPageSize()
    {
        return (nuint)
            Environment.SystemPageSize;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
    }

    public void Dispose()
    {
        if(_disposed)
            return;

        _disposed = true;

        if(_baseAddress != 0)
        {
            if(!VirtualFree(
                    _baseAddress,
                    0,
                    MEM_RELEASE))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "Failed to release UpperAddressSpace.");
            }

            _baseAddress = 0;
        }

        GC.SuppressFinalize(this);
    }

    ~UpperAddressSpace()
    {
        if(_baseAddress != 0)
        {
            VirtualFree(
                _baseAddress,
                0,
                MEM_RELEASE);
        }
    }

    [DllImport(
        "kernel32.dll",
        SetLastError = true)]
    private static extern nint VirtualAlloc(
        nint lpAddress,
        nuint dwSize,
        uint flAllocationType,
        uint flProtect);

    [DllImport(
        "kernel32.dll",
        SetLastError = true)]
    private static extern bool VirtualFree(
        nint lpAddress,
        nuint dwSize,
        uint dwFreeType);
}