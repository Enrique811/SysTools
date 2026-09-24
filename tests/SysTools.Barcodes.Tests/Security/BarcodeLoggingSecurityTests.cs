using SysTools.Barcodes.Tests.TestDoubles;
using SysTools.Business.Barcodes;

namespace SysTools.Barcodes.Tests.Security;

public sealed class BarcodeLoggingSecurityTests
{
    [Fact]
    public void Logs_contain_only_diagnostic_metadata_not_barcode_or_image()
    {
        const string sentinel = "Systools-Barcode-Secret-98765";
        var encoder = new StubBarcodeImageEncoder();
        var logger = new CollectingLogger<BarcodeService>();
        var service = new BarcodeService(encoder, logger);

        var result = service.Generate(sentinel);
        var combined = string.Join(Environment.NewLine, logger.Entries.Select(entry => entry.Message));

        Assert.DoesNotContain(sentinel, combined, StringComparison.Ordinal);
        Assert.DoesNotContain("Secret", combined, StringComparison.Ordinal);
        Assert.DoesNotContain(Convert.ToBase64String(result.GetImageBytes()), combined, StringComparison.Ordinal);
        Assert.Contains("Complete", combined, StringComparison.Ordinal);
        Assert.Contains("Code128", combined, StringComparison.Ordinal);
        Assert.Contains(sentinel.Length.ToString(), combined, StringComparison.Ordinal);
        Assert.All(logger.Entries, entry => Assert.Null(entry.Exception));
    }

    [Fact]
    public void Failure_logs_do_not_include_exception_message_or_input()
    {
        const string sentinel = "Private-Barcode-7788";
        var encoder = new StubBarcodeImageEncoder
        {
            Handler = (_, _, _, _, _) =>
                throw new BarcodeEncodingException(new InvalidOperationException(sentinel))
        };
        var logger = new CollectingLogger<BarcodeService>();

        new BarcodeService(encoder, logger).Generate(sentinel);
        var combined = string.Join(Environment.NewLine, logger.Entries.Select(entry => entry.Message));

        Assert.DoesNotContain(sentinel, combined, StringComparison.Ordinal);
        Assert.DoesNotContain("Private", combined, StringComparison.Ordinal);
        Assert.Contains("GenerationFailed", combined, StringComparison.Ordinal);
        Assert.All(logger.Entries, entry => Assert.Null(entry.Exception));
    }
}
