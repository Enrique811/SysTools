using System.Drawing;
using SysTools.Business.Barcodes;
using SysTools.Data.Barcodes;
using SysTools.Entities.Barcodes;

namespace SysTools.Barcodes.Tests.Data;

public sealed class ZxingBarcodeImageEncoderTests
{
    public static TheoryData<string, BarcodeType, int, int> Cases => new()
    {
        { "96385074", BarcodeType.Ean8, 340, 56 },
        { "4006381333931", BarcodeType.Ean13, 340, 56 },
        { "036000291452", BarcodeType.UpcA, 340, 56 },
        { "SKU-001", BarcodeType.Code128, 500, 60 },
        { new string('A', 50), BarcodeType.Code128, 900, 60 }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Encoder_returns_exact_monochrome_png_dimensions(
        string value,
        BarcodeType type,
        int width,
        int height)
    {
        var bytes = new ZxingBarcodeImageEncoder().EncodePng(value, type, width, height);

        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, bytes[..8]);
        using var stream = new MemoryStream(bytes);
        using var bitmap = new Bitmap(stream);
        Assert.Equal(width, bitmap.Width);
        Assert.Equal(height, bitmap.Height);

        var colors = new HashSet<int>();
        for (var y = 0; y < bitmap.Height; y += Math.Max(1, bitmap.Height / 8))
        {
            for (var x = 0; x < bitmap.Width; x += Math.Max(1, bitmap.Width / 32))
            {
                colors.Add(bitmap.GetPixel(x, y).ToArgb());
            }
        }

        Assert.All(colors, color =>
        {
            var pixel = Color.FromArgb(color);
            Assert.True(
                (pixel.R == 0 && pixel.G == 0 && pixel.B == 0)
                || (pixel.R == 255 && pixel.G == 255 && pixel.B == 255));
        });
    }

    [Fact]
    public void Encoder_translates_unrepresentable_zxing_failure()
    {
        Assert.Throws<BarcodeEncodingException>(() =>
            new ZxingBarcodeImageEncoder().EncodePng(
                "á",
                BarcodeType.Code128,
                500,
                60));
    }

    [Fact]
    public void Encoder_preserves_requested_cancellation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        Assert.Throws<OperationCanceledException>(() =>
            new ZxingBarcodeImageEncoder().EncodePng(
                "ABC",
                BarcodeType.Code128,
                500,
                60,
                source.Token));
    }
}
