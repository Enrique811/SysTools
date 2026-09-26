using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SysTools.Business.Licensing;
using SysTools.Entities.Support;

namespace SysTools.Business.Support;

public sealed class SupportWorkflow : ISupportWorkflow
{
    private readonly IHardwareIdProvider _hardware;
    private readonly IApplicationVersionProvider _application;
    private readonly ISupportExternalLauncher _launcher;
    private readonly ISupportLogStore _logs;
    private readonly ILogger<SupportWorkflow> _logger;
    private readonly SemaphoreSlim _launchGate = new(1, 1);
    private string? _hardwareId;
    private long _epoch;

    public SupportWorkflow(IHardwareIdProvider hardware, IApplicationVersionProvider application,
        ISupportExternalLauncher launcher, ISupportLogStore logs, ILogger<SupportWorkflow> logger)
    {
        _hardware = hardware; _application = application; _launcher = launcher; _logs = logs; _logger = logger;
    }

    public async Task<SupportInitializationResult> InitializeAsync(CancellationToken cancellationToken = default)
    {
        var epoch = Volatile.Read(ref _epoch);
        try
        {
            var value = await _hardware.GetHardwareIdAsync(cancellationToken).ConfigureAwait(false);
            if (epoch != Volatile.Read(ref _epoch)) return new(false, null, "La operación ya no está vigente.");
            _hardwareId = string.IsNullOrWhiteSpace(value) || value.Length > 128 ? null : value;
            return _hardwareId is null
                ? new(false, null, "El identificador del equipo no está disponible.")
                : new(true, _hardwareId, "Identificador del equipo disponible.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch
        {
            _logger.LogWarning("Support operation {Operation} completed with {Status}", "Initialize", "Unavailable");
            return new(false, null, "No fue posible obtener el identificador del equipo.");
        }
    }

    public async Task<SupportActionResult> PrepareLicenseRequestAsync(CancellationToken cancellationToken = default)
    {
        if (_hardwareId is null)
        {
            var initialized = await InitializeAsync(cancellationToken).ConfigureAwait(false);
            if (!initialized.HardwareAvailable) return new(SupportActionStatus.HardwareUnavailable, initialized.Message, true);
        }
        var diagnosticId = Guid.NewGuid().ToString("N");
        var body = $"Solicito una licencia para SysTools.{Environment.NewLine}{Environment.NewLine}Identificador del equipo: {_hardwareId}{Environment.NewLine}Versión: {_application.Version}{Environment.NewLine}Solicitud: {diagnosticId}";
        var draft = new SupportMailDraft(SupportMailKind.LicenseRequest, string.Empty, "Solicitud de licencia SysTools", body, diagnosticId);
        return new(SupportActionStatus.Ready, "Solicitud de licencia preparada.", draft: draft);
    }

    public SupportActionResult PrepareErrorReport(string? summary, string? detail)
    {
        if (string.IsNullOrWhiteSpace(summary) || summary.Length > 120 || summary.ContainsAny('\r', '\n')
            || string.IsNullOrWhiteSpace(detail) || detail.Length > 4000)
            return new(SupportActionStatus.InvalidInput, "Capture un resumen de 1 a 120 caracteres y un detalle de 1 a 4000.");
        var diagnosticId = Guid.NewGuid().ToString("N");
        var body = $"Resumen: {summary}{Environment.NewLine}{Environment.NewLine}Detalle:{Environment.NewLine}{detail}{Environment.NewLine}{Environment.NewLine}Versión: {_application.Version}{Environment.NewLine}Diagnóstico: {diagnosticId}";
        var draft = new SupportMailDraft(SupportMailKind.ErrorReport, string.Empty, $"Soporte SysTools: {summary}", body, diagnosticId);
        return new(SupportActionStatus.Ready, "Reporte preparado.", draft: draft);
    }

    public async Task<SupportActionResult> OpenDraftAsync(SupportMailDraft draft, SupportChannel channel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (!Enum.IsDefined(channel)) return new(SupportActionStatus.InvalidInput, "El canal de correo no es válido.");
        if (!await _launchGate.WaitAsync(0, cancellationToken).ConfigureAwait(false)) return Busy();
        var stopwatch = Stopwatch.StartNew();
        var epoch = Volatile.Read(ref _epoch);
        try
        {
            var result = await _launcher.OpenMailAsync(draft, channel, cancellationToken).ConfigureAwait(false);
            if (epoch != Volatile.Read(ref _epoch)) return new(SupportActionStatus.Stale, "La operación ya no está vigente.");
            var mapped = MapLaunch(result, "Se abrió el borrador en el canal seleccionado.");
            SafeLog("OpenMail", draft.Kind, channel, mapped.Status, draft.DiagnosticId, stopwatch.ElapsedMilliseconds);
            return mapped;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch
        {
            SafeLog("OpenMail", draft.Kind, channel, SupportActionStatus.LaunchFailed, draft.DiagnosticId, stopwatch.ElapsedMilliseconds);
            return new(SupportActionStatus.LaunchFailed, "No fue posible abrir el canal de correo.", true);
        }
        finally { _launchGate.Release(); }
    }

    public async Task<LogCatalogResult> GetLogsAsync(CancellationToken cancellationToken = default)
    {
        try { return await _logs.GetCatalogAsync(cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(LogCatalogStatus.Failed); }
    }

    public async Task<LogPreviewResult> ReadLogAsync(string fileId, CancellationToken cancellationToken = default)
    {
        try { return await _logs.ReadAsync(fileId, cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch { return new(LogPreviewStatus.Failed); }
    }

    public async Task<SupportActionResult> OpenLogFolderAsync(CancellationToken cancellationToken = default)
    {
        if (!await _launchGate.WaitAsync(0, cancellationToken).ConfigureAwait(false)) return Busy();
        var epoch = Volatile.Read(ref _epoch);
        try
        {
            var result = await _launcher.OpenLogFolderAsync(cancellationToken).ConfigureAwait(false);
            if (epoch != Volatile.Read(ref _epoch)) return new(SupportActionStatus.Stale, "La operación ya no está vigente.");
            var mapped = MapLaunch(result, "Se abrió la carpeta de logs.");
            SafeLog("OpenLogs", null, null, mapped.Status, null, 0);
            return mapped;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch
        {
            SafeLog("OpenLogs", null, null, SupportActionStatus.LaunchFailed, null, 0);
            return new(SupportActionStatus.LaunchFailed, "No fue posible abrir la carpeta de logs.", true);
        }
        finally { _launchGate.Release(); }
    }

    public void Invalidate() => Interlocked.Increment(ref _epoch);

    private static SupportActionResult MapLaunch(ExternalLaunchResult result, string success) => result.Status switch
    {
        ExternalLaunchStatus.Opened => new(SupportActionStatus.Opened, success),
        ExternalLaunchStatus.Canceled => new(SupportActionStatus.Canceled, "La apertura fue cancelada.", true),
        _ => new(SupportActionStatus.LaunchFailed, "No fue posible abrir el destino solicitado.", true)
    };
    private static SupportActionResult Busy() => new(SupportActionStatus.Busy, "Ya existe una apertura en curso.");
    private void SafeLog(string operation, SupportMailKind? kind, SupportChannel? channel, SupportActionStatus status, string? diagnosticId, long duration) =>
        _logger.LogInformation("Support {Operation} {Kind} {Channel} completed with {Status}, {DiagnosticId}, {DurationMs}", operation, kind, channel, status, diagnosticId, duration);
}

file static class SupportStringExtensions
{
    public static bool ContainsAny(this string value, params char[] characters) => value.IndexOfAny(characters) >= 0;
}

