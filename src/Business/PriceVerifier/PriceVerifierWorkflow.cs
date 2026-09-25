using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SysTools.Business.Configuration;
using SysTools.Business.Connection;
using SysTools.Business.Labels;
using SysTools.Business.Licensing;
using SysTools.Business.Products;
using SysTools.Entities.Configuration;
using SysTools.Entities.Connection;
using SysTools.Entities.PriceVerifier;

namespace SysTools.Business.PriceVerifier;

public sealed class PriceVerifierWorkflow : IPriceVerifierWorkflow
{
    private readonly IConfigurationService _configurationService;
    private readonly IConnectionTestService _connectionService;
    private readonly ILicenseService _licenseService;
    private readonly IProductService _productService;
    private readonly IPriceFormatterService _priceFormatter;
    private readonly ILogger<PriceVerifierWorkflow> _logger;
    private readonly object _sync = new();
    private AppConfiguration? _configuration;
    private long _epoch;

    public PriceVerifierWorkflow(
        IConfigurationService configurationService,
        IConnectionTestService connectionService,
        ILicenseService licenseService,
        IProductService productService,
        IPriceFormatterService priceFormatter,
        ILogger<PriceVerifierWorkflow> logger)
    {
        _configurationService = configurationService;
        _connectionService = connectionService;
        _licenseService = licenseService;
        _productService = productService;
        _priceFormatter = priceFormatter;
        _logger = logger;
    }

