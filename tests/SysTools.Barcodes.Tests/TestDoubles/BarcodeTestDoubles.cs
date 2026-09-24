using System.Buffers.Binary;
using Microsoft.Extensions.Logging;
using SysTools.Business.Barcodes;
using SysTools.Entities.Barcodes;

namespace SysTools.Barcodes.Tests.TestDoubles;

internal sealed class StubBarcodeImageEncoder : IBarcodeImageEncoder
{
    public Func<string, BarcodeType, int, int, CancellationToken, byte[]> Handler { get; set; } =
        (_, _, width, height, _) => PngTestData.Create(width, height);

    public int Calls { get; private set; }

    public string? LastValue { get; private set; }

    public BarcodeType? LastType { get; private set; }

    public int LastWidth { get; private set; }

    public int LastHeight { get; private set; }

    public byte[] EncodePng(
        string normalizedValue,
        BarcodeType type,
        int width,
        int height,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        LastValue = normalizedValue;
        LastType = type;
        LastWidth = width;
        LastHeight = height;
        return Handler(normalizedValue, type, width, height, cancellationToken);
    }
}

internal sealed class CollectingLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, formatter(state, exception), exception));
}

internal static class PngTestData
{
    private static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static byte[] Create(int width, int height)
    {
        using var stream = new MemoryStream();
        stream.Write(Signature);
        Span<byte> ihdr = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(ihdr[..4], width);
        BinaryPrimitives.WriteInt32BigEndian(ihdr.Slice(4, 4), height);
        ihdr[8] = 8;
        ihdr[9] = 0;
        WriteChunk(stream, "IHDR"u8, ihdr);
        WriteChunk(stream, "IDAT"u8, [0]);
        WriteChunk(stream, "IEND"u8, []);
        return stream.ToArray();
    }

    public static byte[] CorruptCrc(int width, int height)
    {
        var bytes = Create(width, height);
        bytes[^1] ^= 0xFF;
        return bytes;
    }

    private static void WriteChunk(Stream stream, ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        Span<byte> length = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        stream.Write(length);
        stream.Write(type);
        stream.Write(data);

        var crc = 0xFFFFFFFFu;
        foreach (var value in type)
        {
            crc = UpdateCrc(crc, value);
        }

        foreach (var value in data)
        {
            crc = UpdateCrc(crc, value);
        }

        Span<byte> crcBytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(crcBytes, ~crc);
        stream.Write(crcBytes);
    }

    private static uint UpdateCrc(uint crc, byte value)
    {
        crc ^= value;
        for (var bit = 0; bit < 8; bit++)
        {
            crc = (crc & 1) == 0 ? crc >> 1 : 0xEDB88320u ^ (crc >> 1);
        }

        return crc;
    }
}
