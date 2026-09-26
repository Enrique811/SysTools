using SysTools.Entities.Support;

namespace SysTools.BusinessRules.Tests.Support;

public sealed class SupportSecurityReliabilityTests
{
    [Fact] public async Task One_hundred_operations_log_only_safe_metadata()
    {
        const string sentinel="SECRET-PAYLOAD-ROUTE-PASSWORD";
        var logger=new SupportLogger();var launcher=new LauncherStub();var workflow=SupportFactory.Create(sentinel,launcher,logger:logger);
        foreach(var channel in Enum.GetValues<SupportChannel>()) for(var i=0;i<100;i++)
        {
            var draft=workflow.PrepareErrorReport($"summary {i}",$"{sentinel} detail {i}").Draft!;
            Assert.Equal(SupportActionStatus.Opened,(await workflow.OpenDraftAsync(draft,channel)).Status);
        }
        var diagnostic=string.Join('|',logger.Messages);
        Assert.DoesNotContain(sentinel,diagnostic,StringComparison.Ordinal);
        Assert.All(logger.Exceptions,Assert.Null);
        Assert.Equal(300,launcher.MailCalls);
    }
    [Fact] public async Task Concurrent_open_is_busy_and_late_result_is_stale()
    {
        var started=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release=new TaskCompletionSource<ExternalLaunchResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var launcher=new LauncherStub{MailHandler=_=>{started.TrySetResult();return release.Task;}};
        var workflow=SupportFactory.Create(launcher:launcher);
        var draft=workflow.PrepareErrorReport("summary","detail").Draft!;
        var first=workflow.OpenDraftAsync(draft,SupportChannel.DefaultClient);
        await started.Task;
        Assert.Equal(SupportActionStatus.Busy,(await workflow.OpenDraftAsync(draft,SupportChannel.GmailWeb)).Status);
        workflow.Invalidate();release.SetResult(new(ExternalLaunchStatus.Opened));
        Assert.Equal(SupportActionStatus.Stale,(await first).Status);
    }
}
