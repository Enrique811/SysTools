using SysTools.BusinessRules.Tests.TestDoubles;
using SysTools.Entities.PriceVerifier;

namespace SysTools.BusinessRules.Tests.PriceVerifier;

public sealed class PriceVerifierFailureTests
{
    [Fact]
    public async Task Catalog_failure_invalidates_session_until_prepare_runs_again()
    {
        var products = new ProductServiceStub
        {
            GetHandler = (_, _, _) => throw new InvalidOperationException("catalog unavailable")
        };
        var workflow = PriceVerifierLookupTests.Create(products, new PriceFormatterServiceStub());
        await workflow.PrepareAsync();

        var failure = await workflow.LookupAsync("001");
        var blocked = await workflow.LookupAsync("002");

        Assert.Equal(PriceVerifierLookupStatus.OperationalFailure, failure.Status);
        Assert.Equal(PriceVerifierLookupStatus.OperationalFailure, blocked.Status);
        Assert.Equal(1, products.GetCalls);
    }

    [Fact]
    public async Task Requested_cancellation_propagates()
    {
        var products = new ProductServiceStub
        {
            GetHandler = async (_, _, token) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
                return null;
            }
        };
        var workflow = PriceVerifierLookupTests.Create(products, new PriceFormatterServiceStub());
        await workflow.PrepareAsync();
        using var cancellation = new CancellationTokenSource();
        var lookup = workflow.LookupAsync("001", cancellation.Token);
        cancellation.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(() => lookup);
    }
}
