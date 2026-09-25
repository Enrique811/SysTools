using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace SysTools.Presentation.Modules.PriceVerifier.Search;

public sealed class ProductSearchDialogService : IProductSearchDialogService
{
    private readonly IServiceProvider _services;
    private readonly object _sync = new();
    private ProductSearchWindow? _activeWindow;

    public ProductSearchDialogService(IServiceProvider services) => _services = services;

    public ProductSearchDialogResult ShowDialog(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var viewModel = _services.GetRequiredService<ProductSearchViewModel>();
        var window = new ProductSearchWindow(viewModel) { LifecycleToken = cancellationToken };
        if (Application.Current?.MainWindow is Window owner && owner != window)
        {
            window.Owner = owner;
        }

        ProductSearchDialogResult result = ProductSearchDialogResult.Canceled();
        void Complete(object? sender, ProductSearchDialogResult value)
        {
            result = value;
            if (window.IsVisible) window.Close();
        }

        viewModel.CloseRequested += Complete;
        lock (_sync) _activeWindow = window;
        using var registration = cancellationToken.Register(CloseActive);
        try
        {
            window.ShowDialog();
            return result;
        }
        finally
        {
            viewModel.CloseRequested -= Complete;
            lock (_sync)
            {
                if (ReferenceEquals(_activeWindow, window)) _activeWindow = null;
            }
        }
    }

    public void CloseActive()
    {
        ProductSearchWindow? window;
        lock (_sync) window = _activeWindow;
        if (window is null) return;
        window.Dispatcher.BeginInvoke(() =>
        {
            if (window.IsVisible) window.Close();
        });
    }
}
