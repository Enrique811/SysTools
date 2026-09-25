using SysTools.Entities.ConfigurationEditor;

namespace SysTools.Configuration.Tests.ConfigurationEditor;

public sealed class ConfigurationEditorEntityTests
{
    [Fact]
    public void Draft_normalizes_null_without_carrying_secrets()
    {
        var draft = new ConfigurationDraft { IpEmpresa = null!, Informacion = "  texto interior  " };
        Assert.Equal(string.Empty, draft.IpEmpresa);
        Assert.Equal("  texto interior  ", draft.Informacion);
        Assert.DoesNotContain(draft.GetType().GetProperties(), p => p.Name.Contains("Password", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("Licencia", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Catalog_result_is_read_only_and_enforces_state()
    {
        var result = new CatalogResult(CatalogStatus.Available, [new OperationalOption("p", "Printer")]);
        var collection = Assert.IsAssignableFrom<ICollection<OperationalOption>>(result.Options);
        Assert.True(collection.IsReadOnly);
        Assert.Throws<ArgumentException>(() => new CatalogResult(CatalogStatus.Available));
    }
}
