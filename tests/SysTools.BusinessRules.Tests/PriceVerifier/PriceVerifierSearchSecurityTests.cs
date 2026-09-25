using SysTools.Business.PriceVerifier;
using SysTools.BusinessRules.Tests.TestDoubles;
using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.PriceVerifier;

public sealed class PriceVerifierSearchSecurityTests
{
    [Fact]
    public async Task Search_logs_only_sanitized_operational_dimensions()
    {
        const string sentinel = "SECRET_SEARCH_SENTINEL";
        var products = new ProductServiceStub
        {
            SearchHandler = (_, _, _) => Task.FromResult<IReadOnlyList<Product>>([
                new Product(1, "SECRET_BARCODE", "SECRET_DESCRIPTION", null, 1m, "1")])
        };
        var logger = new CollectingLogger<PriceVerifierWorkflow>();
        var workflow = PriceVerifierLookupTests.Create(products, new PriceFormatterServiceStub(), logger);
        await workflow.PrepareAsync();

        await workflow.SearchAsync(sentinel);

        var diagnostic = string.Join('|', logger.Entries.SelectMany(entry =>
            entry.Properties.Select(pair => $"{pair.Key}={pair.Value}")));
        Assert.DoesNotContain(sentinel, diagnostic, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET_BARCODE", diagnostic, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET_DESCRIPTION", diagnostic, StringComparison.Ordinal);
        Assert.All(logger.Entries, entry => Assert.Null(entry.Exception));
    }
}
