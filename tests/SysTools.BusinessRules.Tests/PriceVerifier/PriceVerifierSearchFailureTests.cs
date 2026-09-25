using SysTools.BusinessRules.Tests.TestDoubles;
using SysTools.Entities.PriceVerifier;
using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.PriceVerifier;

public sealed class PriceVerifierSearchFailureTests
{
    [Fact]
    public async Task Old_failure_does_not_invalidate_a_new_prepared_epoch()
    {
        var pending = new TaskCompletionSource<IReadOnlyList<Product>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var calls = 0;
        var products = new ProductServiceStub
        {
            SearchHandler = (_, _, _) => ++calls == 1
                ? pending.Task
                : Task.FromResult<IReadOnlyList<Product>>([
                    new Product(2, "NEW", "Nuevo", null, null, "1")])
        };
        var workflow = PriceVerifierLookupTests.Create(products, new PriceFormatterServiceStub());
        await workflow.PrepareAsync();
        var oldSearch = workflow.SearchAsync("old");

        workflow.Invalidate();
        await workflow.PrepareAsync();
        pending.SetException(new InvalidOperationException("old secret failure"));
        var oldResult = await oldSearch;
        var currentResult = await workflow.SearchAsync("new");

        Assert.Equal(PriceVerifierSearchStatus.OperationalFailure, oldResult.Status);
        Assert.Equal(PriceVerifierSearchStatus.Success, currentResult.Status);
        Assert.Equal("NEW", Assert.Single(currentResult.Items).Barcode);
    }
}
