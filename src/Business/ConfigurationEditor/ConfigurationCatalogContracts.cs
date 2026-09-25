using SysTools.Entities.ConfigurationEditor;

namespace SysTools.Business.ConfigurationEditor;

public interface IPrinterCatalog
{
    Task<CatalogResult> GetAsync(CancellationToken cancellationToken = default);
}

public interface ITemplateCatalog
{
    Task<CatalogResult> GetAsync(CancellationToken cancellationToken = default);
}
