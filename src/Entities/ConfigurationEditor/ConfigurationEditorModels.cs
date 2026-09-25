using SysTools.Entities.Configuration;
using SysTools.Entities.Connection;
using SysTools.Entities.Licensing;

namespace SysTools.Entities.ConfigurationEditor;

public sealed record OperationalOption(string Id, string DisplayName, bool IsAvailable = true)
{
    public string Id { get; } = string.IsNullOrWhiteSpace(Id)
        ? throw new ArgumentException("Option id is required.", nameof(Id))
        : Id;
    public string DisplayName { get; } = string.IsNullOrWhiteSpace(DisplayName)
        ? throw new ArgumentException("Display name is required.", nameof(DisplayName))
        : DisplayName;
}

public enum CatalogStatus { Available, Empty, Unavailable }

public sealed class CatalogResult
{
    public CatalogResult(CatalogStatus status, IEnumerable<OperationalOption>? options = null)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        var snapshot = options?.ToArray() ?? [];
        if (status == CatalogStatus.Available && snapshot.Length == 0)
            throw new ArgumentException("Available catalog requires options.", nameof(options));
        if (status != CatalogStatus.Available && snapshot.Length != 0)
            throw new ArgumentException("Non-available catalog cannot contain options.", nameof(options));
        Status = status;
        Options = Array.AsReadOnly(snapshot);
    }

    public CatalogStatus Status { get; }
    public IReadOnlyList<OperationalOption> Options { get; }
}

public sealed record HardwareIdentityResult(bool IsAvailable, string? Value)
{
    public static HardwareIdentityResult Available(string value) =>
        new(true, string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("UUID required.", nameof(value)) : value);
    public static HardwareIdentityResult Unavailable() => new(false, null);
}

public sealed record LicenseSummary(
    LicenseValidationStatus Status,
    string Message,
    LicenseIssuer Issuer = LicenseIssuer.None,
    DateTime? ValidFrom = null,
    DateTime? ValidUntil = null)
{
    public bool IsValid => Status == LicenseValidationStatus.Valid;
}

public enum ConfigurationEditorOpenStatus { Ready, RecoveryRequired, OperationalFailure }

public sealed class ConfigurationEditorSnapshot
{
    public ConfigurationEditorSnapshot(
        ConfigurationEditorOpenStatus status,
        ConfigurationDraft draft,
        bool hasStoredPassword,
        bool hasStoredLicense,
        bool requiresConnectionTest,
        IEnumerable<ConfigurationIssue>? issues,
        IEnumerable<OperationalOption>? printers,
        IEnumerable<OperationalOption>? templates,
        HardwareIdentityResult hardware)
    {
        Status = status;
        Draft = draft ?? throw new ArgumentNullException(nameof(draft));
        HasStoredPassword = hasStoredPassword;
        HasStoredLicense = hasStoredLicense;
        RequiresConnectionTest = requiresConnectionTest;
        Issues = Array.AsReadOnly((issues ?? []).ToArray());
        Printers = Array.AsReadOnly((printers ?? []).ToArray());
        Templates = Array.AsReadOnly((templates ?? []).ToArray());
        Hardware = hardware ?? throw new ArgumentNullException(nameof(hardware));
    }

    public ConfigurationEditorOpenStatus Status { get; }
    public ConfigurationDraft Draft { get; }
    public bool HasStoredPassword { get; }
    public bool HasStoredLicense { get; }
    public bool RequiresConnectionTest { get; }
    public IReadOnlyList<ConfigurationIssue> Issues { get; }
    public IReadOnlyList<OperationalOption> Printers { get; }
    public IReadOnlyList<OperationalOption> Templates { get; }
    public HardwareIdentityResult Hardware { get; }
}

public sealed record ConnectionEditorResult(ConnectionTestResult Result, string? ProofToken)
{
    public bool IsSuccess => Result.Status == ConnectionTestStatus.Success && ProofToken is not null;
}

public enum LicenseChange { KeepExisting, ReplaceValidated, ClearExplicitly }

public sealed record LicenseEditorResult(LicenseSummary Summary);

public enum ConfigurationEditorSaveStatus { Saved, ValidationFailed, TestRequired, RecoveryRequired, OperationalFailure, Stale }

public sealed class ConfigurationEditorSaveResult
{
    public ConfigurationEditorSaveResult(
        ConfigurationEditorSaveStatus status,
        string message,
        IEnumerable<ConfigurationIssue>? issues = null)
    {
        Status = status;
        Message = string.IsNullOrWhiteSpace(message) ? throw new ArgumentException("Message required.", nameof(message)) : message;
        Issues = Array.AsReadOnly((issues ?? []).ToArray());
    }
    public ConfigurationEditorSaveStatus Status { get; }
    public string Message { get; }
    public IReadOnlyList<ConfigurationIssue> Issues { get; }
    public bool IsSaved => Status == ConfigurationEditorSaveStatus.Saved;
}
