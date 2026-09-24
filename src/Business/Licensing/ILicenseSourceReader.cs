namespace SysTools.Business.Licensing;

public interface ILicenseSourceReader
{
    Task<LicenseSourceReadResult> ReadAsync(
        string path,
        CancellationToken cancellationToken = default);
}

public enum LicenseSourceFailure
{
    None = 0,
    NotFound = 1,
    Unreadable = 2,
    TooLarge = 3
}

public sealed record LicenseSourceReadResult
{
    private LicenseSourceReadResult(
        bool succeeded,
        string? content,
        LicenseSourceFailure failure)
    {
        Succeeded = succeeded;
        Content = content;
        Failure = failure;
    }

    public bool Succeeded { get; }

    public string? Content { get; }

    public LicenseSourceFailure Failure { get; }

    public static LicenseSourceReadResult Success(string content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return new LicenseSourceReadResult(true, content, LicenseSourceFailure.None);
    }

    public static LicenseSourceReadResult Failed(LicenseSourceFailure failure)
    {
        if (failure == LicenseSourceFailure.None)
        {
            throw new ArgumentException("Un fallo requiere una causa.", nameof(failure));
        }

        return new LicenseSourceReadResult(false, null, failure);
    }
}
