using System.Security.Cryptography;
using System.Text.Json;
using SysTools.Data.Reports;
using SysTools.Entities.Labels;

namespace SysTools.Configuration.Tests.Reports;

public sealed class ManagedLabelTemplateProviderTests
{
    [Fact]
    public async Task Resolves_safe_matching_template_and_rejects_traversal_capacity_and_malformed_json()
    {
        using var directory = new TempDirectory();
        var templatePath = Path.Combine(directory.Path,"label-2.label.json");
        await File.WriteAllTextAsync(templatePath, "{\"id\":\"label-2.label.json\",\"version\":\"1\",\"capacity\":2,\"cellWidthMm\":57,\"pageHeightMm\":40,\"dpi\":300,\"productionApproved\":false}");
        await WriteManifestAsync(directory.Path, "label-2.label.json", Hash(templatePath));
        await File.WriteAllTextAsync(Path.Combine(directory.Path,"bad.label.json"), "{bad");
        var provider = new ManagedLabelTemplateProvider(directory.Path);
        var available = await provider.ResolveAsync("label-2.label.json",2);
        Assert.Equal(LabelTemplateStatus.Available, available.Status);
        Assert.Matches("^[0-9A-F]{64}$", available.Template!.Sha256);
        Assert.Equal(LabelTemplateStatus.Incompatible, (await provider.ResolveAsync("label-2.label.json",1)).Status);
        Assert.Equal(LabelTemplateStatus.Incompatible, (await provider.ResolveAsync("../label-2.label.json",2)).Status);
        Assert.Equal(LabelTemplateStatus.Invalid, (await provider.ResolveAsync("bad.label.json",2)).Status);
        await File.WriteAllBytesAsync(Path.Combine(directory.Path,"large.label.json"), new byte[65 * 1024]);
        Assert.Equal(LabelTemplateStatus.Invalid, (await provider.ResolveAsync("large.label.json",1)).Status);
    }

    [Fact]
    public async Task Rejects_missing_or_tampered_manifest_hash()
    {
        using var directory = new TempDirectory();
        var templatePath = Path.Combine(directory.Path, "label-1.label.json");
        await File.WriteAllTextAsync(templatePath, "{\"id\":\"label-1.label.json\",\"version\":\"1\",\"capacity\":1,\"cellWidthMm\":57,\"pageHeightMm\":40,\"dpi\":300,\"productionApproved\":false}");
        var provider = new ManagedLabelTemplateProvider(directory.Path);

        Assert.Equal(LabelTemplateStatus.Invalid, (await provider.ResolveAsync("label-1.label.json", 1)).Status);
        await WriteManifestAsync(directory.Path, "label-1.label.json", new string('0', 64));
        Assert.Equal(LabelTemplateStatus.Invalid, (await provider.ResolveAsync("label-1.label.json", 1)).Status);
    }

    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static Task WriteManifestAsync(string root, string id, string hash) => File.WriteAllTextAsync(
        Path.Combine(root, "manifest.json"),
        JsonSerializer.Serialize(new { format = "managed-label-templates-v1", templates = new Dictionary<string, string> { [id] = hash } }));

    private sealed class TempDirectory : IDisposable
    { public string Path {get;}=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"SysTools.Report",Guid.NewGuid().ToString("N")); public TempDirectory()=>Directory.CreateDirectory(Path); public void Dispose(){if(Directory.Exists(Path))Directory.Delete(Path,true);} }
}
