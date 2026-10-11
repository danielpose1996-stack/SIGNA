using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
namespace GestorDocente.Desktop.ViewModels;

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void Raise([CallerMemberName] string? name=null)=>PropertyChanged?.Invoke(this,new(name));
    protected bool Set<T>(ref T field,T value,[CallerMemberName] string? name=null)
    {
        if(EqualityComparer<T>.Default.Equals(field,value))return false;
        field=value;Raise(name);return true;
    }
}
public sealed class RelayCommand(Action<object?> execute,Func<object?,bool>? canExecute=null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter)=>canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter)=>execute(parameter);
    public void Refresh()=>CanExecuteChanged?.Invoke(this,EventArgs.Empty);
}
