using System.Windows.Input;

namespace PureMediaPlayer.Mvvm;

public sealed class ActionCommand : ICommand
{
    private readonly Action<object?> _invocationTarget;
    private readonly Predicate<object?>? _validationPredicate;

    public ActionCommand(Action<object?> invocationTarget, Predicate<object?>? validationPredicate = null)
    {
        _invocationTarget = invocationTarget ?? throw new ArgumentNullException(nameof(invocationTarget));
        _validationPredicate = validationPredicate;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _validationPredicate?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _invocationTarget(parameter);
}