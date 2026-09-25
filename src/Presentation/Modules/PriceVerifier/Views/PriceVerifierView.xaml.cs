using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SysTools.Presentation.Modules.PriceVerifier.ViewModels;

namespace SysTools.Presentation.Modules.PriceVerifier.Views;

public partial class PriceVerifierView : UserControl
{
    public PriceVerifierView() => InitializeComponent();

    private PriceVerifierViewModel? ViewModel => DataContext as PriceVerifierViewModel;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } viewModel) return;
        viewModel.PropertyChanged -= HandleViewModelPropertyChanged;
        viewModel.PropertyChanged += HandleViewModelPropertyChanged;
        await viewModel.ActivateAsync();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not { } viewModel) return;
        viewModel.PropertyChanged -= HandleViewModelPropertyChanged;
        viewModel.Deactivate();
    }

    private void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PriceVerifierViewModel.FocusRequestVersion)) return;
        Dispatcher.BeginInvoke(() =>
        {
            if (BarcodeInput.IsEnabled && BarcodeInput.IsVisible)
            {
                BarcodeInput.Focus();
                BarcodeInput.SelectAll();
            }
        }, DispatcherPriority.Input);
    }
}
