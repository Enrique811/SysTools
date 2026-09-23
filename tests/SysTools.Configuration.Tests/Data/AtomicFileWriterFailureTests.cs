using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Configuration.Tests.TestDoubles;
using SysTools.Data.Configuration;
using SysTools.Entities.Configuration;

namespace SysTools.Configuration.Tests.Data;

public sealed class AtomicFileWriterFailureTests
{
    [Fact]
    [Trait("Category", "Failure")]
    public async Task InjectedWriteFailure_PreservesExistingDocumentByteForByte()
    {
        using var temporary = new TemporaryDirectory();
        var path = temporary.File();
        var initialRepository = CreateRepository(path, new AtomicFileWriter());
        Assert.Equal(
            ConfigurationRepositoryWriteStatus.Saved,
            (await initialRepository.WriteAsync(AppConfiguration.CreateDefault())).Status);
        var original = await File.ReadAllBytesAsync(path);
        var failingWriter = new FakeAtomicFileWriter { ExceptionToThrow = new IOException("disk unavailable") };
        var failingRepository = CreateRepository(path, failingWriter);

        var result = await failingRepository.WriteAsync(new AppConfiguration { Informacion = "changed" });

        Assert.Equal(ConfigurationRepositoryWriteStatus.StorageFailure, result.Status);
        Assert.Equal(original, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    [Trait("Category", "Failure")]
    public async Task AccessDenied_IsDistinctAndPreservesExistingDocument()
    {
        using var temporary = new TemporaryDirectory();
        var path = temporary.File();
        var initialRepository = CreateRepository(path, new AtomicFileWriter());
        await initialRepository.WriteAsync(AppConfiguration.CreateDefault());
        var original = await File.ReadAllBytesAsync(path);
        var writer = new FakeAtomicFileWriter { ExceptionToThrow = new UnauthorizedAccessException("denied") };

        var result = await CreateRepository(path, writer).WriteAsync(AppConfiguration.CreateDefault());

        Assert.Equal(ConfigurationRepositoryWriteStatus.AccessDenied, result.Status);
        Assert.Equal(original, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    [Trait("Category", "Failure")]
    public async Task CanceledAtomicWrite_PreservesExistingDocumentAndCleansTemporaryFiles()
    {
        using var temporary = new TemporaryDirectory();
        var path = temporary.File();
        var original = "original"u8.ToArray();
        await File.WriteAllBytesAsync(path, original);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new AtomicFileWriter().WriteAsync(path, "replacement"u8.ToArray(), cancellation.Token));

        Assert.Equal(original, await File.ReadAllBytesAsync(path));
        Assert.Empty(Directory.GetFiles(temporary.Path, "*.tmp"));
    }

    private static JsonConfigurationRepository CreateRepository(string path, IAtomicFileWriter writer) => new(
        new FakePathProvider(path),
        new FakeSecretProtector(),
        writer,
        NullLogger<JsonConfigurationRepository>.Instance);
}
