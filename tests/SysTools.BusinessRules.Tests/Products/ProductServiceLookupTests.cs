using SysTools.Business.Products;
using SysTools.BusinessRules.Tests.TestDoubles;
using SysTools.Entities.Configuration;
using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.Products;

public sealed class ProductServiceLookupTests
{
    [Fact]
    public async Task Null_configuration_is_rejected_before_processing_input()
    {
        var service = new ProductService(new ProductRepositoryStub());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.GetByBarcodeAsync(null!, null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_barcode_returns_null_without_contacting_repository(string? barcode)
    {
        var repository = new ProductRepositoryStub();
        var service = new ProductService(repository);

        var result = await service.GetByBarcodeAsync(new AppConfiguration(), barcode);

        Assert.Null(result);
        Assert.Equal(0, repository.GetByBarcodeCalls);
    }

    [Fact]
    public async Task Valid_barcode_is_trimmed_without_losing_leading_zeroes_or_product_identity()
    {
        var product = ProductTestFactory.Create(barcode: "000123");
        var repository = new ProductRepositoryStub
        {
            GetByBarcodeHandler = (_, _, _) => Task.FromResult<Product?>(product)
        };
        var service = new ProductService(repository);
        var configuration = new AppConfiguration();

        var result = await service.GetByBarcodeAsync(configuration, "  000123  ");

        Assert.Same(product, result);
        Assert.Same(configuration, repository.LastConfiguration);
        Assert.Equal("000123", repository.LastBarcode);
        Assert.Equal(1, repository.GetByBarcodeCalls);
    }

    [Fact]
    public async Task Missing_product_is_a_normal_null_result()
    {
        var repository = new ProductRepositoryStub();
        var service = new ProductService(repository);

        var result = await service.GetByBarcodeAsync(new AppConfiguration(), "404");

        Assert.Null(result);
        Assert.Equal(1, repository.GetByBarcodeCalls);
    }
}
