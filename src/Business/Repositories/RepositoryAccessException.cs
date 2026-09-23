namespace SysTools.Business.Repositories;

public sealed class RepositoryAccessException : Exception
{
    public RepositoryAccessException(RepositoryOperation operation)
        : base(MessageFor(operation)) => Operation = operation;

    public RepositoryOperation Operation { get; }

    private static string MessageFor(RepositoryOperation operation) => operation switch
    {
        RepositoryOperation.GetProductByBarcode =>
            "No fue posible consultar el producto.",
        RepositoryOperation.SearchProductsByDescription =>
            "No fue posible buscar productos.",
        RepositoryOperation.GetServerTimestamp =>
            "No fue posible obtener la fecha del servidor.",
        _ => throw new ArgumentOutOfRangeException(nameof(operation))
    };
}
