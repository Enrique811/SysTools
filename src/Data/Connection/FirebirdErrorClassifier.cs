using FirebirdSql.Data.FirebirdClient;
using SysTools.Entities.Connection;

namespace SysTools.Data.Connection;

public class FirebirdErrorClassifier
{
    private static readonly HashSet<int> AuthenticationCodes =
        [335544472, 335545106];

    private static readonly HashSet<int> ServerCodes =
        [335544421, 335544721, 335544726, 335544727];

    private static readonly HashSet<int> DatabaseCodes =
        [335544323, 335544344, 335544375, 335544379];

    public virtual ConnectionTestStatus Classify(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return Classify(GetErrorCodes(exception));
    }

    public ConnectionTestStatus Classify(IEnumerable<int> errorCodes)
    {
        ArgumentNullException.ThrowIfNull(errorCodes);
        var codes = errorCodes.ToHashSet();

        if (codes.Overlaps(AuthenticationCodes))
        {
            return ConnectionTestStatus.AuthenticationFailed;
        }

        if (codes.Overlaps(ServerCodes))
        {
            return ConnectionTestStatus.ServerUnavailable;
        }

        if (codes.Overlaps(DatabaseCodes))
        {
            return ConnectionTestStatus.DatabaseUnavailable;
        }

        return ConnectionTestStatus.UnexpectedFailure;
    }

    public virtual IReadOnlyList<int> GetErrorCodes(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        if (exception is not FbException firebirdException)
        {
            return [];
        }

        return firebirdException.Errors
            .Cast<FbError>()
            .Select(error => error.Number)
            .ToArray();
    }
}
