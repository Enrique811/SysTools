using SysTools.Entities.Support;

namespace SysTools.BusinessRules.Tests.Support;

public sealed class LicenseSupportWorkflowTests
{
    [Fact] public async Task Available_hardware_prepares_versioned_license_request()
    {
        var workflow=SupportFactory.Create();
        var initialized=await workflow.InitializeAsync();
        var result=await workflow.PrepareLicenseRequestAsync();
        Assert.True(initialized.HardwareAvailable);
        Assert.Equal(SupportActionStatus.Ready,result.Status);
        Assert.Contains(initialized.HardwareId,result.Draft!.Body,StringComparison.Ordinal);
        Assert.Contains("1.2.3",result.Draft.Body,StringComparison.Ordinal);
        Assert.Equal(string.Empty,result.Draft.Recipient);
    }
    [Fact] public async Task Missing_hardware_returns_controlled_result_without_launch()
    {
        var launcher=new LauncherStub();var workflow=SupportFactory.Create(null,launcher);
        var result=await workflow.PrepareLicenseRequestAsync();
        Assert.Equal(SupportActionStatus.HardwareUnavailable,result.Status);Assert.Equal(0,launcher.MailCalls);
    }
    [Theory] [InlineData(SupportChannel.DefaultClient)] [InlineData(SupportChannel.GmailWeb)] [InlineData(SupportChannel.OutlookWeb)]
    public async Task Each_channel_opens_exactly_once(SupportChannel channel)
    {
        var launcher=new LauncherStub();var workflow=SupportFactory.Create(launcher:launcher);
        var draft=(await workflow.PrepareLicenseRequestAsync()).Draft!;
        Assert.Equal(SupportActionStatus.Opened,(await workflow.OpenDraftAsync(draft,channel)).Status);
        Assert.Equal(1,launcher.MailCalls);Assert.Equal(channel,launcher.Channel);
    }
}

