using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Presentation.Modules.PriceVerifier.Search;

namespace SysTools.Presentation.Tests.ProductSearch;

public sealed class ProductSearchAccessibilityTests
{
    [Fact]
    public void Dialog_contract_exposes_keyboard_access_read_only_grid_and_accessible_progress()
    {
        var root = FindRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "Presentation", "Modules", "PriceVerifier", "Search", "ProductSearchWindow.xaml"));
        var code = File.ReadAllText(Path.Combine(root, "src", "Presentation", "Modules", "PriceVerifier", "Search", "ProductSearchWindow.xaml.cs"));

        Assert.Contains("Mode=TwoWay", xaml, StringComparison.Ordinal);
        Assert.Contains("Key=\"Enter\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Key=\"Escape\"", xaml, StringComparison.Ordinal);
        Assert.Contains("IsReadOnly=\"True\"", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectionUnit=\"FullRow\"", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.LiveSetting=\"Polite\"", xaml, StringComparison.Ordinal);
        Assert.Contains("MouseDoubleClick", xaml, StringComparison.Ordinal);
        Assert.Contains("SelectCommand.Execute", code, StringComparison.Ordinal);
    }

    [Fact]
    public void Dialog_measures_at_supported_content_size_on_sta()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var vm = new ProductSearchViewModel(
                    TestDoubles.ReadyWorkflow(),
                    NullLogger<ProductSearchViewModel>.Instance);
                var window = new ProductSearchWindow(vm) { Width = 860, Height = 560 };
                window.Measure(new Size(860, 560));
                window.Arrange(new Rect(0, 0, 860, 560));
                Assert.NotNull(window.FindName("SearchInput") as TextBox);
                var grid = Assert.IsType<DataGrid>(window.FindName("ResultsGrid"));
                Assert.True(grid.IsReadOnly);
                Assert.True(window.DesiredSize.Width <= 860);
                Assert.True(window.DesiredSize.Height <= 560);
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
