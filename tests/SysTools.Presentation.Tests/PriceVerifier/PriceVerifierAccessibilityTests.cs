using System.IO;
using System.Windows;
using System.Windows.Controls;
using SysTools.Presentation.Modules.PriceVerifier.Views;

namespace SysTools.Presentation.Tests.PriceVerifier;

public sealed class PriceVerifierAccessibilityTests
{
    [Fact]
    public void View_contract_has_immediate_two_way_enter_focus_and_accessible_status()
    {
        var root = FindRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "Presentation", "Modules", "PriceVerifier", "Views", "PriceVerifierView.xaml"));
        var codeBehind = File.ReadAllText(Path.Combine(root, "src", "Presentation", "Modules", "PriceVerifier", "Views", "PriceVerifierView.xaml.cs"));

        Assert.Contains("Mode=TwoWay", xaml, StringComparison.Ordinal);
        Assert.Contains("UpdateSourceTrigger=PropertyChanged", xaml, StringComparison.Ordinal);
        Assert.Contains("AdditionalInformation, Mode=OneWay", xaml, StringComparison.Ordinal);
        Assert.Contains("Key=\"Enter\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.LiveSetting=\"Polite\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectAll()", codeBehind, StringComparison.Ordinal);
        Assert.Contains("Dispatcher.BeginInvoke", codeBehind, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_and_view_preserve_minimum_layout_and_future_actions_disabled()
    {
        var root = FindRoot();
        var shell = File.ReadAllText(Path.Combine(root, "src", "Presentation", "Shell", "Views", "ShellWindow.xaml"));
        var view = File.ReadAllText(Path.Combine(root, "src", "Presentation", "Modules", "PriceVerifier", "Views", "PriceVerifierView.xaml"));
        Assert.Contains("MinWidth=\"1280\"", shell, StringComparison.Ordinal);
        Assert.Contains("MinHeight=\"720\"", shell, StringComparison.Ordinal);
        Assert.Contains("IsSearchAvailable", view, StringComparison.Ordinal);
        Assert.Contains("IsPrintAvailable", view, StringComparison.Ordinal);
        Assert.Contains("IsSettingsAvailable", view, StringComparison.Ordinal);
        Assert.Contains("TextWrapping=\"Wrap\"", view, StringComparison.Ordinal);
    }

    [Fact]
    public void View_measures_at_the_shells_1280_by_720_content_area_on_sta()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var view = new PriceVerifierView
                {
                    DataContext = TestDoubles.CreatePriceVerifier(),
                    Width = 900,
                    Height = 590
                };
                view.Measure(new Size(900, 590));
                view.Arrange(new Rect(0, 0, 900, 590));
                Assert.NotNull(view.FindName("BarcodeInput") as TextBox);
                Assert.True(view.DesiredSize.Width <= 900);
                Assert.True(view.DesiredSize.Height <= 590);
            }
            catch (Exception exception) { failure = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "SysTools.sln"))) directory = directory.Parent;
        return directory?.FullName ?? throw new InvalidOperationException("Solution root not found.");
    }
}
