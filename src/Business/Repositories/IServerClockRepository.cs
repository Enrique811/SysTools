using SysTools.Entities.Configuration;

namespace SysTools.Business.Repositories;

public interface IServerClockRepository
{
    Task<DateTime> GetCurrentAsync(
        AppConfiguration configuration,
        CancellationToken cancellationToken = default);
}
