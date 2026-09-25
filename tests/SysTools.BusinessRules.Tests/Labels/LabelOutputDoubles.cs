using Microsoft.Extensions.Logging;
using SysTools.Business.Barcodes;
using SysTools.Business.Configuration;
using SysTools.Business.Labels;
using SysTools.Business.Repositories;
using SysTools.Entities.Barcodes;
using SysTools.Entities.Configuration;
using SysTools.Entities.Labels;

namespace SysTools.BusinessRules.Tests.Labels;

internal sealed class LabelConfigurationDouble(int capacity = 1, string environment = "a") : IConfigurationService
{
    internal AppConfiguration Value { get; } = new() { IpEmpresa="server", RutaEmpresa="database", Usuario="user", Password="secret", Ambiente=environment, FormatoPrecio="MX", Columnas=capacity, Reporte=$"label-{capacity}.label.json", Impresora="Exact Printer", Informacion="Info" };
    internal Func<AppConfiguration>? Factory { get; set; }
    public Task<ConfigurationLoadResult> LoadAsync(CancellationToken token=default) => Task.FromResult(new ConfigurationLoadResult(ConfigurationLoadStatus.Loaded, Factory?.Invoke() ?? Value, new(true, true)));
    public ConfigurationValidationResult Validate(AppConfiguration value) => new(true, true);
    public Task<ConfigurationSaveResult> SaveAsync(AppConfiguration value, CancellationToken token=default) => throw new NotSupportedException();
}

internal sealed class LabelBarcodeDouble : IBarcodeService
{
    public BarcodeType DetectType(string? input) => BarcodeType.Code128;
    public BarcodeGenerationResult Generate(string? input, CancellationToken token=default) => BarcodeGenerationResult.Succeeded(input!, BarcodeType.Code128, 1, 1, [1]);
}

internal sealed class LabelClockDouble : IServerClockRepository
{
    internal DateTime Value { get; set; } = new(2026, 9, 25);
    public Task<DateTime> GetCurrentAsync(AppConfiguration configuration, CancellationToken token=default) => Task.FromResult(Value);
}

internal sealed class LabelTemplateDouble(int capacity = 1, bool approved = true) : ILabelTemplateProvider
{
    internal LabelTemplateStatus Status { get; set; } = LabelTemplateStatus.Available;
    public Task<LabelTemplateResult> ResolveAsync(string id, int requestedCapacity, CancellationToken token=default) => Task.FromResult(
        Status == LabelTemplateStatus.Available
            ? new LabelTemplateResult(Status, new(id, "1", capacity, 57, 40, 300, approved))
            : new LabelTemplateResult(Status));
}

internal sealed class LabelRendererDouble : ILabelDocumentRenderer
{
    internal Func<PreparedLabelRow, CancellationToken, Task<LabelRenderResult>> Handler { get; set; } = static (row, _) => Task.FromResult(new LabelRenderResult(LabelRenderStatus.Rendered, new(row.OperationId, 10, 10, 57, 40, [1])));
    internal int Calls { get; private set; }
    public Task<LabelRenderResult> RenderAsync(PreparedLabelRow row, LabelTemplateDescriptor template, CancellationToken token=default) { Calls++; return Handler(row, token); }
}

internal sealed class LabelPrinterDouble : ILabelPrinter
{
    internal LabelPrintStatus Status { get; set; } = LabelPrintStatus.Accepted;
    internal int Calls { get; private set; }
    public Task<LabelPrintResult> PrintAsync(LabelPreviewDocument document, string printer, string operationId, CancellationToken token=default) { Calls++; return Task.FromResult(new LabelPrintResult(Status)); }
}

internal sealed class LabelCollectingLogger<T> : ILogger<T>
{
    internal List<string> Messages { get; } = [];
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel level) => true;
    public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
}
