using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Controls;
public partial class GradeEditor : UserControl
{
    public GradeEditor()=>InitializeComponent();
    private async void CommitOnLostFocus(object? sender,RoutedEventArgs e){if(DataContext is GradeCellViewModel cell)await cell.CommitAsync();}
    private async void CommitOnEnter(object? sender,KeyEventArgs e) {if(e.Key==Key.Enter){e.Handled=true;if(DataContext is GradeCellViewModel cell)await cell.CommitAsync();}}
}
