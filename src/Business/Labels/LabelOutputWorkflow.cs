using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SysTools.Business.Barcodes;
using SysTools.Business.Configuration;
using SysTools.Business.Repositories;
using SysTools.Entities.Configuration;
using SysTools.Entities.Labels;
using SysTools.Entities.Products;

namespace SysTools.Business.Labels;

public sealed class LabelOutputWorkflow : ILabelOutputWorkflow
{
    private readonly IConfigurationService _configuration;
    private readonly ILabelQueueService _queue;
    private readonly IBarcodeService _barcodes;
    private readonly IServerClockRepository _clock;
    private readonly ILabelTemplateProvider _templates;
    private readonly ILabelDocumentRenderer _renderer;
    private readonly ILabelPrinter _printer;
    private readonly ILogger<LabelOutputWorkflow> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _sync = new();
    private LabelRowContext? _context;
    private AppConfiguration? _configurationSnapshot;
    private IReadOnlyList<LabelData>? _completed;
    private string? _operationId;
    private CancellationTokenSource? _activeOperationCancellation;
    private int _pendingCount;
    private long _epoch;

    public LabelOutputWorkflow(IConfigurationService configuration, ILabelQueueService queue, IBarcodeService barcodes,
        IServerClockRepository clock, ILabelTemplateProvider templates, ILabelDocumentRenderer renderer,
        ILabelPrinter printer, ILogger<LabelOutputWorkflow> logger)
    {
        _configuration=configuration; _queue=queue; _barcodes=barcodes; _clock=clock;
        _templates=templates; _renderer=renderer; _printer=printer; _logger=logger;
    }

    public async Task<LabelOutputResult> CaptureAsync(Product product, string formattedPrice, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);
        if (!await _gate.WaitAsync(0, cancellationToken).ConfigureAwait(false)) return Busy();
        try
        {
            if (_completed is not null) return new(LabelOutputStatus.Busy, "Existe una fila pendiente de reintento o descarte.", canRetry: true);
            var epoch = Volatile.Read(ref _epoch);
            if (_context is null)
            {
                var loaded = await _configuration.LoadAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (loaded.Status != ConfigurationLoadStatus.Loaded || loaded.Configuration is null || loaded.Validation?.IsConnectionReady != true)
                    return new(LabelOutputStatus.ConfigurationUnavailable, "La configuración de etiquetas no está disponible.");
                var value = loaded.Configuration;
                var environment = value.Ambiente == "a" ? LabelOutputEnvironment.Preview : value.Ambiente == "b"
                    ? LabelOutputEnvironment.Production : throw new InvalidOperationException("Unsupported environment.");
                var template = string.IsNullOrWhiteSpace(value.Reporte) ? $"label-{value.Columnas}.label.json" : value.Reporte;
                _context = new(environment, value.Columnas, template, value.Impresora, value.Informacion);
                _configurationSnapshot = value;
            }
            if (epoch != Volatile.Read(ref _epoch)) return StaleAndClear();
            LabelData data;
            try { data = new LabelData(product, formattedPrice, _context.Information); }
            catch (ArgumentException) { return new(LabelOutputStatus.InvalidProduct, "El producto no contiene datos suficientes para una etiqueta."); }
            var queued = _queue.Capture(data, _context.Capacity);
            lock (_sync) _pendingCount = queued.IsComplete ? queued.Capacity : queued.Pending.Count;
            if (!queued.IsComplete)
                return new(LabelOutputStatus.Pending, $"Etiqueta {queued.Pending.Count} de {queued.Capacity} preparada; faltan {queued.Remaining}.", queued.Pending.Count, queued.Capacity);
            _completed = queued.Completed;
            _operationId = Guid.NewGuid().ToString("N");
            return await ExecuteProduceAsync(epoch, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            SafeLog("Capture", LabelOutputStatus.ConfigurationUnavailable, null, exception);
            return new(LabelOutputStatus.ConfigurationUnavailable, "No fue posible preparar la etiqueta.", canRetry: _completed is not null);
        }
        finally { _gate.Release(); }
    }

