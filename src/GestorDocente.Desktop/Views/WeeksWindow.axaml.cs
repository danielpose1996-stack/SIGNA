using Avalonia.Controls;
using Avalonia.Interactivity;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;
public partial class WeeksWindow : Window
{
    public WeeksWindow(){InitializeComponent();Controls.WindowLayout.Configure(this);}
    private WeeksViewModel? Model=>DataContext as WeeksViewModel;
    private void Add(object? sender,RoutedEventArgs e)=>Model?.Add();
    private async void Remove(object? sender,RoutedEventArgs e){if(Model?.SelectedWeek is {} w && await new ConfirmWindow($"¿Retirar «{w.Label}»? Se conservarán sus estados en Recuperar retirados y dejarán de contarse en la tabla.","Retirar semana").ShowDialog<bool>(this))Model.Remove();}
    private void CloseDialog(object? sender,RoutedEventArgs e)=>Close();
}
