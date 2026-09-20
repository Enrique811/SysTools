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
            .Where(path => (path.Contains("ViewModels") || path.Contains($"{Path.DirectorySeparatorChar}Views{Path.DirectorySeparatorChar}"))
                && (path.EndsWith(".cs") || path.EndsWith(".xaml")));
        foreach (var file in files)
        {
            Assert.DoesNotContain("SysTools.Data", File.ReadAllText(file), StringComparison.Ordinal);
        }
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SysTools.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Solution root not found.");
    }
}
