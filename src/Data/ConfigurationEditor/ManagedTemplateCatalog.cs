using SysTools.Business.ConfigurationEditor;
using SysTools.Entities.ConfigurationEditor;

namespace SysTools.Data.ConfigurationEditor;

public sealed class ManagedTemplateCatalog : ITemplateCatalog
{
    private readonly string _root;

    public ManagedTemplateCatalog() : this(Path.Combine(AppContext.BaseDirectory, "Reports", "Templates")) { }
    public ManagedTemplateCatalog(string root) => _root = root;

    public Task<CatalogResult> GetAsync(CancellationToken cancellationToken = default) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            if (!Directory.Exists(_root)) return new CatalogResult(CatalogStatus.Empty);
            var options = Directory.EnumerateFiles(_root, "*", SearchOption.TopDirectoryOnly)
                .Where(path => Path.GetExtension(path).Equals(".frx", StringComparison.OrdinalIgnoreCase)
                    || Path.GetExtension(path).Equals(".jasper", StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.CurrentCultureIgnoreCase)
                .Select(name => new OperationalOption(name!, name!))
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
