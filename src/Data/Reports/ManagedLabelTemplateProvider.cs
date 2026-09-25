using System.Text.Json;
using System.Security.Cryptography;
using SysTools.Business.Labels;
using SysTools.Entities.Labels;

namespace SysTools.Data.Reports;

public sealed class ManagedLabelTemplateProvider : ILabelTemplateProvider
{
    private const long MaximumBytes = 64 * 1024;
    private const string ManifestName = "manifest.json";
    private readonly string _root;
    public ManagedLabelTemplateProvider() : this(Path.Combine(AppContext.BaseDirectory, "Reports", "Templates")) { }
    public ManagedLabelTemplateProvider(string root) => _root = Path.GetFullPath(root);

    public async Task<LabelTemplateResult> ResolveAsync(string templateId, int capacity, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(templateId) || Path.GetFileName(templateId) != templateId
                || !templateId.EndsWith(".label.json", StringComparison.OrdinalIgnoreCase))
                return new(LabelTemplateStatus.Incompatible);
            var path = Path.GetFullPath(Path.Combine(_root, templateId));
            var prefix = _root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return new(LabelTemplateStatus.Incompatible);
            if (!File.Exists(path)) return new(LabelTemplateStatus.Missing);
            if (new FileInfo(path).Length is < 2 or > MaximumBytes) return new(LabelTemplateStatus.Invalid);
            var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
            var hash = Convert.ToHexString(SHA256.HashData(bytes));
            if (!await IsManifestHashValidAsync(templateId, hash, cancellationToken).ConfigureAwait(false))
                return new(LabelTemplateStatus.Invalid);
            await using var stream = new MemoryStream(bytes, writable: false);
            var value = await JsonSerializer.DeserializeAsync<TemplateDocument>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken).ConfigureAwait(false);
            if (value is null || !string.Equals(value.Id, templateId, StringComparison.Ordinal) || value.Capacity != capacity)
                return new(LabelTemplateStatus.Incompatible);
            var descriptor = new LabelTemplateDescriptor(value.Id, value.Version, value.Capacity, value.CellWidthMm, value.PageHeightMm, value.Dpi, value.ProductionApproved, hash);
            return new(LabelTemplateStatus.Available, descriptor);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        { return new(LabelTemplateStatus.Invalid); }
    }

    private async Task<bool> IsManifestHashValidAsync(string templateId, string actualHash, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_root, ManifestName);
        if (!File.Exists(path) || new FileInfo(path).Length is < 2 or > MaximumBytes) return false;
        await using var stream = File.OpenRead(path);
        var manifest = await JsonSerializer.DeserializeAsync<TemplateManifestDocument>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken).ConfigureAwait(false);
        if (manifest?.Format != "managed-label-templates-v1" || manifest.Templates is null
            || !manifest.Templates.TryGetValue(templateId, out var expectedHash)
            || expectedHash is null || expectedHash.Length != 64) return false;
        try
        {
            return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(expectedHash), Convert.FromHexString(actualHash));
        }
        catch (FormatException) { return false; }
    }

    private sealed record TemplateDocument(string Id, string Version, int Capacity, decimal CellWidthMm, decimal PageHeightMm, int Dpi, bool ProductionApproved);
    private sealed record TemplateManifestDocument(string Format, Dictionary<string, string> Templates);
}
