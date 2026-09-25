using Microsoft.Extensions.Logging;
using SysTools.Business.Configuration;
using SysTools.Business.Connection;
using SysTools.Business.Labels;
using SysTools.Business.Licensing;
using SysTools.Business.Products;
using SysTools.Entities.Configuration;
using SysTools.Entities.Connection;
using SysTools.Entities.Licensing;
using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.TestDoubles;

internal sealed class ConfigurationServiceStub : IConfigurationService
{
    internal Func<CancellationToken, Task<ConfigurationLoadResult>> LoadHandler { get; set; } =
        static _ => Task.FromResult(new ConfigurationLoadResult(ConfigurationLoadStatus.StorageFailure));

    internal int LoadCalls { get; private set; }
    internal List<string>? CallOrder { get; init; }

    public Task<ConfigurationLoadResult> LoadAsync(CancellationToken cancellationToken = default)
    {
        LoadCalls++;
        CallOrder?.Add("Configuration");
        return LoadHandler(cancellationToken);
    }

    public ConfigurationValidationResult Validate(AppConfiguration configuration) =>
        throw new NotSupportedException();

    public Task<ConfigurationSaveResult> SaveAsync(
        AppConfiguration configuration,
        CancellationToken cancellationToken = default) => throw new NotSupportedException();
}

internal sealed class ConnectionTestServiceStub : IConnectionTestService
{
    internal Func<AppConfiguration, CancellationToken, Task<ConnectionTestResult>> Handler { get; set; } =
        static (_, _) => Task.FromResult(new ConnectionTestResult(
            ConnectionTestStatus.ServerUnavailable,
            "No disponible.",
            TimeSpan.Zero));

    internal int Calls { get; private set; }
    internal List<string>? CallOrder { get; init; }

    public Task<ConnectionTestResult> TestAsync(
        AppConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        CallOrder?.Add("Connection");
        return Handler(configuration, cancellationToken);
    }
}

internal sealed class LicenseServiceStub : ILicenseService
{
    internal Func<string?, AppConfiguration, CancellationToken, Task<LicenseValidationResult>> Handler { get; set; } =
        static (_, _, _) => Task.FromResult(new LicenseValidationResult(LicenseValidationStatus.MissingInput));

    internal int Calls { get; private set; }
    internal List<string>? CallOrder { get; init; }

    public Task<LicenseValidationResult> ValidateAsync(
        string? licenseInput,
        AppConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        CallOrder?.Add("License");
        return Handler(licenseInput, configuration, cancellationToken);
    }
}

internal sealed class ProductServiceStub : IProductService
{
    internal Func<AppConfiguration, string?, CancellationToken, Task<Product?>> GetHandler { get; set; } =
        static (_, _, _) => Task.FromResult<Product?>(null);

    internal int GetCalls { get; private set; }
    internal string? LastBarcode { get; private set; }
    internal Func<AppConfiguration, string?, CancellationToken, Task<IReadOnlyList<Product>>> SearchHandler { get; set; } =
        static (_, _, _) => Task.FromResult<IReadOnlyList<Product>>(Array.Empty<Product>());
    internal int SearchCalls { get; private set; }
    internal string? LastDescriptionPrefix { get; private set; }

    public Task<Product?> GetByBarcodeAsync(
        AppConfiguration configuration,
        string? barcode,
        CancellationToken cancellationToken = default)
    {
        GetCalls++;
        LastBarcode = barcode;
        return GetHandler(configuration, barcode, cancellationToken);
    }

    public Task<IReadOnlyList<Product>> SearchByDescriptionAsync(
        AppConfiguration configuration,
        string? descriptionPrefix,
        CancellationToken cancellationToken = default)
    {
        SearchCalls++;
        LastDescriptionPrefix = descriptionPrefix;
        return SearchHandler(configuration, descriptionPrefix, cancellationToken);
    }
}

internal sealed class PriceFormatterServiceStub : IPriceFormatterService
{
    internal Func<decimal, string?, string> Handler { get; set; } = static (amount, _) => amount.ToString("0.00");
    internal int Calls { get; private set; }
    internal string? LastFormat { get; private set; }

    public string Format(decimal amount, string? formatCode)
    {
        Calls++;
        LastFormat = formatCode;
        return Handler(amount, formatCode);
    }
}

internal sealed record CapturedLog(
    LogLevel Level,
    EventId EventId,
    IReadOnlyDictionary<string, object?> Properties,
    Exception? Exception);

internal sealed class CollectingLogger<T> : ILogger<T>
{
    internal List<CapturedLog> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var properties = state as IEnumerable<KeyValuePair<string, object?>>;
        Entries.Add(new CapturedLog(
            logLevel,
            eventId,
            properties?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<string, object?>(),
            exception));
    }
}

internal static class PriceVerifierTestData
{
    internal static AppConfiguration Configuration(string format = "MX") => new()
    {
        IpEmpresa = "server",
        RutaEmpresa = "database",
        Usuario = "user",
        Password = "secret",
        Licencia = "license",
        FormatoPrecio = format,
        Informacion = "Gracias por su preferencia"
    };

    internal static ConfigurationLoadResult Loaded(AppConfiguration? configuration = null) => new(
        ConfigurationLoadStatus.Loaded,
        configuration ?? Configuration(),
        new ConfigurationValidationResult(true, true));

    internal static ConnectionTestResult Connected() => new(
        ConnectionTestStatus.Success,
        "Conexion disponible.",
        TimeSpan.Zero);

    internal static LicenseValidationResult ValidLicense()
    {
        var now = new DateTime(2026, 9, 24);
        return new LicenseValidationResult(
            LicenseValidationStatus.Valid,
            LicenseIssuer.Developer,
            now.AddDays(-1),
            now.AddDays(1),
            now);
    }
}
