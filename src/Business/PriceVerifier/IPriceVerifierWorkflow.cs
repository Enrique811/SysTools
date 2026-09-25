using SysTools.Entities.PriceVerifier;

namespace SysTools.Business.PriceVerifier;

public interface IPriceVerifierWorkflow
{
    Task<PriceVerifierPreparationResult> PrepareAsync(
        CancellationToken cancellationToken = default);

    Task<PriceVerifierLookupResult> LookupAsync(
        string? barcode,
        CancellationToken cancellationToken = default);

    void Invalidate();
}