    public async Task<PriceVerifierPreparationResult> PrepareAsync(
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        long epoch;
        lock (_sync)
        {
            epoch = ++_epoch;
            _configuration = null;
        }

        try
        {
            var loaded = await _configurationService.LoadAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsCurrent(epoch))
            {
                return ConfigurationUnavailable();
            }

            if (loaded.Status != ConfigurationLoadStatus.Loaded
                || loaded.Configuration is null
                || loaded.Validation?.IsConnectionReady != true)
            {
                return LogPreparation(ConfigurationUnavailable(), stopwatch);
            }

            var connection = await _connectionService
                .TestAsync(loaded.Configuration, cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsCurrent(epoch))
            {
                return ConfigurationUnavailable();
            }

            if (connection.Status != ConnectionTestStatus.Success)
            {
                return LogPreparation(new PriceVerifierPreparationResult(
                    PriceVerifierPreparationStatus.ConnectionUnavailable,
                    "No fue posible establecer la conexión. Revise la configuración y reintente.",
                    connection.Status), stopwatch);
            }

            var license = await _licenseService
                .ValidateAsync(loaded.Configuration.Licencia, loaded.Configuration, cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsCurrent(epoch))
            {
                return ConfigurationUnavailable();
            }

            if (!license.IsValid)
            {
                return LogPreparation(new PriceVerifierPreparationResult(
                    PriceVerifierPreparationStatus.LicenseUnavailable,
                    "La licencia no está disponible. Revise su vigencia y reintente.",
                    ConnectionTestStatus.Success,
                    license.Status), stopwatch);
            }

            lock (_sync)
            {
                if (_epoch != epoch)
                {
                    return ConfigurationUnavailable();
                }

                _configuration = loaded.Configuration;
            }

            return LogPreparation(new PriceVerifierPreparationResult(
                PriceVerifierPreparationStatus.Ready,
                "Verificador disponible.",
                ConnectionTestStatus.Success,
                license.Status,
                loaded.Configuration.Informacion), stopwatch);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            ClearIfCurrent(epoch);
            return LogPreparation(ConfigurationUnavailable(), stopwatch, "UnexpectedFailure");
        }
    }

    public async Task<PriceVerifierLookupResult> LookupAsync(
        string? barcode,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        AppConfiguration? configuration;
        long epoch;
        lock (_sync)
        {
            configuration = _configuration;
            epoch = _epoch;
        }

        if (configuration is null)
        {
            return LogLookup(OperationalFailure(), stopwatch, "SessionUnavailable");
        }

        var normalized = barcode?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            return LogLookup(new PriceVerifierLookupResult(
                PriceVerifierLookupStatus.MissingInput,
                "Ingrese un código de barras."), stopwatch);
        }

        if (normalized.Length > 50)
        {
            return LogLookup(new PriceVerifierLookupResult(
                PriceVerifierLookupStatus.InputTooLong,
                "El código no puede exceder 50 caracteres."), stopwatch);
        }

        try
        {
            var product = await _productService
                .GetByBarcodeAsync(configuration, normalized, cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsCurrent(epoch))
            {
                return OperationalFailure();
            }

            if (product is null)
            {
                return LogLookup(new PriceVerifierLookupResult(
                    PriceVerifierLookupStatus.NotFound,
                    "No se encontró un producto con ese código."), stopwatch);
            }

            var formattedPrice = product.PriceWithTax is decimal price
                ? _priceFormatter.Format(price, configuration.FormatoPrecio)
                : null;
            return LogLookup(new PriceVerifierLookupResult(
                PriceVerifierLookupStatus.Success,
                "Producto encontrado.",
                product,
                formattedPrice), stopwatch);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            ClearIfCurrent(epoch);
            return LogLookup(OperationalFailure(), stopwatch, "CatalogFailure");
        }
    }

    public async Task<PriceVerifierSearchResult> SearchAsync(
        string? descriptionPrefix,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        AppConfiguration? configuration;
        long epoch;
        lock (_sync)
        {
            configuration = _configuration;
            epoch = _epoch;
        }

        if (configuration is null)
        {
            return LogSearch(SearchOperationalFailure(), stopwatch, "SessionUnavailable");
        }

        var normalized = descriptionPrefix?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            return LogSearch(new PriceVerifierSearchResult(
                PriceVerifierSearchStatus.MissingInput,
                "Ingrese el inicio de la descripcion."), stopwatch);
        }

        if (normalized.Length > 255)
        {
            return LogSearch(new PriceVerifierSearchResult(
                PriceVerifierSearchStatus.InputTooLong,
                "La descripcion no puede exceder 255 caracteres."), stopwatch);
        }

        try
        {
            var products = await _productService
                .SearchByDescriptionAsync(configuration, normalized, cancellationToken)
                .ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsCurrent(epoch))
            {
                return SearchOperationalFailure();
            }

            if (products.Count == 0)
            {
                return LogSearch(new PriceVerifierSearchResult(
                    PriceVerifierSearchStatus.NoMatches,
                    "No se encontraron productos."), stopwatch);
            }

            var items = products.Select(product => new PriceVerifierSearchItem(
                product.Barcode,
                product.Description,
                product.PriceWithTax is decimal price
                    ? _priceFormatter.Format(price, configuration.FormatoPrecio)
                    : null,
                product.Stock));
            return LogSearch(new PriceVerifierSearchResult(
                PriceVerifierSearchStatus.Success,
                "Productos encontrados.",
                items), stopwatch);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            ClearIfCurrent(epoch);
            return LogSearch(SearchOperationalFailure(), stopwatch, "CatalogFailure");
        }
    }

    public void Invalidate()
    {
        lock (_sync)
        {
            _epoch++;
            _configuration = null;
        }
    }

    private bool IsCurrent(long epoch)
    {
        lock (_sync)
        {
            return _epoch == epoch;
        }
    }

    private void ClearIfCurrent(long epoch)
    {
        lock (_sync)
        {
            if (_epoch == epoch)
            {
                _configuration = null;
                _epoch++;
            }
        }
    }

    private PriceVerifierPreparationResult LogPreparation(
        PriceVerifierPreparationResult result,
        Stopwatch stopwatch,
        string? category = null)
    {
        _logger.LogInformation(
            "Price verifier operation completed at {Stage} with {Status}, {Category}, {DurationMs}",
            "Preparation",
            result.Status,
            category ?? "Expected",
            stopwatch.ElapsedMilliseconds);
        return result;
    }

    private PriceVerifierLookupResult LogLookup(
        PriceVerifierLookupResult result,
        Stopwatch stopwatch,
        string? category = null)
    {
        _logger.LogInformation(
            "Price verifier operation completed at {Stage} with {Status}, {Category}, {DurationMs}",
            "Lookup",
            result.Status,
            category ?? "Expected",
            stopwatch.ElapsedMilliseconds);
        return result;
    }

    private PriceVerifierSearchResult LogSearch(
        PriceVerifierSearchResult result,
        Stopwatch stopwatch,
        string? category = null)
    {
        _logger.LogInformation(
            "Price verifier operation completed at {Stage} with {Status}, {Category}, {DurationMs}",
            "Search",
            result.Status,
            category ?? "Expected",
            stopwatch.ElapsedMilliseconds);
        return result;
    }

    private static PriceVerifierSearchResult SearchOperationalFailure() => new(
        PriceVerifierSearchStatus.OperationalFailure,
        "No fue posible consultar el catalogo. Reintente la preparacion.");

    private static PriceVerifierPreparationResult ConfigurationUnavailable() => new(
        PriceVerifierPreparationStatus.ConfigurationUnavailable,
        "La configuración no está disponible. Revísela y reintente.");

    private static PriceVerifierLookupResult OperationalFailure() => new(
        PriceVerifierLookupStatus.OperationalFailure,
        "No fue posible consultar el catálogo. Reintente la preparación.");
}
