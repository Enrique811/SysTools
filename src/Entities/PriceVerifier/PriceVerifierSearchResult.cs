namespace SysTools.Entities.PriceVerifier;

public sealed class PriceVerifierSearchResult
{
    public PriceVerifierSearchResult(
        PriceVerifierSearchStatus status,
        string message,
        IEnumerable<PriceVerifierSearchItem>? items = null)
    {
        if (!Enum.IsDefined(status))
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("El mensaje es obligatorio.", nameof(message));
        }

        var snapshot = items?.ToArray() ?? [];
        if (status == PriceVerifierSearchStatus.Success && snapshot.Length == 0)
        {
            throw new ArgumentException("Una busqueda exitosa requiere resultados.", nameof(items));
        }

        if (status != PriceVerifierSearchStatus.Success && snapshot.Length != 0)
        {
            throw new ArgumentException("Un resultado no exitoso no admite productos.", nameof(items));
        }

        Status = status;
        Message = message;
        Items = Array.AsReadOnly(snapshot);
    }

    public PriceVerifierSearchStatus Status { get; }
    public string Message { get; }
    public IReadOnlyList<PriceVerifierSearchItem> Items { get; }
    public bool IsSuccess => Status == PriceVerifierSearchStatus.Success;
}
