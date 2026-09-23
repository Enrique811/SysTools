using SysTools.Business.Products;
using SysTools.BusinessRules.Tests.TestDoubles;
using SysTools.Entities.Configuration;
using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.Products;

public sealed class ProductServiceSearchTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t ")]
    public async Task Empty_prefix_returns_empty_without_contacting_repository(string? prefix)
    {
        var repository = new ProductRepositoryStub();
        var service = new ProductService(repository);

        var result = await service.SearchByDescriptionAsync(new AppConfiguration(), prefix);

        Assert.Empty(result);
        Assert.Equal(0, repository.SearchCalls);
    }

    [Fact]
    public async Task Search_trims_only_outer_whitespace_and_preserves_repository_order()
    {
        var first = ProductTestFactory.Create(id: 1, barcode: "001", description: "Árbol  grande");
        var second = ProductTestFactory.Create(id: 2, barcode: "002", description: "Árbol chico");
        IReadOnlyList<Product> products = Array.AsReadOnly(new[] { first, second });
        var repository = new ProductRepositoryStub
        {
            SearchHandler = (_, _, _) => Task.FromResult(products)
        };
        var service = new ProductService(repository);

        var result = await service.SearchByDescriptionAsync(
            new AppConfiguration(),
            "  Árbol  grande  ");

        Assert.Same(products, result);
        Assert.Equal("Árbol  grande", repository.LastDescriptionPrefix);
        Assert.Collection(result, item => Assert.Same(first, item), item => Assert.Same(second, item));
    }

    [Fact]
    public async Task Null_configuration_is_rejected()
    {
        var service = new ProductService(new ProductRepositoryStub());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.SearchByDescriptionAsync(null!, "A"));
    }
}
