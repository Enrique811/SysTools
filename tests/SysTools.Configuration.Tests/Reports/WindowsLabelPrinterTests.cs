using SysTools.Data.Reports;
using SysTools.Entities.Labels;

namespace SysTools.Configuration.Tests.Reports;

public sealed class WindowsLabelPrinterTests
{
    [Fact]
    public async Task Missing_printer_never_falls_back_or_submits()
    {
        var backend = new Backend(["Default printer"], LabelPrintStatus.Accepted);
        var result = await new WindowsLabelPrinter(backend).PrintAsync(Document(), "Missing printer", OperationId());
        Assert.Equal(LabelPrintStatus.PrinterUnavailable, result.Status);
        Assert.Equal(0, backend.SubmitCalls);
    }

    [Theory]
    [InlineData(LabelPrintStatus.Accepted)]
    [InlineData(LabelPrintStatus.PrinterUnavailable)]
    [InlineData(LabelPrintStatus.Rejected)]
    [InlineData(LabelPrintStatus.OutcomeUnknown)]
    [InlineData(LabelPrintStatus.Canceled)]
    public async Task Exact_printer_preserves_backend_outcome(LabelPrintStatus outcome)
    {
        var backend = new Backend(["LABEL-01"], outcome);
        var result = await new WindowsLabelPrinter(backend).PrintAsync(Document(), "label-01", OperationId());
        Assert.Equal(outcome, result.Status);
        Assert.Equal("LABEL-01", backend.LastPrinter);
        Assert.Equal(1, backend.SubmitCalls);
    }

    [Fact]
    public async Task Cancellation_before_lookup_does_not_submit()
    {
        var backend = new Backend(["LABEL-01"], LabelPrintStatus.Accepted);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new WindowsLabelPrinter(backend).PrintAsync(Document(), "LABEL-01", OperationId(), cancellation.Token));
        Assert.Equal(0, backend.SubmitCalls);
    }

    private static string OperationId() => Guid.NewGuid().ToString("N");
    private static LabelPreviewDocument Document() => new(OperationId(), 10, 10, 57, 40, [1]);

    private sealed class Backend(IReadOnlyCollection<string> printers, LabelPrintStatus status) : IWindowsLabelPrintBackend
    {
        public IReadOnlyCollection<string> InstalledPrinters => printers;
        public int SubmitCalls { get; private set; }
        public string? LastPrinter { get; private set; }
        public LabelPrintStatus Submit(LabelPreviewDocument document, string exactPrinterId, string operationId, CancellationToken cancellationToken)
        {
            SubmitCalls++;
            LastPrinter = exactPrinterId;
            return status;
        }
    }
}
