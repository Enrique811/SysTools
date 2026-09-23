namespace SysTools.Entities.Configuration;

public enum ConfigurationIssueCode
{
    RequiredForConnection,
    UnsupportedEnvironment,
    UnsupportedPriceFormat,
    UnsupportedColumnCount,
    ConfigurationNotFound,
    MalformedConfiguration,
    SecretUnavailable,
    StorageUnavailable,
    AccessDenied,
    OperationCanceled
}

public enum ConfigurationIssueSeverity
{
    Warning,
    Error
}

public sealed record ConfigurationIssue(
    ConfigurationIssueCode Code,
    string? Field,
    string Message,
    ConfigurationIssueSeverity Severity);
