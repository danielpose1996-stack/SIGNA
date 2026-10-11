using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace GestorDocente.Desktop.ViewModels;

public sealed class ObservableRangeCollection<T>:ObservableCollection<T>
{
    public void ReplaceAll(IEnumerable<T> values)
    {
        var next=values.ToList();CheckReentrancy();
        Items.Clear();foreach(var item in next)Items.Add(item);
        OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}
