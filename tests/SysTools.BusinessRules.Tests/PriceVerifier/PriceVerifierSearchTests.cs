using SysTools.Business.PriceVerifier;
using SysTools.BusinessRules.Tests.TestDoubles;
using SysTools.Entities.PriceVerifier;
using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.PriceVerifier;

public sealed class PriceVerifierSearchTests
{
    [Theory]
    [InlineData(null, PriceVerifierSearchStatus.MissingInput)]
    [InlineData("   ", PriceVerifierSearchStatus.MissingInput)]
    public async Task Missing_input_does_not_query_catalog(string? input, PriceVerifierSearchStatus expected)
    {
        var products = new ProductServiceStub();
        var workflow = PriceVerifierLookupTests.Create(products, new PriceFormatterServiceStub());
        await workflow.PrepareAsync();

        var result = await workflow.SearchAsync(input);

        Assert.Equal(expected, result.Status);
        Assert.Equal(0, products.SearchCalls);
    }

    [Fact]
    public async Task Limits_trim_order_and_conditional_formatting_are_preserved()
    {
        var products = new ProductServiceStub
        {
            SearchHandler = (_, _, _) => Task.FromResult<IReadOnlyList<Product>>([
                new Product(1, "001", "Cafe", null, 10.5m, "3"),
                new Product(2, "002", "Cafe molido", null, null, null)])
        };
        var formatter = new PriceFormatterServiceStub { Handler = (amount, _) => $"${amount:0.00}" };
        var workflow = PriceVerifierLookupTests.Create(products, formatter);
        await workflow.PrepareAsync();

        var result = await workflow.SearchAsync("  ca  ");
        Assert.Equal("ca", products.LastDescriptionPrefix);
        Assert.Equal(1, formatter.Calls);
        var accepted = await workflow.SearchAsync(new string('a', 255));
        var rejected = await workflow.SearchAsync(new string('a', 256));

        Assert.Equal(PriceVerifierSearchStatus.Success, result.Status);
        Assert.Collection(result.Items,
            item => { Assert.Equal("001", item.Barcode); Assert.Equal("$10.50", item.FormattedPrice); },
            item => { Assert.Equal("002", item.Barcode); Assert.Null(item.FormattedPrice); });
        Assert.Equal(PriceVerifierSearchStatus.Success, accepted.Status);
        Assert.Equal(PriceVerifierSearchStatus.InputTooLong, rejected.Status);
        Assert.Equal(2, products.SearchCalls);
    }

    [Fact]
    public async Task Search_without_prepared_session_is_operational_failure()
    {
        var products = new ProductServiceStub();
        var workflow = PriceVerifierLookupTests.Create(products, new PriceFormatterServiceStub());

        var result = await workflow.SearchAsync("ca");

        Assert.Equal(PriceVerifierSearchStatus.OperationalFailure, result.Status);
        Assert.Equal(0, products.SearchCalls);
    }

    [Fact]
    public async Task Catalog_failure_invalidates_current_session_and_cancellation_propagates()
    {
        var products = new ProductServiceStub
        {
            SearchHandler = (_, _, _) => throw new InvalidOperationException("secret payload")
        };
        var workflow = PriceVerifierLookupTests.Create(products, new PriceFormatterServiceStub());
        await workflow.PrepareAsync();

        var failed = await workflow.SearchAsync("ca");
        var blocked = await workflow.SearchAsync("next");

        Assert.Equal(PriceVerifierSearchStatus.OperationalFailure, failed.Status);
        Assert.Equal(PriceVerifierSearchStatus.OperationalFailure, blocked.Status);

        await workflow.PrepareAsync();
        products.SearchHandler = (_, _, token) => Task.FromCanceled<IReadOnlyList<Product>>(token);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            workflow.SearchAsync("ca", cancellation.Token));
    }
}
