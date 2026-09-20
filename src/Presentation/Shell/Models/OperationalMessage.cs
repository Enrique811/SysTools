namespace SysTools.Presentation.Shell.Models;

public enum AvailabilityStatus
{
    Unavailable,
    Pending,
    Available,
    Error
}

public enum MessageSeverity
{
    Information,
    Warning,
    Error
}

public sealed record OperationalMessage
{
    public OperationalMessage(string text, MessageSeverity severity)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Message text is required.", nameof(text));
        }

        Text = text;
        Severity = severity;
        AccessibilityLabel = $"{GetSeverityLabel(severity)}: {text}";
    }

    public string Text { get; }

    public MessageSeverity Severity { get; }

    public string AccessibilityLabel { get; }

    private static string GetSeverityLabel(MessageSeverity severity) => severity switch
    {
        MessageSeverity.Information => "Información",
        MessageSeverity.Warning => "Advertencia",
        MessageSeverity.Error => "Error",
        _ => "Estado"
    };
}
