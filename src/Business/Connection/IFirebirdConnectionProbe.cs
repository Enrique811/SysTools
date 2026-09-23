using SysTools.Entities.Configuration;
using SysTools.Entities.Connection;

namespace SysTools.Business.Connection;

public interface IFirebirdConnectionProbe
{
    Task<ConnectionTestStatus> ProbeAsync(
        AppConfiguration configuration,
        CancellationToken cancellationToken = default);
}
