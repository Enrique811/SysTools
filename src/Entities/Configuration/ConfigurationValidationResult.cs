namespace SysTools.Entities.Configuration;

public sealed class ConfigurationValidationResult
{
    public ConfigurationValidationResult(
        bool isPersistable,
        bool isConnectionReady,
        IReadOnlyList<ConfigurationIssue>? issues = null)
    {
        IsPersistable = isPersistable;
        IsConnectionReady = isConnectionReady;
        Issues = issues ?? Array.Empty<ConfigurationIssue>();
    }

    public bool IsPersistable { get; }

    public bool IsConnectionReady { get; }

    public IReadOnlyList<ConfigurationIssue> Issues { get; }
}
