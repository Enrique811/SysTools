using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SysTools.Business.Configuration;
using SysTools.Business.Connection;
using SysTools.Business.Licensing;
using SysTools.Entities.Configuration;
using SysTools.Entities.ConfigurationEditor;
using SysTools.Entities.Connection;

namespace SysTools.Business.ConfigurationEditor;

public sealed class ConfigurationEditorWorkflow : IConfigurationEditorWorkflow
{
    private readonly IConfigurationService _configuration;
    private readonly IConnectionTestService _connection;
    private readonly ILicenseService _license;
    private readonly IHardwareIdProvider _hardware;
    private readonly IPrinterCatalog _printers;
    private readonly ITemplateCatalog _templates;
    private readonly IConfigurationRecoveryService _recovery;
    private readonly ILogger<ConfigurationEditorWorkflow> _logger;
    private readonly object _sync = new();
    private AppConfiguration? _baseline;
    private bool _baselineConnectionReady;
    private long _epoch;
    private ConnectionProof? _proof;
    private string? _validatedLicensePath;
    private long _licenseRevision = -1;

    public ConfigurationEditorWorkflow(
        IConfigurationService configuration,
        IConnectionTestService connection,
        ILicenseService license,
        IHardwareIdProvider hardware,
        IPrinterCatalog printers,
        ITemplateCatalog templates,
        IConfigurationRecoveryService recovery,
        ILogger<ConfigurationEditorWorkflow> logger)
    {
        _configuration = configuration;
        _connection = connection;
        _license = license;
        _hardware = hardware;
        _printers = printers;
        _templates = templates;
        _recovery = recovery;
        _logger = logger;
    }

    public async Task<ConfigurationEditorSnapshot> OpenAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        long epoch;
        lock (_sync)
        {
            epoch = ++_epoch;
            _baseline = null;
            _proof = null;
            _validatedLicensePath = null;
            _licenseRevision = -1;
        }

        var printerTask = _printers.GetAsync(cancellationToken);
        var templateTask = _templates.GetAsync(cancellationToken);
        var hardwareTask = GetHardwareAsync(cancellationToken);
        var loaded = await _configuration.LoadAsync(cancellationToken).ConfigureAwait(false);
        var printerResult = await printerTask.ConfigureAwait(false);
        var templateResult = await templateTask.ConfigureAwait(false);
        var hardwareResult = await hardwareTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var configuration = loaded.Configuration;
        var openStatus = configuration is not null
            ? ConfigurationEditorOpenStatus.Ready
            : loaded.Status is ConfigurationLoadStatus.InvalidContent or ConfigurationLoadStatus.SecretUnavailable
                ? ConfigurationEditorOpenStatus.RecoveryRequired
                : ConfigurationEditorOpenStatus.OperationalFailure;
        configuration ??= AppConfiguration.CreateDefault();

        lock (_sync)
        {
            if (_epoch != epoch) return StaleSnapshot();
            if (openStatus == ConfigurationEditorOpenStatus.Ready)
            {
                _baseline = configuration;
                _baselineConnectionReady = loaded.Validation?.IsConnectionReady == true;
            }
        }

