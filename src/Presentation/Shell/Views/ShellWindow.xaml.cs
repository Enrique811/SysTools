using System.Windows;
using SysTools.Presentation.Shell.Services;
using SysTools.Presentation.Shell.ViewModels;
namespace SysTools.Presentation.Shell.Views;
public partial class ShellWindow : Window
{
    private bool _initialConfigurationChecked;
    public ShellWindow(ShellViewModel viewModel) { InitializeComponent(); DataContext = viewModel; ContentRendered += OnContentRendered; }

    private async void OnContentRendered(object? sender, EventArgs e)
    {
        if (_initialConfigurationChecked || DataContext is not ShellViewModel shell) return;
        _initialConfigurationChecked = true;
        await shell.EnsureInitialConfigurationAsync();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is ShellViewModel closingShell) closingShell.CloseActiveDialogs();
        if (DataContext is ShellViewModel shell
            && shell.ActiveModuleContent is IAsyncModuleLifecycle lifecycle)
        {
            lifecycle.Deactivate();
        }

        base.OnClosed(e);
    }
}
