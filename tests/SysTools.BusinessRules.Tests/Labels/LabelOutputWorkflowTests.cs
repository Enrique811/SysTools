using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Barcodes;
using SysTools.Business.Configuration;
using SysTools.Business.Labels;
using SysTools.Business.Repositories;
using SysTools.Entities.Barcodes;
using SysTools.Entities.Configuration;
using SysTools.Entities.Labels;
using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.Labels;

public sealed class LabelOutputWorkflowTests
{
    [Theory]
    [InlineData(1)] [InlineData(2)] [InlineData(3)]
    public async Task One_hundred_rows_per_capacity_preview_exactly_once_in_order(int capacity)
    {
        var renderer = new RendererStub();
        var printer = new PrinterStub();
        var workflow = Create(capacity, "a", renderer, printer);
        for (var row = 0; row < 100; row++)
        {
            for (var position = 0; position < capacity; position++)
            {
                var id = row * capacity + position + 1;
                var result = await workflow.CaptureAsync(Product(id), "$10.00");
                Assert.Equal(position == capacity - 1 ? LabelOutputStatus.PreviewReady : LabelOutputStatus.Pending, result.Status);
            }
        }
        Assert.Equal(100, renderer.Rows.Count);
        Assert.Equal(0, printer.Calls);
        Assert.All(renderer.Rows, row => Assert.Equal(Enumerable.Range(1, capacity), row.Labels.Select(x => x.Position)));
    }

    [Fact]
    public async Task Production_requires_approval_and_never_calls_printer_for_unapproved_template()
    {
        var renderer = new RendererStub(); var printer = new PrinterStub();
        var workflow = Create(1, "b", renderer, printer, approved: false);
        var result = await workflow.CaptureAsync(Product(1), "$10.00");
        Assert.Equal(LabelOutputStatus.TemplateNotApproved, result.Status);
        Assert.True(result.CanRetry);
        Assert.Empty(renderer.Rows); Assert.Equal(0, printer.Calls);
        Assert.True(workflow.Cancel().HadCompletedRow);
        Assert.Equal(LabelOutputStatus.TemplateNotApproved, (await workflow.CaptureAsync(Product(2), "$10.00")).Status);
    }

    [Fact]
    public async Task Approved_production_uses_printer_once_and_zero_preview_result()
    {
        var renderer = new RendererStub(); var printer = new PrinterStub();
        var workflow = Create(1, "b", renderer, printer, approved: true);
        var result = await workflow.CaptureAsync(Product(1), "$10.00");
        Assert.Equal(LabelOutputStatus.Printed, result.Status);
        Assert.Null(result.Preview); Assert.Equal(1, printer.Calls); Assert.Equal("Exact Printer", printer.LastPrinter);
    }

    [Fact]
    public async Task Cancel_during_render_stops_operation_before_printer_and_discards_row()
    {
        var renderer = new BlockingRendererStub(); var printer = new PrinterStub();
        var workflow = Create(1, "b", renderer, printer, approved: true);

        var capture = workflow.CaptureAsync(Product(1), "$10.00");
        await renderer.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var canceled = workflow.Cancel();
        var result = await capture.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(canceled.HadCompletedRow);
        Assert.Equal(LabelOutputStatus.Canceled, result.Status);
        Assert.Equal(0, printer.Calls);
        Assert.Equal(LabelOutputStatus.Printed, (await workflow.CaptureAsync(Product(2), "$10.00")).Status);
    }

    [Fact]
    public async Task Printer_canceled_is_retryable_and_preserves_completed_row()
    {
        var renderer = new RendererStub(); var printer = new PrinterStub(LabelPrintStatus.Canceled);
        var workflow = Create(1, "b", renderer, printer, approved: true);

        var result = await workflow.CaptureAsync(Product(1), "$10.00");

        Assert.Equal(LabelOutputStatus.Canceled, result.Status);
        Assert.True(result.CanRetry);
        Assert.True(workflow.Cancel().HadCompletedRow);
    }

