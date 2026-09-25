using SysTools.Business.Labels;
using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.Labels;

public sealed class LabelOutputSecurityTests
{
    [Fact]
    public async Task One_hundred_operations_never_log_configuration_product_barcode_or_payload()
    {
        var logger = new LabelCollectingLogger<LabelOutputWorkflow>();
        var workflow = new LabelOutputWorkflow(new LabelConfigurationDouble(), new LabelQueueService(), new LabelBarcodeDouble(),
            new LabelClockDouble(), new LabelTemplateDouble(), new LabelRendererDouble(), new LabelPrinterDouble(), logger);
        for (var index = 0; index < 100; index++)
        {
            var sentinel = $"SENSITIVE-{index:000}";
            var result = await workflow.CaptureAsync(new Product(index + 1, sentinel, sentinel, sentinel, 10m, "1"), $"${index}.00");
            Assert.Equal(SysTools.Entities.Labels.LabelOutputStatus.PreviewReady, result.Status);
        }
        var log = string.Join('\n', logger.Messages);
        Assert.DoesNotContain("SENSITIVE-", log, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", log, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Exact Printer", log, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Byte", log, StringComparison.Ordinal);
    }
}
