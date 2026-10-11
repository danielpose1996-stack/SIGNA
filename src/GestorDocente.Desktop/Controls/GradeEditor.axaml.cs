using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Controls;
public partial class GradeEditor : UserControl
{
    public GradeEditor()=>InitializeComponent();
    private void CommitOnLostFocus(object? sender,RoutedEventArgs e)=>(DataContext as GradeCellViewModel)?.Commit();
    private void CommitOnEnter(object? sender,KeyEventArgs e) {if(e.Key==Key.Enter){(DataContext as GradeCellViewModel)?.Commit();e.Handled=true;}}
}
