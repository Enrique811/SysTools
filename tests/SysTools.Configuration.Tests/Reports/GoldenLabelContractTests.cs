using System.Security.Cryptography;
using System.Text.Json;

namespace SysTools.Configuration.Tests.Reports;

public sealed class GoldenLabelContractTests
{
    [Fact]
    public void Golden_manifest_is_versioned_and_managed_templates_have_stable_sha256()
    {
        var root = FindRoot();
        var goldenManifestPath = Path.Combine(root, "tests", "SysTools.Configuration.Tests", "Reports", "Assets", "Goldens", "manifest.json");
        var runtimeManifestPath = Path.Combine(root, "src", "Reports", "Templates", "manifest.json");
        Assert.True(File.Exists(goldenManifestPath));
        Assert.True(File.Exists(runtimeManifestPath));
        using var goldenManifest = JsonDocument.Parse(File.ReadAllBytes(goldenManifestPath));
        using var runtimeManifest = JsonDocument.Parse(File.ReadAllBytes(runtimeManifestPath));
        var hashes = Enumerable.Range(1, 3).Select(capacity =>
        {
            var id = $"label-{capacity}.label.json";
            var bytes = File.ReadAllBytes(Path.Combine(root, "src", "Reports", "Templates", id));
            var actual = Convert.ToHexString(SHA256.HashData(bytes));
            Assert.Equal(actual, goldenManifest.RootElement.GetProperty("templates").GetProperty(id).GetString());
            Assert.Equal(actual, runtimeManifest.RootElement.GetProperty("templates").GetProperty(id).GetString());
            return actual;
        }).ToArray();
        Assert.Equal(3, hashes.Distinct(StringComparer.Ordinal).Count());
        Assert.All(hashes, hash => Assert.Matches("^[0-9A-F]{64}$", hash));
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SysTools.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Solution root not found.");
    }
}
