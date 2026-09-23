namespace SysTools.Data.Configuration;

public sealed class AppDataConfigurationPathProvider : IConfigurationPathProvider
{
    private readonly Func<string> _applicationDataResolver;

    public AppDataConfigurationPathProvider()
        : this(() => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))
    {
    }

    public AppDataConfigurationPathProvider(Func<string> applicationDataResolver)
    {
        _applicationDataResolver = applicationDataResolver ??
            throw new ArgumentNullException(nameof(applicationDataResolver));
    }

    public string GetConfigurationFilePath()
    {
        var applicationData = _applicationDataResolver();
        if (string.IsNullOrWhiteSpace(applicationData))
        {
            throw new InvalidOperationException("No fue posible resolver la carpeta AppData del usuario actual.");
        }

        return Path.Combine(applicationData, "SysUtilerias", "configuracion.json");
    }
}
