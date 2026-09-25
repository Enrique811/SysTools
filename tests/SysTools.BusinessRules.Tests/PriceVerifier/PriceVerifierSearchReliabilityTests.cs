using SysTools.BusinessRules.Tests.TestDoubles;
using SysTools.Entities.PriceVerifier;
using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.PriceVerifier;

public sealed class PriceVerifierSearchReliabilityTests
{
    [Fact]
    public async Task Fifty_representative_prefixes_preserve_order_content_and_casing_equivalence()
    {
        var catalog = Enumerable.Range(1, 50)
            .Select(index => new Product(index, index.ToString("D5"), $"Producto {index:D2}", null,
                index % 3 == 0 ? null : index + .25m,
                index % 4 == 0 ? null : index.ToString()))
            .ToArray();
        var products = new ProductServiceStub
        {
            SearchHandler = (_, prefix, _) => Task.FromResult<IReadOnlyList<Product>>(catalog
                .Where(product => product.Description.StartsWith(prefix!, StringComparison.OrdinalIgnoreCase))
                .OrderBy(product => product.Description, StringComparer.OrdinalIgnoreCase)
                .ThenBy(product => product.Id)
                .ToArray())
        };
        var workflow = PriceVerifierLookupTests.Create(products, new PriceFormatterServiceStub());
        await workflow.PrepareAsync();

        for (var index = 1; index <= 50; index++)
        {
            var prefix = $"producto {index:D2}";
            var lower = await workflow.SearchAsync(prefix);
            var upper = await workflow.SearchAsync(prefix.ToUpperInvariant());
            Assert.Equal(PriceVerifierSearchStatus.Success, lower.Status);
            Assert.Equal(lower.Items.Select(item => item.Barcode), upper.Items.Select(item => item.Barcode));
            Assert.All(lower.Items, item => Assert.StartsWith(prefix, item.Description, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(lower.Items.OrderBy(item => item.Description, StringComparer.OrdinalIgnoreCase), lower.Items);
        }

        Assert.Equal(100, products.SearchCalls);
    }
}
