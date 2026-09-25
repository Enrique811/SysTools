using System.Windows;
using SysTools.Presentation.Shell.Services;
using SysTools.Presentation.Shell.ViewModels;
namespace SysTools.Presentation.Shell.Views;
public partial class ShellWindow : Window
{
    public ShellWindow(ShellViewModel viewModel) { InitializeComponent(); DataContext = viewModel; }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is ShellViewModel shell
            && shell.ActiveModuleContent is IAsyncModuleLifecycle lifecycle)
        {
            lifecycle.Deactivate();
        }

        base.OnClosed(e);
    }
}
