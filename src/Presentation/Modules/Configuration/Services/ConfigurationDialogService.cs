using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace SysTools.Presentation.Modules.Configuration.Services;

public sealed class ConfigurationDialogService : IConfigurationDialogService
{
    private readonly IServiceProvider _services;
    private readonly object _sync = new();
    private ConfigurationWindow? _active;
    private int _opening;

    public ConfigurationDialogService(IServiceProvider services) => _services = services;

    public Task<ConfigurationDialogResult> ShowDialogAsync(ConfigurationDialogMode mode, CancellationToken cancellationToken = default) =>
        ShowCoreAsync(mode, onlyIfRequired: false, cancellationToken);

    public Task<ConfigurationDialogResult> ShowIfRequiredAsync(CancellationToken cancellationToken = default) =>
        ShowCoreAsync(ConfigurationDialogMode.Initial, onlyIfRequired: true, cancellationToken);

    private async Task<ConfigurationDialogResult> ShowCoreAsync(ConfigurationDialogMode mode, bool onlyIfRequired, CancellationToken token)
    {
        if (Interlocked.CompareExchange(ref _opening, 1, 0) != 0)
            return new(ConfigurationDialogOutcome.Unavailable);
        try
        {
            token.ThrowIfCancellationRequested();
            var vm = _services.GetRequiredService<ConfigurationViewModel>();
            await vm.InitializeAsync(token);
            if (onlyIfRequired && !vm.RequiresInitialAttention)
            { vm.Deactivate(); return new(ConfigurationDialogOutcome.Canceled); }

            var window = new ConfigurationWindow(vm);
            if (Application.Current?.MainWindow is Window owner && owner != window) window.Owner = owner;
            var result = new ConfigurationDialogResult(ConfigurationDialogOutcome.Canceled);
            void Complete(object? sender, ConfigurationDialogResult value) => result = value;
            vm.CloseRequested += Complete;
            lock (_sync) _active = window;
            using var registration = token.Register(CloseActive);
            try { window.ShowDialog(); return result; }
            finally
            {
                vm.CloseRequested -= Complete;
                lock (_sync) if (ReferenceEquals(_active, window)) _active = null;
            }
        }
        finally { Volatile.Write(ref _opening, 0); }
    }

    public void CloseActive()
    {
        ConfigurationWindow? window; lock (_sync) window = _active;
        if (window is not null) window.Dispatcher.BeginInvoke(() => { if (window.IsVisible) window.Close(); });
    }
}