        var draft = ToDraft(configuration);
        var printers = MergePersisted(printerResult, configuration.Impresora);
        var templates = MergePersisted(templateResult, configuration.Reporte);
        Log("Open", openStatus, stopwatch);
        return new ConfigurationEditorSnapshot(
            openStatus,
            draft,
            !string.IsNullOrEmpty(configuration.Password),
            !string.IsNullOrWhiteSpace(configuration.Licencia),
            loaded.Validation?.IsConnectionReady != true,
            loaded.Issues,
            printers,
            templates,
            hardwareResult);
    }

    public ConfigurationValidationResult Validate(ConfigurationDraft draft, bool hasNewPassword)
    {
        ArgumentNullException.ThrowIfNull(draft);
        return _configuration.Validate(ToConfiguration(draft, hasNewPassword ? "candidate" : string.Empty, preservePassword: true));
    }

    public async Task<ConnectionEditorResult> TestConnectionAsync(
        ConfigurationDraft draft,
        string newPassword,
        long revision,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var (epoch, candidate) = CaptureCandidate(draft, newPassword);
        var result = await _connection.TestAsync(candidate, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        string? token = null;
        lock (_sync)
        {
            if (_epoch == epoch && result.Status == ConnectionTestStatus.Success)
            {
                token = Guid.NewGuid().ToString("N");
                _proof = new ConnectionProof(token, epoch, revision, ConnectionKey.From(candidate));
            }
        }

        Log("ConnectionTest", result.Status, stopwatch);
        return new ConnectionEditorResult(result, token);
    }

    public async Task<LicenseEditorResult> ValidateLicenseAsync(
        ConfigurationDraft draft,
        string newPassword,
        string candidatePath,
        long revision,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var (epoch, candidate) = CaptureCandidate(draft, newPassword);
        var result = await _license.ValidateAsync(candidatePath, candidate, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (_epoch == epoch && result.IsValid)
            {
                _validatedLicensePath = candidatePath;
                _licenseRevision = revision;
            }
        }

        Log("LicenseValidation", result.Status, stopwatch);
        return new LicenseEditorResult(new LicenseSummary(
            result.Status, result.Message, result.Issuer, result.ValidFrom, result.ValidUntil));
    }

    public async Task<ConfigurationEditorSaveResult> SaveAsync(
        ConfigurationDraft draft,
        string newPassword,
        long revision,
        string? proofToken,
        LicenseChange licenseChange,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var (epoch, candidateBase) = CaptureCandidate(draft, newPassword);
        AppConfiguration? baseline;
        ConnectionProof? proof;
        string? validatedLicense;
        long licenseRevision;
        bool baselineReady;
        lock (_sync)
        {
            baseline = _baseline;
            proof = _proof;
            validatedLicense = _validatedLicensePath;
            licenseRevision = _licenseRevision;
            baselineReady = _baselineConnectionReady;
        }

        var licenseValue = licenseChange switch
        {
            LicenseChange.ClearExplicitly => string.Empty,
            LicenseChange.ReplaceValidated when validatedLicense is not null && licenseRevision == revision => validatedLicense,
            _ => baseline?.Licencia ?? string.Empty
        };
        var candidate = Copy(candidateBase, licenseValue);
        var validation = _configuration.Validate(candidate);
        if (!validation.IsPersistable || !validation.IsConnectionReady)
        {
            return Logged(new ConfigurationEditorSaveResult(
                ConfigurationEditorSaveStatus.ValidationFailed,
                "Revise los campos indicados.",
                validation.Issues), "Save", stopwatch);
        }

        var connectionChanged = baseline is null || !ConnectionKey.From(candidate).Equals(ConnectionKey.From(baseline));
        var validProof = proof is not null
            && proof.Epoch == epoch
            && proof.Revision == revision
            && string.Equals(proof.Token, proofToken, StringComparison.Ordinal)
            && proof.Key.Equals(ConnectionKey.From(candidate));
        if ((!baselineReady || connectionChanged) && !validProof)
        {
            return Logged(new ConfigurationEditorSaveResult(
                ConfigurationEditorSaveStatus.TestRequired,
                "Pruebe la conexion con los datos actuales antes de guardar."), "Save", stopwatch);
        }

        var saved = await _configuration.SaveAsync(candidate, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_sync)
        {
            if (_epoch != epoch)
            {
                return new ConfigurationEditorSaveResult(ConfigurationEditorSaveStatus.Stale, "La sesion ya no esta vigente.");
            }

            if (saved.Status == ConfigurationSaveStatus.Saved)
            {
                _baseline = candidate;
                _baselineConnectionReady = true;
                _proof = null;
                _validatedLicensePath = null;
            }
        }

        return Logged(new ConfigurationEditorSaveResult(
            saved.Status == ConfigurationSaveStatus.Saved
                ? ConfigurationEditorSaveStatus.Saved
                : ConfigurationEditorSaveStatus.OperationalFailure,
            saved.Status == ConfigurationSaveStatus.Saved
                ? "Configuracion guardada."
                : "No fue posible guardar la configuracion.",
            saved.Issues), "Save", stopwatch);
    }

    public void Invalidate()
    {
        lock (_sync)
        {
            _epoch++;
            _baseline = null;
            _proof = null;
            _validatedLicensePath = null;
        }
    }

    public async Task<ConfigurationEditorSaveResult> RecoverAsync(
        ConfigurationDraft draft, string newPassword, long revision, string? proofToken,
        bool confirmed, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        if (!confirmed)
            return Logged(new(ConfigurationEditorSaveStatus.RecoveryRequired, "La recuperación requiere confirmación."), "Recovery", stopwatch);
        var (epoch, candidate) = CaptureCandidate(draft, newPassword);
        var validation = _configuration.Validate(candidate);
        ConnectionProof? proof;
        lock (_sync) proof = _proof;
        var validProof = proof is not null && proof.Epoch == epoch && proof.Revision == revision
            && proof.Token == proofToken && proof.Key.Equals(ConnectionKey.From(candidate));
        if (!validation.IsConnectionReady)
            return Logged(new(ConfigurationEditorSaveStatus.ValidationFailed, "Revise los campos indicados.", validation.Issues), "Recovery", stopwatch);
        if (!validProof)
            return Logged(new(ConfigurationEditorSaveStatus.TestRequired, "Pruebe la conexión antes de recuperar."), "Recovery", stopwatch);
        var result = await _recovery.RecoverAsync(candidate, cancellationToken).ConfigureAwait(false);
        if (result.Status != ConfigurationRecoveryStatus.Recovered)
            return Logged(new(ConfigurationEditorSaveStatus.OperationalFailure, "No fue posible recuperar la configuración."), "Recovery", stopwatch);
        lock (_sync)
        {
            if (_epoch != epoch) return new(ConfigurationEditorSaveStatus.Stale, "La sesión ya no está vigente.");
            _baseline = candidate; _baselineConnectionReady = true; _proof = null;
        }
        return Logged(new(ConfigurationEditorSaveStatus.Saved, "Configuración recuperada y guardada."), "Recovery", stopwatch);
    }

    private (long Epoch, AppConfiguration Candidate) CaptureCandidate(ConfigurationDraft draft, string newPassword)
    {
        ArgumentNullException.ThrowIfNull(draft);
        lock (_sync)
        {
            var password = newPassword.Length == 0 ? _baseline?.Password ?? string.Empty : newPassword;
            return (_epoch, ToConfiguration(draft, password, preservePassword: false));
        }
    }

    private AppConfiguration ToConfiguration(ConfigurationDraft draft, string password, bool preservePassword)
    {
        if (preservePassword && password.Length == 0)
        {
            lock (_sync) password = _baseline?.Password ?? string.Empty;
        }
        return new AppConfiguration
        {
            IpEmpresa = draft.IpEmpresa, RutaEmpresa = draft.RutaEmpresa, Usuario = draft.Usuario,
            Password = password, Ambiente = draft.Ambiente, Impresora = draft.Impresora,
            FormatoPrecio = draft.FormatoPrecio, Reporte = draft.Reporte, Columnas = draft.Columnas,
            Informacion = draft.Informacion, Licencia = _baseline?.Licencia ?? string.Empty
        };
    }

    private static AppConfiguration Copy(AppConfiguration source, string license) => new()
    {
        IpEmpresa = source.IpEmpresa, RutaEmpresa = source.RutaEmpresa, Usuario = source.Usuario,
        Password = source.Password, Ambiente = source.Ambiente, Impresora = source.Impresora,
        FormatoPrecio = source.FormatoPrecio, Reporte = source.Reporte, Columnas = source.Columnas,
        Informacion = source.Informacion, Licencia = license
    };

    private static ConfigurationDraft ToDraft(AppConfiguration value) => new()
    {
        IpEmpresa = value.IpEmpresa, RutaEmpresa = value.RutaEmpresa, Usuario = value.Usuario,
        Ambiente = value.Ambiente, Impresora = value.Impresora, FormatoPrecio = value.FormatoPrecio,
        Reporte = value.Reporte, Columnas = value.Columnas, Informacion = value.Informacion
    };

    private static IReadOnlyList<OperationalOption> MergePersisted(CatalogResult result, string persisted)
    {
        var values = result.Options.ToList();
        if (!string.IsNullOrWhiteSpace(persisted)
            && !values.Any(x => string.Equals(x.Id, persisted, StringComparison.OrdinalIgnoreCase)))
            values.Insert(0, new OperationalOption(persisted, $"{persisted} (No disponible)", false));
        return Array.AsReadOnly(values.ToArray());
    }

    private async Task<HardwareIdentityResult> GetHardwareAsync(CancellationToken token)
    {
        try
        {
            var value = await _hardware.GetHardwareIdAsync(token).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(value) ? HardwareIdentityResult.Unavailable() : HardwareIdentityResult.Available(value);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch { return HardwareIdentityResult.Unavailable(); }
    }

    private ConfigurationEditorSaveResult Logged(ConfigurationEditorSaveResult result, string stage, Stopwatch stopwatch)
    { Log(stage, result.Status, stopwatch); return result; }

    private void Log(string stage, object status, Stopwatch stopwatch) => _logger.LogInformation(
        "Configuration editor completed at {Stage} with {Status} in {DurationMs}", stage, status, stopwatch.ElapsedMilliseconds);

    private static ConfigurationEditorSnapshot StaleSnapshot() => new(
        ConfigurationEditorOpenStatus.OperationalFailure, new ConfigurationDraft(), false, false, true,
        [], [], [], HardwareIdentityResult.Unavailable());

    private sealed record ConnectionProof(string Token, long Epoch, long Revision, ConnectionKey Key);
    private sealed record ConnectionKey(string Host, string Database, string User, string Password)
    {
        internal static ConnectionKey From(AppConfiguration value) =>
            new(value.IpEmpresa, value.RutaEmpresa, value.Usuario, value.Password);
    }
}
