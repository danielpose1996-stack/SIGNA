using Avalonia.Controls;
using Avalonia.Interactivity;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;
public partial class EvaluationWindow : Window
{
    public EvaluationWindow(){InitializeComponent();Controls.WindowLayout.Configure(this);KeyDown+=(_,e)=>{if(e.Key==Avalonia.Input.Key.Escape)Close();};}
    private void Cancel(object? sender,RoutedEventArgs e)=>Close();
    private void Save(object? sender,RoutedEventArgs e){if(DataContext is EvaluationViewModel vm && vm.Save())Close();}
}
