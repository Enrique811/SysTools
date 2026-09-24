using SysTools.Barcodes.Tests.TestDoubles;
using SysTools.Business.Barcodes;
using SysTools.Entities.Barcodes;

namespace SysTools.Barcodes.Tests.Business;

public sealed class BarcodeServiceGenerationFailureTests
{
    public static IEnumerable<object[]> InvalidOutputs()
    {
        yield return [Array.Empty<byte>()];
        yield return [new byte[] { 1, 2, 3 }];
        yield return [PngTestData.Create(501, 60)];
        yield return [PngTestData.CorruptCrc(500, 60)];
        yield return [PngTestData.Create(500, 60)[..^1]];
        yield return [PngTestData.Create(500, 60)[..^12]];
        yield return [new byte[BarcodeGenerationResult.MaximumImageLength + 1]];
    }

    [Theory]
    [MemberData(nameof(InvalidOutputs))]
    public void Invalid_or_partial_png_returns_generation_failed(byte[] output)
    {
        var encoder = new StubBarcodeImageEncoder
        {
            Handler = (_, _, _, _, _) => output
        };

        var result = Service(encoder).Generate("ABC");

        Assert.Equal(BarcodeGenerationStatus.GenerationFailed, result.Status);
        Assert.Equal("No fue posible generar el código de barras.", result.Message);
        Assert.Empty(result.GetImageBytes());
    }

    [Theory]
    [MemberData(nameof(RecoverableExceptions))]
    public void Recoverable_encoder_exception_returns_generation_failed(Exception exception)
    {
        var encoder = new StubBarcodeImageEncoder
        {
            Handler = (_, _, _, _, _) => throw exception
        };

        var result = Service(encoder).Generate("ABC");

        Assert.Equal(BarcodeGenerationStatus.GenerationFailed, result.Status);
        Assert.Empty(result.GetImageBytes());
    }

    public static IEnumerable<object[]> RecoverableExceptions()
    {
        yield return [new BarcodeEncodingException()];
        yield return [new ArgumentException("encoder")];
        yield return [new InvalidOperationException("encoder")];
        yield return [new IOException("encoder")];
    }

    private static BarcodeService Service(StubBarcodeImageEncoder encoder) =>
        new(encoder, new CollectingLogger<BarcodeService>());
}
