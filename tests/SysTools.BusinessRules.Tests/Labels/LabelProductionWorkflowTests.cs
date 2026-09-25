using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Labels;
using SysTools.Entities.Labels;
using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.Labels;

public sealed class LabelProductionWorkflowTests
{
    [Theory]
    [InlineData(LabelPrintStatus.Accepted, LabelOutputStatus.Printed, false)]
    [InlineData(LabelPrintStatus.PrinterUnavailable, LabelOutputStatus.PrinterUnavailable, true)]
    [InlineData(LabelPrintStatus.Rejected, LabelOutputStatus.PrintRejected, true)]
    [InlineData(LabelPrintStatus.OutcomeUnknown, LabelOutputStatus.OutcomeUnknown, false)]
    [InlineData(LabelPrintStatus.Canceled, LabelOutputStatus.Canceled, true)]
    public async Task Production_maps_print_outcome_and_retry_policy(LabelPrintStatus printStatus, LabelOutputStatus expected, bool retry)
    {
        var printer = new LabelPrinterDouble { Status = printStatus };
        var result = await Create(new LabelRendererDouble(), printer).CaptureAsync(Product(1), "$1.00");
        Assert.Equal(expected, result.Status);
        Assert.Equal(retry, result.CanRetry);
        Assert.Equal(1, printer.Calls);
    }

    [Fact]
    public async Task Fifty_duplicate_activations_create_at_most_one_production_job()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var renderer = new LabelRendererDouble
        {
            Handler = async (row, token) =>
            {
                started.TrySetResult();
                await release.Task.WaitAsync(token);
                return new(LabelRenderStatus.Rendered, new(row.OperationId, 10, 10, 57, 40, [1]));
            }
        };
        var printer = new LabelPrinterDouble();
        var workflow = Create(renderer, printer);
        var first = workflow.CaptureAsync(Product(1), "$1.00");
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var duplicates = await Task.WhenAll(Enumerable.Range(0, 50).Select(_ => workflow.CaptureAsync(Product(1), "$1.00")));
        release.TrySetResult();
        Assert.Equal(LabelOutputStatus.Printed, (await first).Status);
        Assert.All(duplicates, result => Assert.Equal(LabelOutputStatus.Busy, result.Status));
        Assert.Equal(1, printer.Calls);
    }

    private static LabelOutputWorkflow Create(LabelRendererDouble renderer, LabelPrinterDouble printer) =>
        new(new LabelConfigurationDouble(environment: "b"), new LabelQueueService(), new LabelBarcodeDouble(), new LabelClockDouble(),
            new LabelTemplateDouble(approved: true), renderer, printer, NullLogger<LabelOutputWorkflow>.Instance);
    private static Product Product(int id) => new(id, $"SKU-{id:0000}", $"Product {id}", "Piece", id, "1");
}
