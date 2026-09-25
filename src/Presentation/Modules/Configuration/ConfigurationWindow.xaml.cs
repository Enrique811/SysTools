using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace SysTools.Presentation.Modules.Configuration;

public partial class ConfigurationWindow : Window
{
    private readonly ConfigurationViewModel _viewModel;
    private bool _allowClose;

    public ConfigurationWindow(ConfigurationViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.CloseRequested += OnCloseRequested;
        Loaded += (_, _) => { HostInput.Focus(); HostInput.SelectAll(); };
    }

    private async void OnTestConnection(object sender, RoutedEventArgs e) =>
        await _viewModel.TestConnectionAsync(PasswordInput.Password);

    private async void OnSave(object sender, RoutedEventArgs e) =>
        await _viewModel.SaveAsync(PasswordInput.Password);

    private void OnBrowseDatabase(object sender, RoutedEventArgs e) => _viewModel.BrowseDatabase();
    private async void OnImportLicense(object sender, RoutedEventArgs e) =>
        await _viewModel.ImportLicenseAsync(PasswordInput.Password);
    private void OnCopyUuid(object sender, RoutedEventArgs e) => _viewModel.CopyHardwareId();
    private void OnPasswordChanged(object sender, RoutedEventArgs e) =>
        _viewModel.NotifyPasswordChanged(PasswordInput.Password.Length > 0);

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        _viewModel.RequestCancel();
    }

    private async void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
        { e.Handled = true; await _viewModel.SaveAsync(PasswordInput.Password); }
        else if (e.Key == Key.Escape)
        { e.Handled = true; OnCancel(sender, new RoutedEventArgs()); }
    }

    private void OnCloseRequested(object? sender, Services.ConfigurationDialogResult e)
    {
        _allowClose = true;
        PasswordInput.Clear();
        if (IsVisible) Close();
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose) return;
        e.Cancel = !_viewModel.CanDiscardChanges();
        if (!e.Cancel) _allowClose = true;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.CloseRequested -= OnCloseRequested;
        PasswordInput.Clear();
        _viewModel.Deactivate();
    }
}
