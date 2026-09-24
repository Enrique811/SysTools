using SysTools.Barcodes.Tests.TestDoubles;
using SysTools.Business.Barcodes;
using SysTools.Entities.Barcodes;

namespace SysTools.Barcodes.Tests.Business;

public sealed class BarcodeServiceInputFailureTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t\r\n ")]
    public void Missing_input_returns_safe_result_without_encoder(string? input)
    {
        var encoder = new StubBarcodeImageEncoder();
        var result = Service(encoder).Generate(input);

        Assert.Equal(BarcodeGenerationStatus.MissingInput, result.Status);
        Assert.Equal("Ingrese un código de barras.", result.Message);
        Assert.Empty(result.GetImageBytes());
        Assert.Equal(0, encoder.Calls);
    }

    [Fact]
    public void Fifty_characters_are_accepted()
    {
        var encoder = new StubBarcodeImageEncoder();
        var result = Service(encoder).Generate(new string('A', 50));
        Assert.Equal(BarcodeGenerationStatus.Success, result.Status);
        Assert.Equal(900, result.Width);
        Assert.Equal(1, encoder.Calls);
    }

    [Fact]
    public void Fifty_one_characters_are_rejected_before_encoder()
    {
        var encoder = new StubBarcodeImageEncoder();
        var result = Service(encoder).Generate(new string('A', 51));
        Assert.Equal(BarcodeGenerationStatus.InputTooLong, result.Status);
        Assert.Equal("El código excede la longitud permitida.", result.Message);
        Assert.Empty(result.GetImageBytes());
        Assert.Equal(0, encoder.Calls);
    }

    [Theory]
    [InlineData("á")]
    [InlineData("１２３")]
    [InlineData("\u00A0A")]
    [InlineData("ñ-001")]
    public void Non_ascii_code128_is_rejected_without_encoder(string input)
    {
        var encoder = new StubBarcodeImageEncoder();
        var result = Service(encoder).Generate(input);
        Assert.Equal(BarcodeGenerationStatus.UnsupportedValue, result.Status);
        Assert.Equal("El código contiene caracteres no compatibles.", result.Message);
        Assert.Empty(result.GetImageBytes());
        Assert.Equal(0, encoder.Calls);
    }

    private static BarcodeService Service(StubBarcodeImageEncoder encoder) =>
        new(encoder, new CollectingLogger<BarcodeService>());
}
