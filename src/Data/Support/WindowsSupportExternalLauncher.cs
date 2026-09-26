using System.ComponentModel;
using System.Diagnostics;
using SysTools.Business.Support;
using SysTools.Entities.Support;

namespace SysTools.Data.Support;

public interface IExternalProcessStarter
{
    bool Start(string target);
}

public sealed class WindowsSupportExternalLauncher : ISupportExternalLauncher
{
    private readonly IExternalProcessStarter _processes;
    private readonly string _logRoot;

    public WindowsSupportExternalLauncher() : this(new ShellExternalProcessStarter(),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SysTools", "Logs")) { }

    public WindowsSupportExternalLauncher(IExternalProcessStarter processes, string logRoot)
    {
        _processes = processes ?? throw new ArgumentNullException(nameof(processes));
        _logRoot = Path.GetFullPath(logRoot ?? throw new ArgumentNullException(nameof(logRoot)));
    }

    public Task<ExternalLaunchResult> OpenMailAsync(SupportMailDraft draft, SupportChannel channel, CancellationToken cancellationToken = default) =>
        OpenAsync(BuildMailTarget(draft, channel), cancellationToken);

    public Task<ExternalLaunchResult> OpenLogFolderAsync(CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_logRoot);
        return OpenAsync(_logRoot, cancellationToken);
    }

    public static string BuildMailTarget(SupportMailDraft draft, SupportChannel channel)
    {
        ArgumentNullException.ThrowIfNull(draft);
        if (!Enum.IsDefined(channel)) throw new ArgumentOutOfRangeException(nameof(channel));
        var recipient = Uri.EscapeDataString(draft.Recipient);
        var subject = Uri.EscapeDataString(draft.Subject);
        var body = Uri.EscapeDataString(draft.Body);
        return channel switch
        {
            SupportChannel.DefaultClient => $"mailto:{recipient}?subject={subject}&body={body}",
            SupportChannel.GmailWeb => $"https://mail.google.com/mail/?view=cm&fs=1&to={recipient}&su={subject}&body={body}",
            SupportChannel.OutlookWeb => $"https://outlook.office.com/mail/deeplink/compose?to={recipient}&subject={subject}&body={body}",
            _ => throw new ArgumentOutOfRangeException(nameof(channel))
        };
    }

    private Task<ExternalLaunchResult> OpenAsync(string target, CancellationToken cancellationToken) => Task.Run(() =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        try { return new ExternalLaunchResult(_processes.Start(target) ? ExternalLaunchStatus.Opened : ExternalLaunchStatus.Failed); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return new(ExternalLaunchStatus.Canceled); }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException or IOException or UnauthorizedAccessException)
        { return new(ExternalLaunchStatus.Failed); }
    }, CancellationToken.None);

    private sealed class ShellExternalProcessStarter : IExternalProcessStarter
    {
        public bool Start(string target) => Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }) is not null;
    }
}

