using System.IO;

namespace SysTools.Presentation.Tests.Architecture;

public sealed class LayerDependencyTests
{
    [Fact]
    public void Entities_project_has_no_project_references()
    {
        var root = FindRoot();
        var project = File.ReadAllText(Path.Combine(root, "src", "Entities", "SysTools.Entities.csproj"));
        Assert.DoesNotContain("ProjectReference", project, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Views_and_view_models_do_not_reference_data_layer()
    {
        var presentation = Path.Combine(FindRoot(), "src", "Presentation");
        var files = Directory.EnumerateFiles(presentation, "*.*", SearchOption.AllDirectories)
            .Where(path => (path.Contains("ViewModels")
                    || path.Contains($"{Path.DirectorySeparatorChar}Views{Path.DirectorySeparatorChar}")
                    || path.Contains($"{Path.DirectorySeparatorChar}Commands{Path.DirectorySeparatorChar}"))
                && (path.EndsWith(".cs") || path.EndsWith(".xaml")));
        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("SysTools.Data", content, StringComparison.Ordinal);
            Assert.DoesNotContain("JsonConfigurationRepository", content, StringComparison.Ordinal);
            Assert.DoesNotContain("ProtectedData", content, StringComparison.Ordinal);
            Assert.DoesNotContain("System.IO", content, StringComparison.Ordinal);
            Assert.DoesNotContain("File.", content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Entities_sources_have_no_ui_infrastructure_or_serialization_dependencies()
    {
        var entities = Path.Combine(FindRoot(), "src", "Entities");
        var forbidden = new[]
        {
            "SysTools.Business",
            "SysTools.Data",
            "SysTools.Presentation",
            "System.Text.Json",
            "System.Security.Cryptography",
            "System.Windows",
            "Microsoft.Extensions"
        };

        foreach (var file in Directory.EnumerateFiles(entities, "*.cs", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            foreach (var value in forbidden)
            {
                Assert.DoesNotContain(value, content, StringComparison.Ordinal);
            }
        }
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SysTools.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Solution root not found.");
    }
}
