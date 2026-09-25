using System.IO;
using System.Windows;
using SysTools.Entities.Labels;
using SysTools.Presentation.Modules.Labels;

namespace SysTools.Presentation.Tests.VisualLabels;

public sealed class LabelPreviewAccessibilityTests
{
    [Fact]
    public void Preview_contract_supports_keyboard_zoom_scroll_owner_and_text_status()
    {
        var root = FindRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "Presentation", "Modules", "Labels", "LabelPreviewWindow.xaml"));
        var code = File.ReadAllText(Path.Combine(root, "src", "Presentation", "Modules", "Labels", "LabelPreviewWindow.xaml.cs"));
        var service = File.ReadAllText(Path.Combine(root, "src", "Presentation", "Modules", "Labels", "LabelPreviewDialogService.cs"));
        Assert.Contains("ScrollViewer", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.LiveSetting", xaml, StringComparison.Ordinal);
        Assert.Contains("Minimum=\"0.25\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Key.Escape", code, StringComparison.Ordinal);
        Assert.Contains("Key.OemPlus", code, StringComparison.Ordinal);
        Assert.Contains("window.Owner", service, StringComparison.Ordinal);
    }

    [Fact]
    public void Preview_measures_inside_1280_by_720_and_zoom_is_bounded_on_sta()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9Zl1sAAAAASUVORK5CYII=");
                var document = new LabelPreviewDocument(Guid.NewGuid().ToString("N"), 1, 1, 57, 40, png);
                var viewModel = new LabelPreviewViewModel(document);
                viewModel.Zoom = 99;
                Assert.Equal(3, viewModel.Zoom);
                var window = new LabelPreviewWindow(viewModel);
                window.Measure(new Size(1280, 720));
                window.Arrange(new Rect(0, 0, 1000, 650));
                Assert.True(window.DesiredSize.Width <= 1280);
                Assert.True(window.DesiredSize.Height <= 720);
                window.Close();
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
