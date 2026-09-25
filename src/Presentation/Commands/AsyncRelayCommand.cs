using System.Windows.Input;

namespace SysTools.Presentation.Commands;

public sealed class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private readonly Func<bool>? _canExecute;
    private int _isExecuting;

    public AsyncRelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool IsExecuting => Volatile.Read(ref _isExecuting) != 0;

    public bool CanExecute(object? parameter) => !IsExecuting && (_canExecute?.Invoke() ?? true);

    public async Task ExecuteAsync()
    {
        if (!(_canExecute?.Invoke() ?? true)
            || Interlocked.CompareExchange(ref _isExecuting, 1, 0) != 0)
        {
            return;
        }

        NotifyCanExecuteChanged();
        try
        {
            await _execute();
        }
        finally
        {
            Volatile.Write(ref _isExecuting, 0);
            NotifyCanExecuteChanged();
        }
    }

    public async void Execute(object? parameter)
    {
        try
        {
            await ExecuteAsync();
        }
        catch
        {
            // The awaitable path exposes failures to tests/callers. ICommand must not
            // surface an async-void exception to the WPF dispatcher.
        }
    }

    public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

