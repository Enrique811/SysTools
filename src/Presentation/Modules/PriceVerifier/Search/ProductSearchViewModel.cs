using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using SysTools.Business.PriceVerifier;
using SysTools.Entities.PriceVerifier;
using SysTools.Presentation.Commands;
using SysTools.Presentation.ViewModels;

namespace SysTools.Presentation.Modules.PriceVerifier.Search;

public sealed class ProductSearchViewModel : ViewModelBase
{
    private readonly IPriceVerifierWorkflow _workflow;
    private readonly ILogger<ProductSearchViewModel> _logger;
    private readonly ObservableCollection<PriceVerifierSearchItem> _items = [];
    private CancellationTokenSource? _lifecycleCancellation;
    private CancellationTokenSource? _searchCancellation;
    private string _searchText = string.Empty;
    private string _statusMessage = "Ingrese el inicio de la descripcion.";
    private PriceVerifierSearchItem? _selectedItem;
    private bool _isBusy;
    private int _searchFocusRequestVersion;
    private int _resultsFocusRequestVersion;
    private long _generation;
    private int _completionState;

    public ProductSearchViewModel(
        IPriceVerifierWorkflow workflow,
        ILogger<ProductSearchViewModel> logger)
    {
        _workflow = workflow;
        _logger = logger;
        SearchCommand = new AsyncRelayCommand(SearchAsync, () => !IsBusy);
        SelectCommand = new RelayCommand(Select, CanSelect);
        CancelCommand = new RelayCommand(Cancel);
    }

    public event EventHandler<ProductSearchDialogResult>? CloseRequested;

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value ?? string.Empty);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public IReadOnlyList<PriceVerifierSearchItem> Items => _items;

    public PriceVerifierSearchItem? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
            {
                SelectCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                SearchCommand.NotifyCanExecuteChanged();
                SelectCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public int SearchFocusRequestVersion
    {
        get => _searchFocusRequestVersion;
        private set => SetProperty(ref _searchFocusRequestVersion, value);
    }

    public int ResultsFocusRequestVersion
    {
        get => _resultsFocusRequestVersion;
        private set => SetProperty(ref _resultsFocusRequestVersion, value);
    }

    public AsyncRelayCommand SearchCommand { get; }
    public RelayCommand SelectCommand { get; }
    public RelayCommand CancelCommand { get; }

    public void Activate(CancellationToken cancellationToken = default)
    {
        DeactivateCore();
        _lifecycleCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Interlocked.Exchange(ref _completionState, 0);
        SearchFocusRequestVersion++;
    }

    public void Deactivate()
    {
        Interlocked.Exchange(ref _completionState, 1);
        DeactivateCore();
    }

    private async Task SearchAsync()
    {
        var lifecycleToken = _lifecycleCancellation?.Token ?? CancellationToken.None;
        if (lifecycleToken.IsCancellationRequested || Volatile.Read(ref _completionState) != 0)
        {
            return;
        }

        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = CancellationTokenSource.CreateLinkedTokenSource(lifecycleToken);
        var token = _searchCancellation.Token;
        var generation = Interlocked.Increment(ref _generation);
        IsBusy = true;
        ReplaceItems([]);
        StatusMessage = "Buscando productos...";

        try
        {
            var result = await _workflow.SearchAsync(SearchText, token);
            if (!IsCurrent(generation, token)) return;

            StatusMessage = result.Message;
            if (result.Status == PriceVerifierSearchStatus.Success)
            {
                ReplaceItems(result.Items);
                SelectedItem = _items[0];
                ResultsFocusRequestVersion++;
            }
            else if (result.Status == PriceVerifierSearchStatus.OperationalFailure)
            {
                Complete(ProductSearchDialogResult.OperationalFailure());
            }
            else
            {
                SearchFocusRequestVersion++;
            }

            _logger.LogInformation(
                "Product search completed at {Stage} with {Status}",
                "Search",
                result.Status);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        finally
        {
            if (generation == Volatile.Read(ref _generation))
            {
                IsBusy = false;
            }
        }
    }

    private bool CanSelect(object? parameter) =>
        !IsBusy && (parameter as PriceVerifierSearchItem ?? SelectedItem) is not null;

    private void Select(object? parameter)
    {
        var item = parameter as PriceVerifierSearchItem ?? SelectedItem;
        if (item is not null)
        {
            Complete(ProductSearchDialogResult.Selected(item.Barcode));
        }
    }

    private void Cancel() => Complete(ProductSearchDialogResult.Canceled());

    private void Complete(ProductSearchDialogResult result)
    {
        if (Interlocked.CompareExchange(ref _completionState, 1, 0) != 0)
        {
            return;
        }

        _searchCancellation?.Cancel();
        CloseRequested?.Invoke(this, result);
    }

    private bool IsCurrent(long generation, CancellationToken token) =>
        generation == Volatile.Read(ref _generation)
        && !token.IsCancellationRequested
        && Volatile.Read(ref _completionState) == 0;

    private void ReplaceItems(IEnumerable<PriceVerifierSearchItem> items)
    {
        _items.Clear();
        foreach (var item in items)
        {
            _items.Add(item);
        }

        SelectedItem = null;
        OnPropertyChanged(nameof(Items));
    }

    private void DeactivateCore()
    {
        Interlocked.Increment(ref _generation);
        _searchCancellation?.Cancel();
        _searchCancellation?.Dispose();
        _searchCancellation = null;
        _lifecycleCancellation?.Cancel();
        _lifecycleCancellation?.Dispose();
        _lifecycleCancellation = null;
        IsBusy = false;
    }
}
