namespace SysTools.Business.Licensing;

public interface IHardwareIdProvider
{
    Task<string?> GetHardwareIdAsync(CancellationToken cancellationToken = default);
}
