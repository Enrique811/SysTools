using System.Drawing.Printing;
using SysTools.Business.ConfigurationEditor;
using SysTools.Entities.ConfigurationEditor;

namespace SysTools.Data.ConfigurationEditor;

public sealed class WindowsPrinterCatalog : IPrinterCatalog
{
    public Task<CatalogResult> GetAsync(CancellationToken cancellationToken = default) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
                return new CatalogResult(CatalogStatus.Unavailable);
            var options = PrinterSettings.InstalledPrinters.Cast<string>()
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.CurrentCultureIgnoreCase)
                .Select(value => new OperationalOption(value, value))
                .ToArray();
            return options.Length == 0
                ? new CatalogResult(CatalogStatus.Empty)
                : new CatalogResult(CatalogStatus.Available, options);
        }
        catch
        {
            return new CatalogResult(CatalogStatus.Unavailable);
        }
    }, cancellationToken);
}
