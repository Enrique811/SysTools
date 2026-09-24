using System.Drawing;
using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Barcodes;
using SysTools.Data.Barcodes;
using SysTools.Entities.Barcodes;
using ZXing;
using ZXing.Common;

namespace SysTools.Barcodes.Tests.Compatibility;

public sealed class BarcodeRoundTripTests
{
    public static TheoryData<string, BarcodeType, BarcodeFormat> Cases => new()
    {
        { "96385074", BarcodeType.Ean8, BarcodeFormat.EAN_8 },
        { "4006381333931", BarcodeType.Ean13, BarcodeFormat.EAN_13 },
        { "036000291452", BarcodeType.UpcA, BarcodeFormat.UPC_A },
        { "000 SKU 01", BarcodeType.Code128, BarcodeFormat.CODE_128 }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Generated_png_decodes_to_exact_type_and_text(
        string value,
        BarcodeType expectedType,
        BarcodeFormat expectedFormat)
    {
        var service = new BarcodeService(
            new ZxingBarcodeImageEncoder(),
            NullLogger<BarcodeService>.Instance);

        var generated = service.Generate(value);
        var bytes = generated.GetImageBytes();
        using var stream = new MemoryStream(bytes);
        using var bitmap = new Bitmap(stream);
        Assert.Equal(generated.Width, bitmap.Width);
        Assert.Equal(generated.Height, bitmap.Height);
        using var readableCanvas = AddReaderQuietZone(bitmap);
        var reader = new ZXing.Windows.Compatibility.BarcodeReader
        {
            Options = new DecodingOptions
            {
                PossibleFormats = [expectedFormat],
                PureBarcode = false,
                TryHarder = true
            }
        };

        var decoded = reader.Decode(readableCanvas);

        Assert.NotNull(decoded);
        Assert.Equal(expectedType, generated.Type);
        Assert.Equal(expectedFormat, decoded.BarcodeFormat);
        Assert.Equal(value, decoded.Text);
        Assert.Equal(value, generated.NormalizedValue);
    }

    private static Bitmap AddReaderQuietZone(Bitmap barcode)
    {
        var canvas = new Bitmap(barcode.Width + 40, barcode.Height + 20);
        using var graphics = Graphics.FromImage(canvas);
        graphics.Clear(Color.White);
        graphics.DrawImageUnscaled(barcode, 20, 10);
        return canvas;
    }
}
