using SysTools.Data.Repositories;
using SysTools.Repositories.Tests.TestDoubles;

namespace SysTools.Repositories.Tests.Data;

public sealed class FirebirdServerClockRepositoryTests
{
    [Fact]
    public async Task GetCurrentAsync_returns_exact_server_value_with_read_only_query()
    {
        var serverValue = new DateTime(2026, 9, 23, 15, 42, 17, DateTimeKind.Unspecified);
        var command = new TrackingDbCommand
        {
            ScalarBehavior = _ => Task.FromResult<object?>(serverValue)
        };
        var connection = new TrackingDbConnection { CommandFactory = () => command };
        var repository = new FirebirdServerClockRepository(
            new FakeRepositoryConnectionFactory(() => connection),
            new RepositoryListLogger<FirebirdServerClockRepository>());

        var result = await repository.GetCurrentAsync(RepositoryTestConfiguration.Valid());

        Assert.Equal(serverValue, result);
        Assert.Equal(DateTimeKind.Unspecified, result.Kind);
        Assert.Equal(
            "SELECT CURRENT_TIMESTAMP AS HORAFECHA FROM RDB$DATABASE",
            command.CommandText);
        Assert.DoesNotContain("INSERT", command.CommandText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE", command.CommandText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE", command.CommandText, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(command.ParametersSnapshot);
        Assert.Equal(5, command.CommandTimeout);
        Assert.Equal(1, command.ScalarCalls);
        Assert.Equal(1, command.DisposeCalls);
        Assert.Equal(1, connection.DisposeCalls);
    }
}
