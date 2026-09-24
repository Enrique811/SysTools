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

    [Fact]
    public void Firebird_provider_types_are_confined_to_data()
    {
        var root = FindRoot();
        var layerDirectories = new[]
        {
            Path.Combine(root, "src", "Entities"),
            Path.Combine(root, "src", "Business")
        };

        foreach (var directory in layerDirectories)
        {
            foreach (var file in EnumerateSourceAndProjectFiles(directory))
            {
                var content = File.ReadAllText(file);
                Assert.DoesNotContain("FirebirdSql", content, StringComparison.Ordinal);
                Assert.DoesNotContain("FbConnection", content, StringComparison.Ordinal);
                Assert.DoesNotContain("SysTools.Data.Connection", content, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Entities_and_business_projects_do_not_reference_data_project()
    {
        var root = FindRoot();
        var projects = new[]
        {
            Path.Combine(root, "src", "Entities", "SysTools.Entities.csproj"),
            Path.Combine(root, "src", "Business", "SysTools.Business.csproj")
        };

        foreach (var project in projects)
        {
            var content = File.ReadAllText(project);
            Assert.DoesNotContain("SysTools.Data.csproj", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("..\\Data\\", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("../Data/", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Views_and_view_models_do_not_reference_firebird_or_data_connection_types()
    {
        var presentation = Path.Combine(FindRoot(), "src", "Presentation");
        var files = EnumerateSourceAndProjectFiles(presentation)
            .Where(path => path.Contains(
                    $"{Path.DirectorySeparatorChar}Views{Path.DirectorySeparatorChar}")
                || path.Contains(
                    $"{Path.DirectorySeparatorChar}ViewModels{Path.DirectorySeparatorChar}"));
        var forbidden = new[]
        {
            "FirebirdSql",
            "FbConnection",
            "SysTools.Data.Connection",
            "FirebirdConnectionFactory"
        };

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            foreach (var value in forbidden)
            {
                Assert.DoesNotContain(value, content, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Views_and_view_models_do_not_perform_licensing_infrastructure_work()
    {
        var presentation = Path.Combine(FindRoot(), "src", "Presentation");
        var files = EnumerateSourceAndProjectFiles(presentation)
            .Where(path => path.Contains(
                    $"{Path.DirectorySeparatorChar}Views{Path.DirectorySeparatorChar}")
                || path.Contains(
                    $"{Path.DirectorySeparatorChar}ViewModels{Path.DirectorySeparatorChar}"));
        var forbidden = new[]
        {
            "System.Security.Cryptography",
            "System.Diagnostics.Process",
            "FileLicenseSourceReader",
            "WindowsHardwareIdProvider",
            "IServerClockRepository"
        };

        foreach (var file in files)
        {
            var content = File.ReadAllText(file);
            foreach (var value in forbidden)
            {
                Assert.DoesNotContain(value, content, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void Licensing_has_no_private_keys_local_clock_or_layer_violation()
    {
        var root = FindRoot();
        var business = Path.Combine(root, "src", "Business", "Licensing");
        var data = Path.Combine(root, "src", "Data", "Licensing");
        foreach (var file in EnumerateSourceAndProjectFiles(business))
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("SysTools.Data", content, StringComparison.Ordinal);
            Assert.DoesNotContain("FirebirdSql", content, StringComparison.Ordinal);
            Assert.DoesNotContain("DateTime.Now", content, StringComparison.Ordinal);
            Assert.DoesNotContain("DateTime.UtcNow", content, StringComparison.Ordinal);
            Assert.DoesNotContain("BEGIN PRIVATE KEY", content, StringComparison.Ordinal);
            Assert.DoesNotContain("private.key", content, StringComparison.OrdinalIgnoreCase);
        }

        foreach (var file in EnumerateSourceAndProjectFiles(data))
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("DateTime.Now", content, StringComparison.Ordinal);
            Assert.DoesNotContain("DateTime.UtcNow", content, StringComparison.Ordinal);
            Assert.DoesNotContain("BEGIN PRIVATE KEY", content, StringComparison.Ordinal);
            Assert.DoesNotContain("private.key", content, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Repository_contracts_and_entities_are_provider_independent_and_sql_is_read_only()
    {
        var root = FindRoot();
        var providerIndependentDirectories = new[]
        {
            Path.Combine(root, "src", "Business", "Repositories"),
            Path.Combine(root, "src", "Entities", "Products")
        };
        foreach (var directory in providerIndependentDirectories)
        {
            foreach (var file in EnumerateSourceAndProjectFiles(directory))
            {
                var content = File.ReadAllText(file);
                Assert.DoesNotContain("FirebirdSql", content, StringComparison.Ordinal);
                Assert.DoesNotContain("FbConnection", content, StringComparison.Ordinal);
                Assert.DoesNotContain("SysTools.Data", content, StringComparison.Ordinal);
            }
        }

        var repositories = Path.Combine(root, "src", "Data", "Repositories");
        foreach (var file in Directory.EnumerateFiles(repositories, "*.cs", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            Assert.DoesNotContain("INSERT ", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UPDATE ", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DELETE ", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("MERGE ", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("EXECUTE PROCEDURE", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DateTime.Now", content, StringComparison.Ordinal);
            Assert.DoesNotContain("DateTime.UtcNow", content, StringComparison.Ordinal);
        }
    }

    private static IEnumerable<string> EnumerateSourceAndProjectFiles(string directory) =>
        Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories)
            .Where(path => !path.Contains(
                    $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                && !path.Contains(
                    $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                && (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
                    || path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)));

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SysTools.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Solution root not found.");
    }
}
