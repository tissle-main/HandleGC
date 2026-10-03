namespace HandleGC;

using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
public struct ObjectHeader
{
    /// <summary>
    /// Повний розмір об'єкта разом із Header.
    /// </summary>
    public uint Length;

    /// <summary>
    /// Індекс Handle у Handle Table.
    /// </summary>
    public uint HandleIndex;

    /// <summary>
    /// Прапори об'єкта.
    /// </summary>
    private ushort Flags;

    /// <summary>
    /// Адреса/type identifier метаданих.
    /// Поки зберігаємо як 64-bit значення.
    /// Реальне пакування адреси буде визначене пізніше.
    /// </summary>
    private ulong Metadata;

    public const int Size = 16;

    private const ushort AliveMask = 1 << 0;

    public bool Alive
    {
        readonly get =>
            (Flags & AliveMask) != 0;

        set
        {
            if(value)
                Flags |= AliveMask;
            else
                Flags &= unchecked((ushort)~AliveMask);
        }
    }

    public ulong MetadataAddress
    {
        readonly get => Metadata;
        set => Metadata = value;
    }

    public readonly ushort RawFlags =>
        Flags;
}