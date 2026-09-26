using SysTools.Business.Support;
using SysTools.Entities.Support;
using SysTools.Presentation.Modules.Configuration.Services;

namespace SysTools.Presentation.Tests.Support;

internal sealed class SupportWorkflowStub : ISupportWorkflow
{
    public SupportInitializationResult Initialization { get; set; }=new(true,"11111111-2222-3333-4444-555555555555","ready");
    public SupportActionResult License { get; set; }=Ready(SupportMailKind.LicenseRequest);
    public SupportActionResult Error { get; set; }=Ready(SupportMailKind.ErrorReport);
    public SupportActionResult Open { get; set; }=new(SupportActionStatus.Opened,"opened");
    public LogCatalogResult Catalog { get; set; }=new(LogCatalogStatus.Empty);
    public LogPreviewResult Preview { get; set; }=new(LogPreviewStatus.Missing);
    public Func<CancellationToken,Task<SupportActionResult>>? OpenHandler { get; set; }
    public Func<CancellationToken,Task<SupportActionResult>>? LicenseHandler { get; set; }
    public int OpenCalls { get; private set; } public int InvalidateCalls { get; private set; }
    public Task<SupportInitializationResult> InitializeAsync(CancellationToken token=default)=>Task.FromResult(Initialization);
    public Task<SupportActionResult> PrepareLicenseRequestAsync(CancellationToken token=default)=>LicenseHandler?.Invoke(token)??Task.FromResult(License);
    public SupportActionResult PrepareErrorReport(string? summary,string? detail)=>Error;
    public Task<SupportActionResult> OpenDraftAsync(SupportMailDraft draft,SupportChannel channel,CancellationToken token=default){OpenCalls++;return OpenHandler?.Invoke(token)??Task.FromResult(Open);}
    public Task<LogCatalogResult> GetLogsAsync(CancellationToken token=default)=>Task.FromResult(Catalog);
    public Task<LogPreviewResult> ReadLogAsync(string id,CancellationToken token=default)=>Task.FromResult(Preview);
    public Task<SupportActionResult> OpenLogFolderAsync(CancellationToken token=default)=>Task.FromResult(Open);
    public void Invalidate()=>InvalidateCalls++;
    public static SupportActionResult Ready(SupportMailKind kind)=>new(SupportActionStatus.Ready,"ready",draft:new(kind,"","subject","body",Guid.NewGuid().ToString("N")));
}
internal sealed class ClipboardStub : IClipboardService
{ public string? Value {get;private set;} public bool Result {get;set;}=true; public bool TrySetText(string value){Value=value;return Result;} }
