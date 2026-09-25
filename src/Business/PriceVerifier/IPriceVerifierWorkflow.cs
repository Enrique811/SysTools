using SysTools.Entities.PriceVerifier;

namespace SysTools.Business.PriceVerifier;

public interface IPriceVerifierWorkflow
{
    Task<PriceVerifierPreparationResult> PrepareAsync(
        CancellationToken cancellationToken = default);

    Task<PriceVerifierLookupResult> LookupAsync(
        string? barcode,
        CancellationToken cancellationToken = default);

    Task<PriceVerifierSearchResult> SearchAsync(
        string? descriptionPrefix,
        CancellationToken cancellationToken = default);

    void Invalidate();
}
