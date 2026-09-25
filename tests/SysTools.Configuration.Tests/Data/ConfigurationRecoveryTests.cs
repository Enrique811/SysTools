using SysTools.Data.Configuration;
using SysTools.Data.ConfigurationEditor;
using SysTools.Entities.Configuration;
using SysTools.Entities.ConfigurationEditor;

namespace SysTools.Configuration.Tests.Data;

public sealed class ConfigurationRecoveryTests
{
    [Fact]
    public async Task Recovery_backs_up_invalid_document_and_writes_new_document()
    {
        using var directory = new TempDirectory();
        var path = Path.Combine(directory.Path, "configuracion.json");
        await File.WriteAllTextAsync(path, "{invalid");
        var service = new ConfigurationRecoveryService(
            new PathStub(path), new ProtectorStub(), new Writer());

        var result = await service.RecoverAsync(new AppConfiguration
        { IpEmpresa="server", RutaEmpresa="db", Usuario="u", Password="SECRET", Ambiente="a", FormatoPrecio="MX", Columnas=1 });

        Assert.Equal(ConfigurationRecoveryStatus.Recovered, result.Status);
        Assert.Contains("passwordProtegido", await File.ReadAllTextAsync(path), StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET", await File.ReadAllTextAsync(path), StringComparison.Ordinal);
        Assert.Single(Directory.GetFiles(directory.Path, "*.bak"));
    }

    [Fact]
    public async Task Failed_recovery_restores_original_document_byte_for_byte()
    {
        using var directory = new TempDirectory();
        var path = Path.Combine(directory.Path, "configuracion.json");
        var original = new byte[] { 0, 1, 2, 127, 128, 255 };
        await File.WriteAllBytesAsync(path, original);
        var service = new ConfigurationRecoveryService(
            new PathStub(path), new ProtectorStub(), new FailingWriter());

        var result = await service.RecoverAsync(new AppConfiguration
        { IpEmpresa="server", RutaEmpresa="db", Usuario="u", Password="SECRET", Ambiente="a", FormatoPrecio="MX", Columnas=1 });

        Assert.Equal(ConfigurationRecoveryStatus.StorageFailure, result.Status);
        Assert.Equal(original, await File.ReadAllBytesAsync(path));
    }

    private sealed class PathStub(string path) : IConfigurationPathProvider { public string GetConfigurationFilePath()=>path; }
    private sealed class ProtectorStub : ISecretProtector { public string Protect(string value)=>Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value)); public string Unprotect(string value)=>throw new NotSupportedException(); }
    private sealed class Writer : IAtomicFileWriter { public Task WriteAsync(string path, ReadOnlyMemory<byte> content, CancellationToken token=default)=>File.WriteAllBytesAsync(path, content.ToArray(), token); }
    private sealed class FailingWriter : IAtomicFileWriter { public Task WriteAsync(string path, ReadOnlyMemory<byte> content, CancellationToken token=default)=>Task.FromException(new IOException("C:\\private\\disk unavailable")); }
    private sealed class TempDirectory : IDisposable { public string Path {get;}=System.IO.Path.Combine(System.IO.Path.GetTempPath(), "SysTools.Recovery", Guid.NewGuid().ToString("N")); public TempDirectory()=>Directory.CreateDirectory(Path); public void Dispose(){if(Directory.Exists(Path))Directory.Delete(Path,true);} }
}
