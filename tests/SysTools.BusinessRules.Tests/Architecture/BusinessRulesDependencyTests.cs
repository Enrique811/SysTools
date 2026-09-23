using System.IO;

namespace SysTools.BusinessRules.Tests.Architecture;

public sealed class BusinessRulesDependencyTests
{
    [Fact]
    public void Business_and_entities_projects_do_not_reference_infrastructure_or_presentation()
    {
        var root = FindRoot();
        var projects = new[]
        {
            Path.Combine(root, "src", "Business", "SysTools.Business.csproj"),
            Path.Combine(root, "src", "Entities", "SysTools.Entities.csproj")
        };
        var forbidden = new[] { "SysTools.Data", "SysTools.Presentation", "FirebirdSql", "UseWPF" };

        foreach (var project in projects)
        {
            var content = File.ReadAllText(project);
            foreach (var value in forbidden)
            {
                Assert.DoesNotContain(value, content, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void New_business_rules_sources_do_not_use_infrastructure_ui_or_reports()
    {
        var root = FindRoot();
        var directories = new[]
        {
            Path.Combine(root, "src", "Business", "Products"),
            Path.Combine(root, "src", "Business", "Labels"),
            Path.Combine(root, "src", "Entities", "Labels")
        };
        var forbidden = new[]
        {
            "SysTools.Data", "SysTools.Presentation", "System.Windows", "FirebirdSql",
            "FbConnection", "FastReport", "ZXing", "System.IO"
        };

        foreach (var directory in directories)
        {
            Assert.True(Directory.Exists(directory), $"Missing planned directory: {directory}");
            foreach (var file in Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            {
                var content = File.ReadAllText(file);
                foreach (var value in forbidden)
                {
                    Assert.DoesNotContain(value, content, StringComparison.Ordinal);
                }
            }
        }
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SysTools.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Solution root not found.");
    }
}
