namespace SysTools.Entities.Connection;

public enum ConnectionTestStatus
{
    Success,
    InvalidConfiguration,
    AuthenticationFailed,
    ServerUnavailable,
    DatabaseUnavailable,
    Timeout,
    Canceled,
    UnexpectedFailure
}
