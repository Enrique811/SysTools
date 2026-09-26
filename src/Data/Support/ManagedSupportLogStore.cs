using System.Security;
using System.Text;
using SysTools.Business.Support;
using SysTools.Entities.Support;

namespace SysTools.Data.Support;

public sealed class ManagedSupportLogStore : ISupportLogStore
{
    private readonly string _root;
    public ManagedSupportLogStore() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SysTools", "Logs")) { }
    public ManagedSupportLogStore(string root) => _root = Path.GetFullPath(root ?? throw new ArgumentNullException(nameof(root)));

    public Task<LogCatalogResult> GetCatalogAsync(CancellationToken cancellationToken = default) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Directory.Exists(_root)) return new LogCatalogResult(LogCatalogStatus.DirectoryUnavailable);
        try
        {
            var files = Directory.EnumerateFiles(_root, "*.log", SearchOption.TopDirectoryOnly)
                .Select(path => TryDescribe(path))
                .Where(summary => summary is not null)
                .Cast<LogFileSummary>()
                .OrderByDescending(summary => summary.LastWriteUtc)
                .ThenBy(summary => summary.Id, StringComparer.OrdinalIgnoreCase)
                .Take(20)
                .ToArray();
            return files.Length == 0 ? new(LogCatalogStatus.Empty) : new(LogCatalogStatus.Available, files);
        }
        catch (UnauthorizedAccessException) { return new(LogCatalogStatus.AccessDenied); }
        catch (SecurityException) { return new(LogCatalogStatus.AccessDenied); }
        catch (IOException) { return new(LogCatalogStatus.Failed); }
    }, cancellationToken);

    public Task<LogPreviewResult> ReadAsync(string fileId, CancellationToken cancellationToken = default) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!IsSafeId(fileId)) return new LogPreviewResult(LogPreviewStatus.InvalidIdentifier);
        var path = Path.GetFullPath(Path.Combine(_root, fileId));
        if (!IsContained(path)) return new(LogPreviewStatus.InvalidIdentifier);
        try
        {
            if (!File.Exists(path)) return new(LogPreviewStatus.Missing);
            var info = new FileInfo(path);
            if ((info.Attributes & FileAttributes.ReparsePoint) != 0) return new(LogPreviewStatus.InvalidIdentifier);
            if (info.Length > LogFileSummary.MaximumFileBytes) return new(LogPreviewStatus.TooLarge);
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete,
                4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
            var count = (int)Math.Min(stream.Length, LogPreviewResult.MaximumBytes);
            var offset = stream.Length - count;
            stream.Seek(offset, SeekOrigin.Begin);
            var bytes = new byte[count];
            var read = 0;
            while (read < count)
            {
                var current = stream.Read(bytes, read, count - read);
                if (current == 0) break;
                read += current;
            }
            var decoded = DecodeUtf8(bytes.AsSpan(0, read), offset > 0);
            if (decoded is null) return new(LogPreviewStatus.InvalidContent);
            var lines = decoded.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n');
            var truncated = offset > 0 || lines.Length > LogPreviewResult.MaximumLines;
            var selected = lines.TakeLast(LogPreviewResult.MaximumLines);
            var content = string.Join(Environment.NewLine, selected);
            return new(LogPreviewStatus.Loaded, fileId, content, truncated);
        }
        catch (UnauthorizedAccessException) { return new(LogPreviewStatus.AccessDenied); }
        catch (SecurityException) { return new(LogPreviewStatus.AccessDenied); }
        catch (FileNotFoundException) { return new(LogPreviewStatus.Missing); }
        catch (DirectoryNotFoundException) { return new(LogPreviewStatus.Missing); }
        catch (IOException) { return new(LogPreviewStatus.Failed); }
    }, cancellationToken);

    private LogFileSummary? TryDescribe(string path)
    {
        var full = Path.GetFullPath(path);
        if (!IsContained(full)) return null;
        var info = new FileInfo(full);
        if ((info.Attributes & FileAttributes.ReparsePoint) != 0 || info.Length > LogFileSummary.MaximumFileBytes) return null;
        return new(info.Name, info.LastWriteTimeUtc, info.Length);
    }

    private bool IsContained(string path)
    {
        var prefix = _root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            && string.Equals(Path.GetDirectoryName(path), _root, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSafeId(string? id) => !string.IsNullOrWhiteSpace(id) && id.Length <= 255
        && Path.GetFileName(id) == id && id.EndsWith(".log", StringComparison.OrdinalIgnoreCase);

    private static string? DecodeUtf8(ReadOnlySpan<byte> bytes, bool mayStartMidCharacter)
    {
        var encoding = new UTF8Encoding(false, true);
        var attempts = mayStartMidCharacter ? Math.Min(4, bytes.Length + 1) : 1;
        for (var skip = 0; skip < attempts; skip++)
        {
            try { return encoding.GetString(bytes[skip..]); }
            catch (DecoderFallbackException) { }
        }
        return null;
    }
}

