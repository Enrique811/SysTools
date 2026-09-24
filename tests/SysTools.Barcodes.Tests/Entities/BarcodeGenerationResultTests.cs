using SysTools.Entities.Barcodes;

namespace SysTools.Barcodes.Tests.Entities;

public sealed class BarcodeGenerationResultTests
{
    [Fact]
    public void Success_copies_input_and_each_returned_image()
    {
        byte[] source = [1, 2, 3, 4];
        var result = BarcodeGenerationResult.Succeeded(
            "000123",
            BarcodeType.Code128,
            500,
            60,
            source);

        source[0] = 99;
        var first = result.GetImageBytes();
        first[1] = 88;
        var second = result.GetImageBytes();

        Assert.Equal(BarcodeGenerationStatus.Success, result.Status);
        Assert.Equal("000123", result.NormalizedValue);
        Assert.Equal(BarcodeType.Code128, result.Type);
        Assert.Equal(500, result.Width);
        Assert.Equal(60, result.Height);
        Assert.Equal(4, result.ImageLength);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, second);
        Assert.NotSame(first, second);
        Assert.Equal("Código de barras generado correctamente.", result.Message);
    }

    [Theory]
    [InlineData(BarcodeGenerationStatus.MissingInput, "Ingrese un código de barras.")]
    [InlineData(BarcodeGenerationStatus.UnsupportedValue, "El código contiene caracteres no compatibles.")]
    [InlineData(BarcodeGenerationStatus.InputTooLong, "El código excede la longitud permitida.")]
    [InlineData(BarcodeGenerationStatus.GenerationFailed, "No fue posible generar el código de barras.")]
    public void Failure_has_safe_message_and_no_success_data(
        BarcodeGenerationStatus status,
        string expectedMessage)
    {
        var result = BarcodeGenerationResult.Failed(status);

        Assert.Equal(status, result.Status);
        Assert.Equal(expectedMessage, result.Message);
        Assert.Null(result.NormalizedValue);
        Assert.Null(result.Type);
        Assert.Equal(0, result.Width);
        Assert.Equal(0, result.Height);
        Assert.Equal(0, result.ImageLength);
        Assert.Empty(result.GetImageBytes());
    }

    [Fact]
    public void Failure_rejects_success_status() =>
        Assert.Throws<ArgumentException>(() =>
            BarcodeGenerationResult.Failed(BarcodeGenerationStatus.Success));

    [Theory]
    [InlineData(0, 60, 1)]
    [InlineData(500, 0, 1)]
    [InlineData(500, 60, 0)]
    [InlineData(500, 60, 1048577)]
    public void Success_rejects_invalid_dimensions_or_image_length(
        int width,
        int height,
        int imageLength)
    {
        var bytes = new byte[imageLength];
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            BarcodeGenerationResult.Succeeded(
                "A",
                BarcodeType.Code128,
                width,
                height,
                bytes));
    }
}
