using System.Data.Common;
using SysTools.Business.Repositories;
using SysTools.Data.Repositories;
using SysTools.Repositories.Tests.TestDoubles;

namespace SysTools.Repositories.Tests.Security;

public sealed class RepositoryLoggingTests
{
    [Fact]
    [Trait("Category", "RepositorySecurity")]
    public async Task Failures_do_not_log_configuration_inputs_rows_sql_or_raw_exception()
    {
        var sentinels = new[]
        {
            "Systools-Repo-Secret-Password",
            "Systools-Repo-Secret-Host",
            "Systools-Repo-Secret-Database",
            "Systools-Repo-Secret-User",
            "Systools-Repo-Secret-License",
            "Systools-Repo-Barcode-Input",
            "Systools-Repo-Description-Input",
            "Systools-Repo-Secret-Provider"
        };
        var command = new TrackingDbCommand
        {
            ReaderBehavior = _ => Task.FromException<DbDataReader>(
                new InvalidOperationException(sentinels[^1]))
        };
        var logger = new RepositoryListLogger<FirebirdProductRepository>();
        var repository = new FirebirdProductRepository(
            new FakeRepositoryConnectionFactory(() =>
                new TrackingDbConnection { CommandFactory = () => command }),
            logger);

        var lookup = await Assert.ThrowsAsync<RepositoryAccessException>(() =>
            repository.GetByBarcodeAsync(
                RepositoryTestConfiguration.Valid(),
                sentinels[5]));
        var search = await Assert.ThrowsAsync<RepositoryAccessException>(() =>
            repository.SearchByDescriptionAsync(
                RepositoryTestConfiguration.Valid(),
                sentinels[6]));

        var captured = string.Join(
            "|",
            logger.Entries.SelectMany(entry =>
                new[] { entry.Message, entry.Exception?.ToString() ?? string.Empty }
                    .Concat(entry.Properties.Select(property =>
                        $"{property.Key}={property.Value}"))));
        captured += $"|{lookup}|{search}";

        Assert.All(sentinels, sentinel =>
            Assert.DoesNotContain(sentinel, captured, StringComparison.Ordinal));
        Assert.DoesNotContain("SELECT", captured, StringComparison.OrdinalIgnoreCase);
        Assert.All(logger.Entries, entry => Assert.Null(entry.Exception));
        Assert.All(
            logger.Entries,
            entry => Assert.Subset(
                new HashSet<string>(StringComparer.Ordinal)
                {
                    "Operation", "Outcome", "ExceptionType", "ErrorCodes"
                },
                entry.Properties.Keys.ToHashSet(StringComparer.Ordinal)));
    }
}
