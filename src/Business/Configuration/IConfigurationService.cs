using SysTools.Entities.Configuration;

namespace SysTools.Business.Configuration;

public interface IConfigurationService
{
    Task<ConfigurationLoadResult> LoadAsync(CancellationToken cancellationToken = default);

    ConfigurationValidationResult Validate(AppConfiguration configuration);

    Task<ConfigurationSaveResult> SaveAsync(
        AppConfiguration configuration,
        CancellationToken cancellationToken = default);
}
