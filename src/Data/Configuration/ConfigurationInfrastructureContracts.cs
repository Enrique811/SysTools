namespace SysTools.Data.Configuration;

public interface IConfigurationPathProvider
{
    string GetConfigurationFilePath();
}

public interface ISecretProtector
{
    string Protect(string plaintext);

    string Unprotect(string protectedValue);
}

public interface IAtomicFileWriter
{
    Task WriteAsync(string destinationPath, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default);
}
