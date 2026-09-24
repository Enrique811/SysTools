using System.Text;
using SysTools.Business.Licensing;

namespace SysTools.Data.Licensing;

public sealed class FileLicenseSourceReader : ILicenseSourceReader
{
    internal const int MaximumBytes = 64 * 1024;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public async Task<LicenseSourceReadResult> ReadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return LicenseSourceReadResult.Failed(LicenseSourceFailure.NotFound);
            }

            var fullPath = Path.GetFullPath(path.Trim(), Environment.CurrentDirectory);
            var info = new FileInfo(fullPath);
            if (!info.Exists)
            {
                return LicenseSourceReadResult.Failed(LicenseSourceFailure.NotFound);
            }

            if (info.Length > MaximumBytes)
            {
                return LicenseSourceReadResult.Failed(LicenseSourceFailure.TooLarge);
            }

            await using var stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            if (stream.Length > MaximumBytes)
            {
                return LicenseSourceReadResult.Failed(LicenseSourceFailure.TooLarge);
            }

            var bytes = new byte[checked((int)stream.Length)];
            var offset = 0;
            while (offset < bytes.Length)
            {
                var count = await stream.ReadAsync(
                    bytes.AsMemory(offset),
                    cancellationToken).ConfigureAwait(false);
                if (count == 0)
                {
                    break;
                }

                offset += count;
            }

            if (offset != bytes.Length)
            {
                return LicenseSourceReadResult.Failed(LicenseSourceFailure.Unreadable);
            }

            return LicenseSourceReadResult.Success(StrictUtf8.GetString(bytes));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            return LicenseSourceReadResult.Failed(LicenseSourceFailure.Unreadable);
        }
        catch (DecoderFallbackException)
        {
            return LicenseSourceReadResult.Failed(LicenseSourceFailure.Unreadable);
        }
        catch (IOException)
        {
            return LicenseSourceReadResult.Failed(LicenseSourceFailure.Unreadable);
        }
        catch (ArgumentException)
        {
            return LicenseSourceReadResult.Failed(LicenseSourceFailure.Unreadable);
        }
        catch (NotSupportedException)
        {
            return LicenseSourceReadResult.Failed(LicenseSourceFailure.Unreadable);
        }
    }
}
