using System.IO;

namespace SysTools.Presentation.Tests.Support;

public sealed class SupportAccessibilityTests
{
    [Fact] public void View_declares_keyboard_scroll_accessible_names_and_bounded_inputs()
    {
        var root=FindRoot();var xaml=File.ReadAllText(Path.Combine(root,"src","Presentation","Modules","Support","SupportView.xaml"));
        Assert.Contains("KeyBinding Key=\"F5\"",xaml,StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"",xaml,StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.Name",xaml,StringComparison.Ordinal);
        Assert.Contains("MaxLength=\"120\"",xaml,StringComparison.Ordinal);
        Assert.Contains("MaxLength=\"4000\"",xaml,StringComparison.Ordinal);
        Assert.Contains("MinWidth=\"760\"",xaml,StringComparison.Ordinal);
    }
    [Fact] public void ViewModel_has_no_filesystem_or_process_access()
    {
        var root=FindRoot();var source=File.ReadAllText(Path.Combine(root,"src","Presentation","Modules","Support","SupportViewModel.cs"));
        Assert.DoesNotContain("System.IO",source,StringComparison.Ordinal);Assert.DoesNotContain("Process.",source,StringComparison.Ordinal);
        Assert.DoesNotContain("File.",source,StringComparison.Ordinal);Assert.DoesNotContain("Directory.",source,StringComparison.Ordinal);
    }
    private static string FindRoot(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d is not null&&!File.Exists(Path.Combine(d.FullName,"SysTools.sln")))d=d.Parent;return d?.FullName??throw new InvalidOperationException();}
}
