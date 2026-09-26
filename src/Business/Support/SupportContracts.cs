using SysTools.Entities.Support;

namespace SysTools.Business.Support;

public interface ISupportExternalLauncher
{
    Task<ExternalLaunchResult> OpenMailAsync(SupportMailDraft draft, SupportChannel channel, CancellationToken cancellationToken = default);
    Task<ExternalLaunchResult> OpenLogFolderAsync(CancellationToken cancellationToken = default);
}

public interface ISupportLogStore
{
    Task<LogCatalogResult> GetCatalogAsync(CancellationToken cancellationToken = default);
    Task<LogPreviewResult> ReadAsync(string fileId, CancellationToken cancellationToken = default);
}

public interface IApplicationVersionProvider
{
    string Version { get; }
}

