using SysTools.Presentation.Shell.Models;

namespace SysTools.Presentation.Tests.Shell;

public sealed class OperationalMessageTests
{
    [Fact]
    public void Enums_include_contract_values()
    {
        Assert.Equal(new[] { "Unavailable", "Pending", "Available", "Error" }, Enum.GetNames<AvailabilityStatus>());
        Assert.Equal(new[] { "Information", "Warning", "Error" }, Enum.GetNames<MessageSeverity>());
    }

    [Fact]
    public void Message_requires_text_and_has_accessible_summary()
    {
        Assert.Throws<ArgumentException>(() => new OperationalMessage(" ", MessageSeverity.Information));
        var message = new OperationalMessage("Módulo en preparación", MessageSeverity.Information);
        Assert.Contains("Información", message.AccessibilityLabel);
        Assert.Contains(message.Text, message.AccessibilityLabel);
    }
}
