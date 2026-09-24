using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using SysTools.Business.Barcodes;
using SysTools.Entities.Barcodes;
using ZXing;
using ZXing.Common;

namespace SysTools.Data.Barcodes;

[SupportedOSPlatform("windows")]
public sealed class ZxingBarcodeImageEncoder : IBarcodeImageEncoder
{
    public byte[] EncodePng(
        string normalizedValue,
        BarcodeType type,
        int width,
        int height,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(normalizedValue);
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = Map(type),
                Options = new EncodingOptions
                {
                    Width = width,
                    Height = height,
                    Margin = 0
                }
            };
            var pixels = writer.Write(normalizedValue);
            cancellationToken.ThrowIfCancellationRequested();

            using var bitmap = new Bitmap(
                pixels.Width,
                pixels.Height,
                PixelFormat.Format32bppRgb);
            var area = new Rectangle(0, 0, pixels.Width, pixels.Height);
            var bitmapData = bitmap.LockBits(
                area,
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppRgb);
            try
            {
                Marshal.Copy(pixels.Pixels, 0, bitmapData.Scan0, pixels.Pixels.Length);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            cancellationToken.ThrowIfCancellationRequested();
            using var output = new MemoryStream();
            bitmap.Save(output, ImageFormat.Png);
            cancellationToken.ThrowIfCancellationRequested();
            return output.ToArray();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or InvalidOperationException
                or ExternalException
                or IOException)
        {
            throw new BarcodeEncodingException(exception);
        }
    }

    private static BarcodeFormat Map(BarcodeType type) => type switch
    {
        BarcodeType.Ean8 => BarcodeFormat.EAN_8,
        BarcodeType.Ean13 => BarcodeFormat.EAN_13,
        BarcodeType.UpcA => BarcodeFormat.UPC_A,
        BarcodeType.Code128 => BarcodeFormat.CODE_128,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
