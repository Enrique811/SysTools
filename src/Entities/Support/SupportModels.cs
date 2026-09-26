using System.Collections.ObjectModel;

namespace SysTools.Entities.Support;

public enum SupportMailKind { LicenseRequest, ErrorReport }
public enum SupportChannel { DefaultClient, GmailWeb, OutlookWeb }
public enum SupportActionStatus { Ready, Opened, InvalidInput, HardwareUnavailable, Busy, LaunchFailed, Canceled, Stale }
public enum ExternalLaunchStatus { Opened, Failed, Canceled }
public enum LogCatalogStatus { Available, Empty, DirectoryUnavailable, AccessDenied, Failed, Canceled }
public enum LogPreviewStatus { Loaded, Missing, InvalidIdentifier, TooLarge, AccessDenied, InvalidContent, Failed, Canceled }

public sealed record SupportMailDraft
{
    public const int MaximumSubjectLength = 160;
    public const int MaximumBodyLength = 6000;

    public SupportMailDraft(SupportMailKind kind, string? recipient, string subject, string body, string diagnosticId)
    {
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        recipient ??= string.Empty;
        if (recipient.Length > 254 || recipient.ContainsAny('\r', '\n')) throw new ArgumentException("Invalid recipient.", nameof(recipient));
        if (string.IsNullOrWhiteSpace(subject) || subject.Length > MaximumSubjectLength || subject.ContainsAny('\r', '\n'))
            throw new ArgumentException("Subject must contain 1..160 characters on one line.", nameof(subject));
        if (string.IsNullOrWhiteSpace(body) || body.Length > MaximumBodyLength)
            throw new ArgumentException("Body must contain 1..6000 characters.", nameof(body));
        if (!Guid.TryParseExact(diagnosticId, "N", out _)) throw new ArgumentException("Opaque diagnostic id required.", nameof(diagnosticId));
        Kind = kind; Recipient = recipient; Subject = subject; Body = body; DiagnosticId = diagnosticId;
    }

    public SupportMailKind Kind { get; }
    public string Recipient { get; }
    public string Subject { get; }
    public string Body { get; }
    public string DiagnosticId { get; }
}

public sealed record SupportInitializationResult
{
    public SupportInitializationResult(bool hardwareAvailable, string? hardwareId, string message)
    {
        HardwareId = hardwareId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message required.", nameof(message));
        if (hardwareAvailable && string.IsNullOrWhiteSpace(HardwareId)) throw new ArgumentException("Hardware id required when available.", nameof(hardwareId));
        if (!hardwareAvailable && HardwareId.Length != 0) throw new ArgumentException("Unavailable hardware cannot expose an id.", nameof(hardwareId));
        HardwareAvailable = hardwareAvailable; Message = message;
    }
    public bool HardwareAvailable { get; }
    public string HardwareId { get; }
    public string Message { get; }
}

public sealed record ExternalLaunchResult(ExternalLaunchStatus Status);

public sealed record SupportActionResult
{
    public SupportActionResult(SupportActionStatus status, string message, bool canRetry = false, SupportMailDraft? draft = null)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("Message required.", nameof(message));
        if (draft is not null && status != SupportActionStatus.Ready) throw new ArgumentException("Draft only valid for Ready.", nameof(draft));
        Status = status; Message = message; CanRetry = canRetry; Draft = draft;
    }
    public SupportActionStatus Status { get; }
    public string Message { get; }
    public bool CanRetry { get; }
    public SupportMailDraft? Draft { get; }
}

public sealed record LogFileSummary
{
    public const long MaximumFileBytes = 50L * 1024 * 1024;
    public LogFileSummary(string id, DateTime lastWriteUtc, long length)
    {
        if (string.IsNullOrWhiteSpace(id) || id.Length > 255 || Path.GetFileName(id) != id || !id.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Safe log identifier required.", nameof(id));
        if (length is < 0 or > MaximumFileBytes) throw new ArgumentOutOfRangeException(nameof(length));
        Id = id; LastWriteUtc = lastWriteUtc.ToUniversalTime(); Length = length;
    }
    public string Id { get; }
    public DateTime LastWriteUtc { get; }
    public long Length { get; }
}

public sealed record LogCatalogResult
{
    public LogCatalogResult(LogCatalogStatus status, IEnumerable<LogFileSummary>? files = null)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        var values = files?.ToArray() ?? [];
        if (values.Length > 20) throw new ArgumentOutOfRangeException(nameof(files));
        if (status == LogCatalogStatus.Available && values.Length == 0) throw new ArgumentException("Available catalog requires files.", nameof(files));
        if (status != LogCatalogStatus.Available && values.Length != 0) throw new ArgumentException("Only an available catalog can expose files.", nameof(files));
        Status = status; Files = new ReadOnlyCollection<LogFileSummary>(values);
    }
    public LogCatalogStatus Status { get; }
    public IReadOnlyList<LogFileSummary> Files { get; }
}

public sealed record LogPreviewResult
{
    public const int MaximumBytes = 256 * 1024;
    public const int MaximumLines = 500;
    public LogPreviewResult(LogPreviewStatus status, string fileId = "", string content = "", bool isTruncated = false)
    {
        if (!Enum.IsDefined(status)) throw new ArgumentOutOfRangeException(nameof(status));
        if (status == LogPreviewStatus.Loaded)
        {
            if (string.IsNullOrWhiteSpace(fileId) || Path.GetFileName(fileId) != fileId) throw new ArgumentException("Safe file id required.", nameof(fileId));
            if (System.Text.Encoding.UTF8.GetByteCount(content) > MaximumBytes) throw new ArgumentOutOfRangeException(nameof(content));
            if (CountLines(content) > MaximumLines) throw new ArgumentOutOfRangeException(nameof(content));
        }
        else if (fileId.Length != 0 || content.Length != 0) throw new ArgumentException("Failed preview cannot expose file data.");
        Status = status; FileId = fileId; Content = content; IsTruncated = isTruncated;
    }
    public LogPreviewStatus Status { get; }
    public string FileId { get; }
    public string Content { get; }
    public bool IsTruncated { get; }
    private static int CountLines(string value) => value.Length == 0 ? 0 : value.Count(character => character == '\n') + 1;
}

file static class StringValidationExtensions
{
    public static bool ContainsAny(this string value, params char[] characters) => value.IndexOfAny(characters) >= 0;
}
