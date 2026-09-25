using SysTools.Data.ConfigurationEditor;
using SysTools.Entities.ConfigurationEditor;

namespace SysTools.Configuration.Tests.ConfigurationEditor;

public sealed class ConfigurationCatalogTests
{
    [Fact]
    public async Task Template_catalog_filters_extensions_orders_and_uses_relative_names()
    {
        using var directory = new TestDirectory();
        File.WriteAllText(Path.Combine(directory.Path, "z.jasper"), "x");
        File.WriteAllText(Path.Combine(directory.Path, "Á.frx"), "x");
        File.WriteAllText(Path.Combine(directory.Path, "UPPER.FRX"), "x");
        File.WriteAllText(Path.Combine(directory.Path, "ignore.txt"), "x");
        var nested = Path.Combine(directory.Path, "nested");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(nested, "not-visible.jasper"), "x");
        var catalog = new ManagedTemplateCatalog(directory.Path);

        var result = await catalog.GetAsync();

        Assert.Equal(CatalogStatus.Available, result.Status);
        Assert.Equal(3, result.Options.Count);
        Assert.All(result.Options, item => Assert.DoesNotContain(Path.DirectorySeparatorChar, item.Id));
        Assert.DoesNotContain(result.Options, item => item.Id.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(result.Options, item => item.Id.Contains("not-visible", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Missing_template_root_is_safe_empty_catalog()
    {
        using var directory = new TestDirectory();
        var missing = Path.Combine(directory.Path, "missing");
        var result = await new ManagedTemplateCatalog(missing).GetAsync();
        Assert.Equal(CatalogStatus.Empty, result.Status);
    }

    private sealed class TestDirectory : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SysTools.Catalog", Guid.NewGuid().ToString("N"));
        public TestDirectory() => Directory.CreateDirectory(Path);
        public void Dispose() { if (Directory.Exists(Path)) Directory.Delete(Path, true); }
    }
}
