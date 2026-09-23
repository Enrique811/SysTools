namespace SysTools.Entities.Connection;

public sealed class ConnectionTestResult
{
    public ConnectionTestResult(
        ConnectionTestStatus status,
        string message,
        TimeSpan duration,
        IEnumerable<ConnectionConfigurationIssue>? issues = null)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("El mensaje es obligatorio.", nameof(message));
        }

        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        var materializedIssues = issues?.ToArray() ?? [];
        if (status == ConnectionTestStatus.InvalidConfiguration && materializedIssues.Length == 0)
        {
            throw new ArgumentException(
                "Una configuración inválida debe incluir al menos un problema.",
                nameof(issues));
        }

        if (status != ConnectionTestStatus.InvalidConfiguration && materializedIssues.Length != 0)
        {
            throw new ArgumentException(
                "Solo una configuración inválida puede incluir problemas.",
                nameof(issues));
        }

        Status = status;
        Message = message;
        Duration = duration;
        Issues = Array.AsReadOnly(materializedIssues);
    }

    public ConnectionTestStatus Status { get; }

    public string Message { get; }

    public TimeSpan Duration { get; }

    public IReadOnlyList<ConnectionConfigurationIssue> Issues { get; }

    public override string ToString() =>
        $"{nameof(ConnectionTestResult)} {{ Status = {Status}, Duration = {Duration} }}";
}
