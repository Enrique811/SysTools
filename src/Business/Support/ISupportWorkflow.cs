using SysTools.Entities.Support;

namespace SysTools.Business.Support;

public interface ISupportWorkflow
{
    Task<SupportInitializationResult> InitializeAsync(CancellationToken cancellationToken = default);
    Task<SupportActionResult> PrepareLicenseRequestAsync(CancellationToken cancellationToken = default);
    SupportActionResult PrepareErrorReport(string? summary, string? detail);
    Task<SupportActionResult> OpenDraftAsync(SupportMailDraft draft, SupportChannel channel, CancellationToken cancellationToken = default);
    Task<LogCatalogResult> GetLogsAsync(CancellationToken cancellationToken = default);
    Task<LogPreviewResult> ReadLogAsync(string fileId, CancellationToken cancellationToken = default);
    Task<SupportActionResult> OpenLogFolderAsync(CancellationToken cancellationToken = default);
    void Invalidate();
}

