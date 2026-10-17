using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PureMediaPlayer.Mvvm;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void NotifyStateChanged([CallerMemberName] string? propertyIdentifier = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyIdentifier));
    }

    protected bool MutateField<TValue>(ref TValue backingStorage, TValue incomingValue, [CallerMemberName] string? propertyIdentifier = null)
    {
        if (EqualityComparer<TValue>.Default.Equals(backingStorage, incomingValue))
        {
            return false;
        }

        backingStorage = incomingValue;
        NotifyStateChanged(propertyIdentifier);
        return true;
    }
}