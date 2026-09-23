using SysTools.Business.Products;
using SysTools.Business.Repositories;
using SysTools.BusinessRules.Tests.TestDoubles;
using SysTools.Entities.Configuration;
using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.Products;

public sealed class ProductServiceFailureTests
{
    [Fact]
    public async Task Lookup_preserves_consumer_cancellation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var repository = new ProductRepositoryStub
        {
            GetByBarcodeHandler = (_, _, token) => Task.FromCanceled<Product?>(token)
        };
        var service = new ProductService(repository);

        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.GetByBarcodeAsync(new AppConfiguration(), "001", source.Token));

        Assert.Equal(source.Token, exception.CancellationToken);
        Assert.Equal(source.Token, repository.LastCancellationToken);
    }

    [Fact]
    public async Task Search_propagates_same_safe_repository_exception()
    {
        var expected = new RepositoryAccessException(RepositoryOperation.SearchProductsByDescription);
        var repository = new ProductRepositoryStub
        {
            SearchHandler = (_, _, _) => Task.FromException<IReadOnlyList<Product>>(expected)
        };
        var service = new ProductService(repository);

        var actual = await Assert.ThrowsAsync<RepositoryAccessException>(() =>
            service.SearchByDescriptionAsync(new AppConfiguration(), "Ar"));

        Assert.Same(expected, actual);
        Assert.Null(actual.InnerException);
    }
}
