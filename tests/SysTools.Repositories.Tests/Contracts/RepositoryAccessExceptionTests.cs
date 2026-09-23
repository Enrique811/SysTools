using SysTools.Business.Repositories;

namespace SysTools.Repositories.Tests.Contracts;

public sealed class RepositoryAccessExceptionTests
{
    [Theory]
    [InlineData(RepositoryOperation.GetProductByBarcode, "No fue posible consultar el producto.")]
    [InlineData(RepositoryOperation.SearchProductsByDescription, "No fue posible buscar productos.")]
    [InlineData(RepositoryOperation.GetServerTimestamp, "No fue posible obtener la fecha del servidor.")]
    public void Exception_exposes_only_operation_and_safe_catalog_message(
        RepositoryOperation operation,
        string expectedMessage)
    {
        var exception = new RepositoryAccessException(operation);

        Assert.Equal(operation, exception.Operation);
        Assert.Equal(expectedMessage, exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Exception_rejects_unknown_operation() =>
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RepositoryAccessException((RepositoryOperation)999));

    [Fact]
    public void Exception_text_never_contains_external_payload()
    {
        const string sentinel = "Systools-Repo-Secret-Exception";
        var exception = new RepositoryAccessException(RepositoryOperation.GetProductByBarcode);

        Assert.DoesNotContain(sentinel, exception.ToString(), StringComparison.Ordinal);
    }
}
