using Microsoft.Extensions.Logging;
using SysTools.Business.Repositories;
using SysTools.Data.Connection;

namespace SysTools.Data.Repositories;

internal static class FirebirdRepositoryDiagnostics
{
    internal static RepositoryAccessException Translate<T>(
        ILogger<T> logger,
        RepositoryOperation operation,
        Exception exception)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(exception);

        var codes = new FirebirdErrorClassifier().GetErrorCodes(exception);
        logger.LogWarning(
            "FirebirdRepositoryReadFailed Operation={Operation} Outcome={Outcome} ExceptionType={ExceptionType} ErrorCodes={ErrorCodes}",
            operation,
            "Failure",
            exception.GetType().Name,
            codes);
        return new RepositoryAccessException(operation);
    }
}