    public async Task<LabelOutputResult> RetryAsync(CancellationToken cancellationToken = default)
    {
        if (!await _gate.WaitAsync(0, cancellationToken).ConfigureAwait(false)) return Busy();
        try
        {
            if (_completed is null || _context is null || _configurationSnapshot is null || _operationId is null)
                return new(LabelOutputStatus.Canceled, "No existe una fila para reintentar.");
            return await ExecuteProduceAsync(Volatile.Read(ref _epoch), cancellationToken).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    public LabelCancelResult Cancel()
    {
        var ownsGate = _gate.Wait(0);
        try
        {
            lock (_sync)
            {
                var pending = _queue.Cancel();
                var hadCompleted = _completed is not null;
                _epoch++;
                if (ownsGate)
                    ClearRow();
                else
                    _activeOperationCancellation?.Cancel();
                return new(pending, hadCompleted);
            }
        }
        finally { if (ownsGate) _gate.Release(); }
    }

    public LabelWorkflowSnapshot GetState()
    {
        lock (_sync)
        {
            var capacity = _context?.Capacity ?? 0;
            var completed = _completed is not null;
            var isBusy = _gate.CurrentCount == 0;
            return new LabelWorkflowSnapshot(_pendingCount, capacity, completed, completed && !isBusy, isBusy);
        }
    }

    public void Invalidate() => Cancel();

    private async Task<LabelOutputResult> ExecuteProduceAsync(long epoch, CancellationToken callerToken)
    {
        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(callerToken);
        lock (_sync) _activeOperationCancellation = operationCancellation;
        try
        {
            return await ProduceAsync(epoch, operationCancellation.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!callerToken.IsCancellationRequested)
        {
            ClearRow();
            return new(LabelOutputStatus.Canceled, "La operación de etiquetas fue cancelada.");
        }
        finally
        {
            lock (_sync)
            {
                if (ReferenceEquals(_activeOperationCancellation, operationCancellation))
                    _activeOperationCancellation = null;
            }
        }
    }

    private async Task<LabelOutputResult> ProduceAsync(long epoch, CancellationToken token)
    {
        var stopwatch = Stopwatch.StartNew();
        var context = _context!;
        var operationId = _operationId!;
        var template = await _templates.ResolveAsync(context.TemplateId, context.Capacity, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (template.Status != LabelTemplateStatus.Available || template.Template is null)
            return Failed(template.Status == LabelTemplateStatus.Missing ? LabelOutputStatus.TemplateMissing : LabelOutputStatus.TemplateIncompatible,
                template.Status == LabelTemplateStatus.Missing ? "La plantilla no está disponible." : "La plantilla no es compatible.", true, stopwatch, operationId);
        if (context.Environment == LabelOutputEnvironment.Production && !template.Template.ProductionApproved)
            return Failed(LabelOutputStatus.TemplateNotApproved, "La plantilla todavía no está aprobada para impresión productiva.", true, stopwatch, operationId);

        var labels = new List<PreparedLabel>(context.Capacity);
        for (var index = 0; index < _completed!.Count; index++)
        {
            var barcode = _barcodes.Generate(_completed[index].Product.Barcode, token);
            if (barcode.Status != SysTools.Entities.Barcodes.BarcodeGenerationStatus.Success)
                return Failed(LabelOutputStatus.BarcodeFailed, "No fue posible generar un código de barras legible.", true, stopwatch, operationId);
            labels.Add(new PreparedLabel(index + 1, _completed[index], barcode));
        }
        DateTime serverDate;
        try { serverDate = await _clock.GetCurrentAsync(_configurationSnapshot!, token).ConfigureAwait(false); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch { return Failed(LabelOutputStatus.RenderFailed, "No fue posible obtener la fecha de la etiqueta.", true, stopwatch, operationId); }
        var row = new PreparedLabelRow(operationId, context, serverDate, labels);
        var rendered = await _renderer.RenderAsync(row, template.Template, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (epoch != Volatile.Read(ref _epoch)) return StaleAndClear();
        if (rendered.Status != LabelRenderStatus.Rendered || rendered.Document is null)
            return Failed(LabelOutputStatus.RenderFailed, "No fue posible generar la etiqueta.", true, stopwatch, operationId);

        if (context.Environment == LabelOutputEnvironment.Preview)
        {
            var result = new LabelOutputResult(LabelOutputStatus.PreviewReady, "Vista previa lista.", preview: rendered.Document);
            ClearRow(); SafeLog("Preview", result.Status, operationId, null, stopwatch); return result;
        }
        var printed = await _printer.PrintAsync(rendered.Document, context.PrinterId, operationId, token).ConfigureAwait(false);
        token.ThrowIfCancellationRequested();
        if (epoch != Volatile.Read(ref _epoch)) return StaleAndClear();
        var status = printed.Status switch
        {
            LabelPrintStatus.Accepted => LabelOutputStatus.Printed,
            LabelPrintStatus.PrinterUnavailable => LabelOutputStatus.PrinterUnavailable,
            LabelPrintStatus.OutcomeUnknown => LabelOutputStatus.OutcomeUnknown,
            LabelPrintStatus.Canceled => LabelOutputStatus.Canceled,
            _ => LabelOutputStatus.PrintRejected
        };
        var canRetry = printed.Status is LabelPrintStatus.PrinterUnavailable or LabelPrintStatus.Rejected or LabelPrintStatus.Canceled;
        var message = status switch
        {
            LabelOutputStatus.Printed => "Trabajo de impresión aceptado.",
            LabelOutputStatus.PrinterUnavailable => "La impresora configurada no está disponible.",
            LabelOutputStatus.OutcomeUnknown => "No fue posible confirmar el resultado; no reintente automáticamente.",
            LabelOutputStatus.Canceled => "La impresión fue cancelada antes de enviarse.",
            _ => "La impresora rechazó el trabajo."
        };
        if (!canRetry) ClearRow();
        return Failed(status, message, canRetry, stopwatch, operationId);
    }

    private LabelOutputResult Failed(LabelOutputStatus status, string message, bool retry, Stopwatch sw, string? id)
    { SafeLog("Output", status, id, null, sw); return new(status, message, canRetry: retry); }
    private static LabelOutputResult Busy() => new(LabelOutputStatus.Busy, "Ya existe una operación de etiquetas en curso.");
    private LabelOutputResult StaleAndClear()
    {
        ClearRow();
        return new(LabelOutputStatus.Stale, "La operación ya no está vigente.");
    }
    private void ClearRow()
    {
        _queue.Cancel();
        lock (_sync)
        {
            _context=null; _configurationSnapshot=null; _completed=null; _operationId=null; _pendingCount=0;
        }
    }
    private void SafeLog(string stage, object status, string? id, Exception? ignored, Stopwatch? sw=null) => _logger.LogInformation(
        "Label output completed at {Stage} with {Status}, {OperationId}, {DurationMs}", stage, status, id, sw?.ElapsedMilliseconds ?? 0);
}
