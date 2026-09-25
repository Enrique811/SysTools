using SysTools.Entities.Configuration;
using SysTools.Entities.ConfigurationEditor;

namespace SysTools.Business.ConfigurationEditor;

public interface IConfigurationRecoveryService
{
    Task<ConfigurationRecoveryResult> RecoverAsync(AppConfiguration configuration, CancellationToken cancellationToken = default);
}
