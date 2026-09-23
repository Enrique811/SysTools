using SysTools.Entities.Configuration;
using SysTools.Entities.Connection;

namespace SysTools.Business.Connection;

public interface IConnectionTestService
{
    Task<ConnectionTestResult> TestAsync(
        AppConfiguration configuration,
        CancellationToken cancellationToken = default);
}
