using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Labels;
using SysTools.Entities.Labels;
using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.Labels;

public sealed class LabelOutputCancellationTests
{
    [Fact]
    public async Task Twenty_inflight_cancellations_publish_no_preview_or_print_job()
    {
        for (var iteration = 0; iteration < 20; iteration++)
        {
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var renderer = new LabelRendererDouble
            {
                Handler = async (_, token) =>
                {
                    started.TrySetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, token);
                    return new(LabelRenderStatus.Failed);
                }
            };
            var printer = new LabelPrinterDouble();
            var workflow = new LabelOutputWorkflow(new LabelConfigurationDouble(environment: "b"), new LabelQueueService(), new LabelBarcodeDouble(),
                new LabelClockDouble(), new LabelTemplateDouble(approved: true), renderer, printer, NullLogger<LabelOutputWorkflow>.Instance);
            var operation = workflow.CaptureAsync(new Product(1, "SKU-0001", "Product", "Piece", 10m, "1"), "$10.00");
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
            workflow.Invalidate();
            var result = await operation.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Equal(LabelOutputStatus.Canceled, result.Status);
            Assert.Equal(0, printer.Calls);
        }
    }

    [Fact]
    public async Task Non_cooperative_late_render_is_stale_and_never_reaches_printer()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var renderer = new LabelRendererDouble
        {
            Handler = async (row, _) =>
            {
                started.TrySetResult();
                await release.Task;
                return new(LabelRenderStatus.Rendered, new(row.OperationId, 10, 10, 57, 40, [1]));
            }
        };
        var printer = new LabelPrinterDouble();
        var workflow = new LabelOutputWorkflow(new LabelConfigurationDouble(environment: "b"), new LabelQueueService(), new LabelBarcodeDouble(),
            new LabelClockDouble(), new LabelTemplateDouble(approved: true), renderer, printer, NullLogger<LabelOutputWorkflow>.Instance);
        var operation = workflow.CaptureAsync(new Product(1, "SKU-0001", "Product", "Piece", 10m, "1"), "$10.00");
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        workflow.Invalidate();
        release.TrySetResult();
        var result = await operation.WaitAsync(TimeSpan.FromSeconds(2));
        Assert.Equal(LabelOutputStatus.Canceled, result.Status);
        Assert.Equal(0, printer.Calls);
    }
}
