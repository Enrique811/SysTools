using System.Text;
using SysTools.Business.Licensing;
using SysTools.Data.Licensing;

namespace SysTools.Licensing.Tests.Data;

public sealed class FileLicenseSourceReaderTests
{
    [Fact]
    public async Task Reads_utf8_file_from_absolute_path()
    {
        using var file = TemporaryFile.Create(Encoding.UTF8.GetBytes("{\"ok\":true}"));
        var reader = new FileLicenseSourceReader();

        var result = await reader.ReadAsync(file.Path);

        Assert.True(result.Succeeded);
        Assert.Equal("{\"ok\":true}", result.Content);
    }

    [Fact]
    public async Task Resolves_relative_path_against_working_directory()
    {
        using var file = TemporaryFile.Create(Encoding.UTF8.GetBytes("{}"), AppContext.BaseDirectory);
        var reader = new FileLicenseSourceReader();
        var relative = Path.GetRelativePath(Environment.CurrentDirectory, file.Path);

        var result = await reader.ReadAsync(relative);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Missing_file_and_directory_are_not_found()
    {
        var reader = new FileLicenseSourceReader();

        var missing = await reader.ReadAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".lic"));
        var directory = await reader.ReadAsync(Path.GetTempPath());

        Assert.Equal(LicenseSourceFailure.NotFound, missing.Failure);
        Assert.Equal(LicenseSourceFailure.NotFound, directory.Failure);
    }

    [Fact]
    public async Task Rejects_file_larger_than_64_kib()
    {
        using var file = TemporaryFile.Create(new byte[(64 * 1024) + 1]);

        var result = await new FileLicenseSourceReader().ReadAsync(file.Path);

        Assert.Equal(LicenseSourceFailure.TooLarge, result.Failure);
    }

    [Fact]
    public async Task Rejects_invalid_utf8()
    {
        using var file = TemporaryFile.Create([0xC3, 0x28]);

        var result = await new FileLicenseSourceReader().ReadAsync(file.Path);

        Assert.Equal(LicenseSourceFailure.Unreadable, result.Failure);
    }

    [Fact]
    public async Task Propagates_requested_cancellation()
    {
        using var file = TemporaryFile.Create(Encoding.UTF8.GetBytes("{}"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            new FileLicenseSourceReader().ReadAsync(file.Path, cancellation.Token));
    }

    private sealed class TemporaryFile : IDisposable
    {
        private TemporaryFile(string path) => Path = path;

        internal string Path { get; }

        internal static TemporaryFile Create(byte[] content, string? directory = null)
        {
            directory ??= System.IO.Path.GetTempPath();
            var path = System.IO.Path.Combine(directory, Guid.NewGuid() + ".lic");
            File.WriteAllBytes(path, content);
            return new TemporaryFile(path);
        }

        public void Dispose()
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
    }
}
