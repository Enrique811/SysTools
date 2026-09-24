using System.Buffers.Binary;

namespace SysTools.Business.Barcodes;

internal static class PngOutputValidator
{
    private static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];

    internal static bool IsComplete(
        ReadOnlySpan<byte> bytes,
        int expectedWidth,
        int expectedHeight)
    {
        if (bytes.Length is < 45 or > 1024 * 1024
            || !bytes[..8].SequenceEqual(Signature))
        {
            return false;
        }

        var offset = 8;
        var chunkIndex = 0;
        var sawHeader = false;
        var sawImageData = false;
        while (offset < bytes.Length)
        {
            if (bytes.Length - offset < 12)
            {
                return false;
            }

            var dataLength = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(offset, 4));
            if (dataLength > int.MaxValue)
            {
                return false;
            }

            var length = (int)dataLength;
            var chunkEnd = (long)offset + 12L + length;
            if (chunkEnd > bytes.Length)
            {
                return false;
            }

            var type = bytes.Slice(offset + 4, 4);
            var data = bytes.Slice(offset + 8, length);
            var storedCrc = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(offset + 8 + length, 4));
            if (storedCrc != CalculateCrc(type, data))
            {
                return false;
            }

            if (type.SequenceEqual("IHDR"u8))
            {
                if (chunkIndex != 0 || sawHeader || length != 13)
                {
                    return false;
                }

                sawHeader = true;
                if (BinaryPrimitives.ReadInt32BigEndian(data[..4]) != expectedWidth
                    || BinaryPrimitives.ReadInt32BigEndian(data.Slice(4, 4)) != expectedHeight)
                {
                    return false;
                }
            }
            else if (type.SequenceEqual("IDAT"u8))
            {
                if (!sawHeader)
                {
                    return false;
                }

                sawImageData = true;
            }
            else if (type.SequenceEqual("IEND"u8))
            {
                return length == 0
                    && sawHeader
                    && sawImageData
                    && chunkEnd == bytes.Length;
            }

            offset = (int)chunkEnd;
            chunkIndex++;
        }

        return false;
    }

    private static uint CalculateCrc(ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var value in type)
        {
            crc = UpdateCrc(crc, value);
        }

        foreach (var value in data)
        {
            crc = UpdateCrc(crc, value);
        }

        return ~crc;
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
