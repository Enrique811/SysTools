using SysTools.Barcodes.Tests.TestDoubles;
using SysTools.Business.Barcodes;

namespace SysTools.Barcodes.Tests.Business;

public sealed class BarcodeCancellationTests
{
    [Fact]
    public void Cancellation_before_generation_is_propagated_without_encoder_or_log()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var encoder = new StubBarcodeImageEncoder();
        var logger = new CollectingLogger<BarcodeService>();
        var service = new BarcodeService(encoder, logger);

        Assert.Throws<OperationCanceledException>(() =>
            service.Generate("ABC", source.Token));
        Assert.Equal(0, encoder.Calls);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void Cancellation_from_encoder_is_propagated_without_error_log()
    {
        using var source = new CancellationTokenSource();
        var encoder = new StubBarcodeImageEncoder
        {
            Handler = (_, _, _, _, token) =>
            {
                source.Cancel();
                token.ThrowIfCancellationRequested();
                return [];
            }
        };
        var logger = new CollectingLogger<BarcodeService>();
        var service = new BarcodeService(encoder, logger);

        Assert.Throws<OperationCanceledException>(() =>
            service.Generate("ABC", source.Token));
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void Cancellation_observed_after_encoder_is_not_converted_to_success()
    {
        using var source = new CancellationTokenSource();
        var encoder = new StubBarcodeImageEncoder
        {
            Handler = (_, _, width, height, _) =>
            {
                source.Cancel();
                return PngTestData.Create(width, height);
            }
        };
        var logger = new CollectingLogger<BarcodeService>();
        var service = new BarcodeService(encoder, logger);

        Assert.Throws<OperationCanceledException>(() =>
            service.Generate("ABC", source.Token));
        Assert.Empty(logger.Entries);
    }
}
