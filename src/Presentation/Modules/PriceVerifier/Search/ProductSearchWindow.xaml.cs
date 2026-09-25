using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace SysTools.Presentation.Modules.PriceVerifier.Search;

public partial class ProductSearchWindow : Window
{
    private readonly ProductSearchViewModel _viewModel;

    public ProductSearchWindow(ProductSearchViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    public CancellationToken LifecycleToken { get; set; }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.Activate(LifecycleToken);
        SearchInput.Focus();
        SearchInput.SelectAll();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.Deactivate();
    }

    private void OnResultsDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel.SelectCommand.CanExecute(ResultsGrid.SelectedItem))
        {
            _viewModel.SelectCommand.Execute(ResultsGrid.SelectedItem);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductSearchViewModel.SearchFocusRequestVersion))
        {
            Dispatcher.BeginInvoke(() => { SearchInput.Focus(); SearchInput.SelectAll(); });
        }
        else if (e.PropertyName == nameof(ProductSearchViewModel.ResultsFocusRequestVersion))
        {
            Dispatcher.BeginInvoke(() => ResultsGrid.Focus());
        }
    }
}
