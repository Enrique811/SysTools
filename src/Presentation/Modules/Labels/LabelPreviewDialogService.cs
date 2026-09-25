using System.Windows;
using SysTools.Entities.Labels;

namespace SysTools.Presentation.Modules.Labels;

public sealed class LabelPreviewDialogService : ILabelPreviewDialogService
{
    private readonly object _sync = new();
    private LabelPreviewWindow? _active;
    public Task ShowAsync(LabelPreviewDocument document, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var window = new LabelPreviewWindow(new LabelPreviewViewModel(document));
        if (Application.Current?.MainWindow is Window owner) window.Owner = owner;
        lock (_sync)
        {
            if (_active is not null) throw new InvalidOperationException("A preview is already open.");
            _active = window;
        }
        using var registration = cancellationToken.Register(CloseActive);
        try { window.ShowDialog(); }
        finally { lock (_sync) if (ReferenceEquals(_active, window)) _active = null; }
        return Task.CompletedTask;
    }
    public void CloseActive()
    {
        LabelPreviewWindow? value; lock (_sync) value = _active;
        value?.Dispatcher.BeginInvoke(() => { if (value.IsVisible) value.Close(); });
    }
}
