using SysTools.Data.Support;
using SysTools.Entities.Support;

namespace SysTools.Configuration.Tests.Support;

public sealed class WindowsSupportExternalLauncherTests
{
    public static IEnumerable<object[]> Cases()
    {
        var texts=new[]{"áéíóú","uno dos","a&b=c","línea\notra","東京","emoji 😀","+plus","?query","#hash","%percent","/slash","\\back","'quote'","\"double\"","(paren)","[bracket]","{brace}","colon:semicolon","tab\tvalue","ñ Ñ"};
        foreach(var channel in Enum.GetValues<SupportChannel>()) foreach(var text in texts) yield return [channel,text];
    }
    [Theory] [MemberData(nameof(Cases))]
    public async Task Channels_encode_user_text_without_changing_fixed_destination(SupportChannel channel,string text)
    {
        using var directory=new SupportLogTestDirectory();var starter=new ProcessStarterStub();var launcher=new WindowsSupportExternalLauncher(starter,directory.Path);
        var draft=new SupportMailDraft(SupportMailKind.ErrorReport,"",text.Replace('\n',' '),text,Guid.NewGuid().ToString("N"));
        Assert.Equal(ExternalLaunchStatus.Opened,(await launcher.OpenMailAsync(draft,channel)).Status);
        var target=Assert.Single(starter.Targets);
        Assert.Contains(Uri.EscapeDataString(text),target,StringComparison.Ordinal);
        Assert.StartsWith(channel==SupportChannel.DefaultClient?"mailto:":channel==SupportChannel.GmailWeb?"https://mail.google.com/":"https://outlook.office.com/",target,StringComparison.Ordinal);
    }
    [Fact] public async Task Folder_is_created_and_only_managed_root_is_opened()
    {
        using var directory=new SupportLogTestDirectory(false);var starter=new ProcessStarterStub();var launcher=new WindowsSupportExternalLauncher(starter,directory.Path);
        Assert.Equal(ExternalLaunchStatus.Opened,(await launcher.OpenLogFolderAsync()).Status);
        Assert.True(Directory.Exists(directory.Path));Assert.Equal(Path.GetFullPath(directory.Path),Assert.Single(starter.Targets));
    }
}

