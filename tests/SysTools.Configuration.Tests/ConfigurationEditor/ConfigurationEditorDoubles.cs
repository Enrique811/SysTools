using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.Configuration;
using SysTools.Business.ConfigurationEditor;
using SysTools.Business.Connection;
using SysTools.Business.Licensing;
using SysTools.Entities.Configuration;
using SysTools.Entities.ConfigurationEditor;
using SysTools.Entities.Connection;
using SysTools.Entities.Licensing;

namespace SysTools.Configuration.Tests.ConfigurationEditor;

internal sealed class EditorConfigurationServiceStub : IConfigurationService
{
    internal ConfigurationLoadResult LoadResult { get; set; } = Loaded();
    internal AppConfiguration? Saved { get; private set; }
    internal int SaveCalls { get; private set; }
    public Task<ConfigurationLoadResult> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(LoadResult);
    public ConfigurationValidationResult Validate(AppConfiguration value)
    {
        var issues = new List<ConfigurationIssue>();
        if (string.IsNullOrWhiteSpace(value.IpEmpresa)) issues.Add(Issue("ipEmpresa"));
        if (string.IsNullOrWhiteSpace(value.RutaEmpresa)) issues.Add(Issue("rutaEmpresa"));
        if (string.IsNullOrWhiteSpace(value.Usuario)) issues.Add(Issue("usuario"));
        if (string.IsNullOrWhiteSpace(value.Password)) issues.Add(Issue("password"));
        if (value.Ambiente is not ("a" or "b")) issues.Add(new(ConfigurationIssueCode.UnsupportedEnvironment, "ambiente", "Inválido", ConfigurationIssueSeverity.Error));
        if (value.FormatoPrecio is not ("MX" or "CO")) issues.Add(new(ConfigurationIssueCode.UnsupportedPriceFormat, "formatoPrecio", "Inválido", ConfigurationIssueSeverity.Error));
        if (value.Columnas is < 1 or > 3) issues.Add(new(ConfigurationIssueCode.UnsupportedColumnCount, "columnas", "Inválido", ConfigurationIssueSeverity.Error));
        var persistable = issues.All(x => x.Severity != ConfigurationIssueSeverity.Error);
        return new(persistable, persistable && issues.Count == 0, issues);
    }
    public Task<ConfigurationSaveResult> SaveAsync(AppConfiguration value, CancellationToken cancellationToken = default)
    { SaveCalls++; Saved = value; return Task.FromResult(new ConfigurationSaveResult(ConfigurationSaveStatus.Saved, Validate(value))); }
    internal static ConfigurationLoadResult Loaded(string password = "stored-secret")
    {
        var value = Valid(password);
        return new(ConfigurationLoadStatus.Loaded, value, new ConfigurationValidationResult(true, true));
    }
    internal static AppConfiguration Valid(string password = "stored-secret") => new()
    { IpEmpresa="server", RutaEmpresa="db.fdb", Usuario="SYSDBA", Password=password, Ambiente="a", FormatoPrecio="MX", Columnas=1, Licencia="old.lic" };
    private static ConfigurationIssue Issue(string field) => new(ConfigurationIssueCode.RequiredForConnection, field, "Requerido", ConfigurationIssueSeverity.Warning);
}

internal sealed class EditorConnectionStub : IConnectionTestService
{
    internal int Calls { get; private set; }
    internal AppConfiguration? Last { get; private set; }
    internal ConnectionTestStatus Status { get; set; } = ConnectionTestStatus.Success;
    internal Func<AppConfiguration, CancellationToken, Task<ConnectionTestResult>>? Handler { get; set; }
    public Task<ConnectionTestResult> TestAsync(AppConfiguration value, CancellationToken cancellationToken = default)
    {
        Calls++; Last=value;
        return Handler?.Invoke(value, cancellationToken)
            ?? Task.FromResult(new ConnectionTestResult(
                Status,
                Status == ConnectionTestStatus.Success ? "Conexión disponible." : $"Estado: {Status}.",
                TimeSpan.Zero,
                Status == ConnectionTestStatus.InvalidConfiguration
                    ? [new ConnectionConfigurationIssue("ipEmpresa", "Requerido")]
                    : null));
    }
}

internal sealed class EditorLicenseStub : ILicenseService
{
    internal LicenseValidationResult Result { get; set; } = new(LicenseValidationStatus.MissingInput);
    internal string? LastInput { get; private set; }
    public Task<LicenseValidationResult> ValidateAsync(string? input, AppConfiguration configuration, CancellationToken cancellationToken = default) =>
        Task.FromResult(Capture(input));
    private LicenseValidationResult Capture(string? input) { LastInput = input; return Result; }
}

internal sealed class EditorHardwareStub : IHardwareIdProvider
{
    internal string? Value { get; set; } = "00112233-4455-6677-8899-AABBCCDDEEFF";
    internal Exception? Failure { get; set; }
    public Task<string?> GetHardwareIdAsync(CancellationToken cancellationToken = default) =>
        Failure is null ? Task.FromResult(Value) : Task.FromException<string?>(Failure);
}

internal sealed class EditorCatalogStub : IPrinterCatalog, ITemplateCatalog
{
    public Task<CatalogResult> GetAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new CatalogResult(CatalogStatus.Empty));
}

internal sealed class EditorRecoveryStub : IConfigurationRecoveryService
{
    public Task<ConfigurationRecoveryResult> RecoverAsync(AppConfiguration configuration, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ConfigurationRecoveryResult(ConfigurationRecoveryStatus.Recovered));
}

internal static class EditorWorkflowFactory
{
    internal static ConfigurationEditorWorkflow Create(
        EditorConfigurationServiceStub config,
        EditorConnectionStub? connection = null,
        EditorLicenseStub? license = null,
        IHardwareIdProvider? hardware = null,
        IPrinterCatalog? printers = null,
        ITemplateCatalog? templates = null,
        IConfigurationRecoveryService? recovery = null,
        Microsoft.Extensions.Logging.ILogger<ConfigurationEditorWorkflow>? logger = null)
    {
        var catalog = new EditorCatalogStub();
        return new(
            config,
            connection ?? new EditorConnectionStub(),
            license ?? new EditorLicenseStub(),
            hardware ?? new EditorHardwareStub(),
            printers ?? catalog,
            templates ?? catalog,
            recovery ?? new EditorRecoveryStub(),
            logger ?? NullLogger<ConfigurationEditorWorkflow>.Instance);
    }
    internal static ConfigurationDraft Draft() => new() { IpEmpresa="server", RutaEmpresa="db.fdb", Usuario="SYSDBA", Ambiente="a", FormatoPrecio="MX", Columnas=1 };
}
