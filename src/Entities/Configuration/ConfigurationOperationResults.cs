namespace SysTools.Entities.Configuration;

public enum ConfigurationLoadStatus
{
    Loaded,
    DefaultCreated,
    InvalidContent,
    SecretUnavailable,
    StorageFailure,
    AccessDenied,
    Canceled
}

public enum ConfigurationSaveStatus
{
    Saved,
    ValidationFailed,
    InvalidExistingContent,
    ProtectionFailure,
    StorageFailure,
    AccessDenied,
    Canceled
}

public enum ConfigurationRepositoryReadStatus
{
    Found,
    NotFound,
    InvalidContent,
    SecretUnavailable,
    StorageFailure,
    AccessDenied,
    Canceled
}

public enum ConfigurationRepositoryWriteStatus
{
    Saved,
    InvalidExistingContent,
    ProtectionFailure,
    StorageFailure,
    AccessDenied,
    Canceled
}

public sealed class ConfigurationLoadResult
{
    public ConfigurationLoadResult(
        ConfigurationLoadStatus status,
        AppConfiguration? configuration = null,
        ConfigurationValidationResult? validation = null,
        IReadOnlyList<ConfigurationIssue>? issues = null)
    {
        Status = status;
        Configuration = configuration;
        Validation = validation;
        Issues = issues ?? Array.Empty<ConfigurationIssue>();
    }

    public ConfigurationLoadStatus Status { get; }
    public AppConfiguration? Configuration { get; }
    public ConfigurationValidationResult? Validation { get; }
    public IReadOnlyList<ConfigurationIssue> Issues { get; }
}

public sealed class ConfigurationSaveResult
{
    public ConfigurationSaveResult(
        ConfigurationSaveStatus status,
        ConfigurationValidationResult validation,
        IReadOnlyList<ConfigurationIssue>? issues = null)
    {
        Status = status;
        Validation = validation;
        Issues = issues ?? Array.Empty<ConfigurationIssue>();
    }

    public ConfigurationSaveStatus Status { get; }
    public ConfigurationValidationResult Validation { get; }
    public IReadOnlyList<ConfigurationIssue> Issues { get; }
}

public sealed class ConfigurationRepositoryReadResult
{
    public ConfigurationRepositoryReadResult(
        ConfigurationRepositoryReadStatus status,
        AppConfiguration? configuration = null)
    {
        Status = status;
        Configuration = configuration;
    }

    public ConfigurationRepositoryReadStatus Status { get; }
    public AppConfiguration? Configuration { get; }
}

public sealed record ConfigurationRepositoryWriteResult(ConfigurationRepositoryWriteStatus Status);
