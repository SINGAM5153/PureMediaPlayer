using System.Windows.Input;

namespace PureMediaPlayer.Mvvm;

public sealed class AsyncActionCommand : ICommand
{
    private readonly Func<object?, CancellationToken, Task> _asyncTaskFactory;
    private readonly Predicate<object?>? _canRunCondition;
    private CancellationTokenSource? _activeTokenSource;
    private bool _isCurrentlyRunning;

    public AsyncActionCommand(Func<object?, CancellationToken, Task> asyncTaskFactory, Predicate<object?>? canRunCondition = null)
    {
        _asyncTaskFactory = asyncTaskFactory ?? throw new ArgumentNullException(nameof(asyncTaskFactory));
        _canRunCondition = canRunCondition;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter)
    {
        if (_isCurrentlyRunning) return false;
        return _canRunCondition?.Invoke(parameter) ?? true;
    }

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        _isCurrentlyRunning = true;
        _activeTokenSource = new CancellationTokenSource();
        CommandManager.InvalidateRequerySuggested();

        try
        {
            await _asyncTaskFactory(parameter, _activeTokenSource.Token);
        }
        finally
        {
            _isCurrentlyRunning = false;
            _activeTokenSource.Dispose();
            _activeTokenSource = null;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public void CancelOngoingExecution() => _activeTokenSource?.Cancel();
}