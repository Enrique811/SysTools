using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Configuration.Tests.TestDoubles;
using SysTools.Data.Configuration;
using SysTools.Entities.Configuration;

namespace SysTools.Configuration.Tests.Data;

public sealed class ConfigurationMalformedContentTests
{
    public static TheoryData<byte[]> InvalidDocuments => new()
    {
        Array.Empty<byte>(),
        "{"u8.ToArray(),
        "{\"columnas\":\"dos\"}"u8.ToArray(),
        Enumerable.Repeat((byte)'x', 1024 * 1024 + 1).ToArray()
    };

    [Theory]
    [MemberData(nameof(InvalidDocuments))]
    [Trait("Category", "Failure")]
    public async Task InvalidDocument_IsReportedAndPreserved(byte[] original)
    {
        using var temporary = new TemporaryDirectory();
        var path = temporary.File();
        await File.WriteAllBytesAsync(path, original);
        var repository = CreateRepository(path, new AtomicFileWriter());

        var result = await repository.ReadAsync();

        Assert.Equal(ConfigurationRepositoryReadStatus.InvalidContent, result.Status);
        Assert.Null(result.Configuration);
        Assert.Equal(original, await File.ReadAllBytesAsync(path));
    }

    [Fact]
    [Trait("Category", "Failure")]
    public async Task SaveAgainstMalformedDocument_DoesNotSilentlyReplaceIt()
    {
        using var temporary = new TemporaryDirectory();
        var path = temporary.File();
        var original = "{malformed"u8.ToArray();
        await File.WriteAllBytesAsync(path, original);
        var repository = CreateRepository(path, new AtomicFileWriter());

        var result = await repository.WriteAsync(AppConfiguration.CreateDefault());

        Assert.Equal(ConfigurationRepositoryWriteStatus.InvalidExistingContent, result.Status);
        Assert.Equal(original, await File.ReadAllBytesAsync(path));
    }

    private static JsonConfigurationRepository CreateRepository(string path, IAtomicFileWriter writer) => new(
        new FakePathProvider(path),
        new FakeSecretProtector(),
        writer,
        NullLogger<JsonConfigurationRepository>.Instance);
}
