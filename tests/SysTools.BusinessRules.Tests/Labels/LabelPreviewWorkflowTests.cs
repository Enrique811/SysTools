using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Labels;
using SysTools.Entities.Labels;
using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.Labels;

public sealed class LabelPreviewWorkflowTests
{
    [Fact]
    public async Task Preview_uses_frozen_configuration_server_date_and_never_spools()
    {
        var configuration = new LabelConfigurationDouble(2, "a");
        var clock = new LabelClockDouble { Value = new(2030, 1, 2) };
        PreparedLabelRow? renderedRow = null;
        var renderer = new LabelRendererDouble { Handler = (row, _) => { renderedRow = row; return Task.FromResult(new LabelRenderResult(LabelRenderStatus.Rendered, new(row.OperationId, 10, 10, 114, 40, [1]))); } };
        var printer = new LabelPrinterDouble();
        var workflow = Create(configuration, clock, new LabelTemplateDouble(2), renderer, printer);
        Assert.Equal(LabelOutputStatus.Pending, (await workflow.CaptureAsync(Product(1), "$1.00")).Status);
        configuration.Factory = () => new() { IpEmpresa="server", RutaEmpresa="database", Usuario="user", Password="secret", Ambiente="b", FormatoPrecio="MX", Columnas=2, Reporte="label-2.label.json", Impresora="Changed", Informacion="Changed" };
        var result = await workflow.CaptureAsync(Product(2), "$2.00");
        Assert.Equal(LabelOutputStatus.PreviewReady, result.Status);
        Assert.Equal(new DateTime(2030, 1, 2), renderedRow!.ServerDate);
        Assert.Equal(LabelOutputEnvironment.Preview, renderedRow.Context.Environment);
        Assert.Equal(0, printer.Calls);
    }

    [Theory]
    [InlineData(LabelTemplateStatus.Missing, LabelOutputStatus.TemplateMissing)]
    [InlineData(LabelTemplateStatus.Invalid, LabelOutputStatus.TemplateIncompatible)]
    [InlineData(LabelTemplateStatus.Incompatible, LabelOutputStatus.TemplateIncompatible)]
    public async Task Template_failures_are_typed_and_retryable(LabelTemplateStatus templateStatus, LabelOutputStatus expected)
    {
        var template = new LabelTemplateDouble { Status = templateStatus };
        var workflow = Create(new(), new(), template, new(), new());
        var result = await workflow.CaptureAsync(Product(1), "$1.00");
        Assert.Equal(expected, result.Status);
        Assert.True(result.CanRetry);
    }

    [Fact]
    public async Task Render_failure_is_typed_and_retryable()
    {
        var renderer = new LabelRendererDouble { Handler = static (_, _) => Task.FromResult(new LabelRenderResult(LabelRenderStatus.Failed)) };
        var result = await Create(new(), new(), new(), renderer, new()).CaptureAsync(Product(1), "$1.00");
        Assert.Equal(LabelOutputStatus.RenderFailed, result.Status);
        Assert.True(result.CanRetry);
    }

    private static LabelOutputWorkflow Create(LabelConfigurationDouble configuration, LabelClockDouble clock, LabelTemplateDouble template, LabelRendererDouble renderer, LabelPrinterDouble printer) =>
        new(configuration, new LabelQueueService(), new LabelBarcodeDouble(), clock, template, renderer, printer, NullLogger<LabelOutputWorkflow>.Instance);
    private static Product Product(int id) => new(id, $"SKU-{id:0000}", $"Product {id}", "Piece", id, "1");
}
