using SysTools.Entities.Configuration;

namespace SysTools.Business.Configuration;

public interface IConfigurationRepository
{
    Task<ConfigurationRepositoryReadResult> ReadAsync(
        CancellationToken cancellationToken = default);

    Task<ConfigurationRepositoryWriteResult> WriteAsync(
        AppConfiguration configuration,
        CancellationToken cancellationToken = default);
}
