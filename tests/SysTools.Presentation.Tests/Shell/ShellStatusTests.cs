using SysTools.Presentation.Shell.Models;

namespace SysTools.Presentation.Tests.Shell;

public sealed class ShellStatusTests
{
    [Fact]
    public void Starts_with_neutral_operational_status()
    {
        var shell = TestDoubles.CreateShell();
        Assert.Equal(AvailabilityStatus.Unavailable, shell.ConnectionStatus);
        Assert.Equal(AvailabilityStatus.Unavailable, shell.LicenseStatus);
        Assert.Equal("Módulo en preparación", shell.StatusMessage.Text);
    }

    [Fact]
    public void Recoverable_failure_keeps_module_and_hides_diagnostics()
    {
        var shell = TestDoubles.CreateShell(new ThrowingInitializer());
        Assert.Equal("price-verifier", shell.ActiveModule.Id);
        Assert.DoesNotContain("InvalidOperationException", shell.StatusMessage.Text);
        Assert.DoesNotContain("C:\\", shell.StatusMessage.Text);
        Assert.Equal(MessageSeverity.Error, shell.StatusMessage.Severity);
    }
}