    [Fact]
    public async Task State_reports_pending_completed_retry_and_clear_without_configuration_details()
    {
        var workflow = Create(2, "b", new RendererStub(), new PrinterStub(), approved: false);
        Assert.Equal(new LabelWorkflowSnapshot(0, 0, false, false, false), workflow.GetState());

        Assert.Equal(LabelOutputStatus.Pending, (await workflow.CaptureAsync(Product(1), "$10.00")).Status);
        Assert.Equal(new LabelWorkflowSnapshot(1, 2, false, false, false), workflow.GetState());

        Assert.Equal(LabelOutputStatus.TemplateNotApproved, (await workflow.CaptureAsync(Product(2), "$10.00")).Status);
        Assert.Equal(new LabelWorkflowSnapshot(2, 2, true, true, false), workflow.GetState());
        workflow.Cancel();
        Assert.Equal(new LabelWorkflowSnapshot(0, 0, false, false, false), workflow.GetState());
    }

    private static LabelOutputWorkflow Create(int capacity, string environment, ILabelDocumentRenderer renderer, PrinterStub printer, bool approved=true) =>
        new(new ConfigurationStub(capacity, environment), new LabelQueueService(), new BarcodeStub(), new ClockStub(),
            new TemplateStub(capacity, approved), renderer, printer, NullLogger<LabelOutputWorkflow>.Instance);
    private static Product Product(int id) => new(id, $"CODE{id:00000000}", $"Product {id}", "Piece", 10m, "1");

    private sealed class ConfigurationStub(int columns, string environment) : IConfigurationService
    {
        public Task<ConfigurationLoadResult> LoadAsync(CancellationToken token=default)
        {
            var value = new AppConfiguration { IpEmpresa="s", RutaEmpresa="d", Usuario="u", Password="p", Ambiente=environment, FormatoPrecio="MX", Columnas=columns, Reporte=$"label-{columns}.label.json", Impresora="Exact Printer", Informacion="Info" };
            return Task.FromResult(new ConfigurationLoadResult(ConfigurationLoadStatus.Loaded, value, new ConfigurationValidationResult(true,true)));
        }
        public ConfigurationValidationResult Validate(AppConfiguration value)=>new(true,true);
        public Task<ConfigurationSaveResult> SaveAsync(AppConfiguration value,CancellationToken token=default)=>throw new NotSupportedException();
    }
    private sealed class BarcodeStub : IBarcodeService
    {
        public BarcodeType DetectType(string? input)=>BarcodeType.Code128;
        public BarcodeGenerationResult Generate(string? input,CancellationToken token=default)=>BarcodeGenerationResult.Succeeded(input!, BarcodeType.Code128, 1, 1, [1]);
    }
    private sealed class ClockStub : IServerClockRepository { public Task<DateTime> GetCurrentAsync(AppConfiguration c,CancellationToken t=default)=>Task.FromResult(new DateTime(2026,9,25)); }
    private sealed class TemplateStub(int capacity,bool approved) : ILabelTemplateProvider
    { public Task<LabelTemplateResult> ResolveAsync(string id,int c,CancellationToken t=default)=>Task.FromResult(new LabelTemplateResult(LabelTemplateStatus.Available,new LabelTemplateDescriptor(id,"1",capacity,57,40,300,approved))); }
    private sealed class RendererStub : ILabelDocumentRenderer
    {
        public List<PreparedLabelRow> Rows {get;}=[];
        public Task<LabelRenderResult> RenderAsync(PreparedLabelRow row,LabelTemplateDescriptor t,CancellationToken ct=default)
        { Rows.Add(row); return Task.FromResult(new LabelRenderResult(LabelRenderStatus.Rendered,new LabelPreviewDocument(row.OperationId,10,10,57*t.Capacity,40,[1]))); }
    }
    private sealed class BlockingRendererStub : ILabelDocumentRenderer
    {
        private int _calls;
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task<LabelRenderResult> RenderAsync(PreparedLabelRow row, LabelTemplateDescriptor template, CancellationToken token=default)
        {
            if (Interlocked.Increment(ref _calls) == 1)
            {
                Started.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }
            return new(LabelRenderStatus.Rendered, new LabelPreviewDocument(row.OperationId, 10, 10, 57 * template.Capacity, 40, [1]));
        }
    }
    private sealed class PrinterStub(LabelPrintStatus status = LabelPrintStatus.Accepted) : ILabelPrinter
    { public int Calls {get;private set;} public string? LastPrinter {get;private set;} public Task<LabelPrintResult> PrintAsync(LabelPreviewDocument d,string p,string id,CancellationToken t=default){Calls++;LastPrinter=p;return Task.FromResult(new LabelPrintResult(status));} }
}
