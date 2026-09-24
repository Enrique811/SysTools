using SysTools.Barcodes.Tests.TestDoubles;
using SysTools.Business.Barcodes;
using SysTools.Entities.Barcodes;

namespace SysTools.Barcodes.Tests.Business;

public sealed class BarcodeServiceGenerationTests
{
    [Theory]
    [InlineData("  96385074  ", "96385074", BarcodeType.Ean8, 340, 56)]
    [InlineData("4006381333931", "4006381333931", BarcodeType.Ean13, 340, 56)]
    [InlineData("036000291452", "036000291452", BarcodeType.UpcA, 340, 56)]
    [InlineData("  A B  ", "A B", BarcodeType.Code128, 500, 60)]
    public void Generate_normalizes_once_and_calls_encoder_once(
        string input,
        string expectedValue,
        BarcodeType expectedType,
        int expectedWidth,
        int expectedHeight)
    {
        var encoder = new StubBarcodeImageEncoder();
        var service = new BarcodeService(encoder, new CollectingLogger<BarcodeService>());

        var result = service.Generate(input);

        Assert.Equal(BarcodeGenerationStatus.Success, result.Status);
        Assert.Equal(expectedValue, result.NormalizedValue);
        Assert.Equal(expectedType, result.Type);
        Assert.Equal(expectedWidth, result.Width);
        Assert.Equal(expectedHeight, result.Height);
        Assert.Equal(1, encoder.Calls);
        Assert.Equal(expectedValue, encoder.LastValue);
        Assert.Equal(expectedType, encoder.LastType);
        Assert.Equal(expectedWidth, encoder.LastWidth);
        Assert.Equal(expectedHeight, encoder.LastHeight);
    }

    [Fact]
    public void Generate_uses_dynamic_width_after_code128_minimum()
    {
        var encoder = new StubBarcodeImageEncoder();
        var service = new BarcodeService(encoder, new CollectingLogger<BarcodeService>());
        var value = new string('A', 30);

        var result = service.Generate(value);

        Assert.Equal(540, result.Width);
        Assert.Equal(540, encoder.LastWidth);
    }
}
