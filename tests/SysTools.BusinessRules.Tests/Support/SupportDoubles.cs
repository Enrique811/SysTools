using Microsoft.Extensions.Logging;
using SysTools.Business.Licensing;
using SysTools.Business.Support;
using SysTools.Entities.Support;

namespace SysTools.BusinessRules.Tests.Support;

internal sealed class HardwareStub(string? value) : IHardwareIdProvider
{ public Task<string?> GetHardwareIdAsync(CancellationToken cancellationToken=default) => Task.FromResult(value); }
internal sealed class VersionStub(string value="1.2.3") : IApplicationVersionProvider { public string Version => value; }
internal sealed class LauncherStub(ExternalLaunchStatus status=ExternalLaunchStatus.Opened) : ISupportExternalLauncher
{
    public int MailCalls { get; private set; } public int FolderCalls { get; private set; }
    public SupportMailDraft? Draft { get; private set; } public SupportChannel Channel { get; private set; }
    public Func<CancellationToken,Task<ExternalLaunchResult>>? MailHandler { get; set; }
    public Task<ExternalLaunchResult> OpenMailAsync(SupportMailDraft draft,SupportChannel channel,CancellationToken token=default)
    { MailCalls++;Draft=draft;Channel=channel;return MailHandler?.Invoke(token)??Task.FromResult(new ExternalLaunchResult(status)); }
    public Task<ExternalLaunchResult> OpenLogFolderAsync(CancellationToken token=default)
    { FolderCalls++;return Task.FromResult(new ExternalLaunchResult(status)); }
}
internal sealed class LogStoreStub : ISupportLogStore
{
    public LogCatalogResult Catalog { get; set; } = new(LogCatalogStatus.Empty);
    public LogPreviewResult Preview { get; set; } = new(LogPreviewStatus.Missing);
    public Task<LogCatalogResult> GetCatalogAsync(CancellationToken token=default)=>Task.FromResult(Catalog);
    public Task<LogPreviewResult> ReadAsync(string id,CancellationToken token=default)=>Task.FromResult(Preview);
}
internal sealed class SupportLogger : ILogger<SupportWorkflow>
{
    public List<string> Messages { get; }=[]; public List<Exception?> Exceptions { get; }=[];
    public IDisposable? BeginScope<TState>(TState state) where TState:notnull=>null;
    public bool IsEnabled(LogLevel level)=>true;
    public void Log<TState>(LogLevel level,EventId eventId,TState state,Exception? exception,Func<TState,Exception?,string> formatter)
    {Messages.Add(formatter(state,exception));Exceptions.Add(exception);}
}
internal static class SupportFactory
{
    public static SupportWorkflow Create(string? hardware="11111111-2222-3333-4444-555555555555",LauncherStub? launcher=null,LogStoreStub? logs=null,SupportLogger? logger=null) =>
        new(new HardwareStub(hardware),new VersionStub(),launcher??new(),logs??new(),logger??new());
}

