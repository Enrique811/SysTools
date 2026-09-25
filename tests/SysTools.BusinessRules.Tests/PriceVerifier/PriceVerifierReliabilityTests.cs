using SysTools.BusinessRules.Tests.TestDoubles;
using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.PriceVerifier;

public sealed class PriceVerifierReliabilityTests
{
    [Fact]
    public async Task Matrix_of_fifty_products_preserves_identity_accents_nulls_stock_and_format()
    {
        var products = new ProductServiceStub();
        products.GetHandler = (_, barcode, _) =>
        {
            var number = int.Parse(barcode![1..]);
            return Task.FromResult<Product?>(new Product(
                number + 1,
                barcode,
                number % 5 == 0 ? string.Empty : $"Artículo número {number}",
                number % 3 == 0 ? null : "Pieza",
                number % 4 == 0 ? null : number + 0.25m,
                number % 2 == 0 ? null : number.ToString()));
        };
        var formatter = new PriceFormatterServiceStub
        {
            Handler = (amount, format) => $"{format}:{amount:0.00}"
        };
        var workflow = PriceVerifierLookupTests.Create(products, formatter);
        await workflow.PrepareAsync();

        for (var index = 0; index < 50; index++)
        {
            var barcode = $"0{index:D3}";
            var result = await workflow.LookupAsync(barcode);
            Assert.True(result.IsSuccess);
            Assert.Equal(barcode, result.Product!.Barcode);
            Assert.Equal(index % 2 == 0 ? Product.StockNotRegistered : index.ToString(), result.Product.Stock);
            if (result.Product.PriceWithTax is null) Assert.Null(result.FormattedPrice);
            else Assert.StartsWith("MX:", result.FormattedPrice);
        }

        Assert.Equal(50, products.GetCalls);
    }
}
