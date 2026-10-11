using Avalonia.Controls;
using Avalonia.Interactivity;
using GestorDocente.Domain;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;
public partial class RubricWindow : Window
{
    public RubricWindow(){InitializeComponent();Controls.WindowLayout.Configure(this);KeyDown+=(_,e)=>{if(e.Key==Avalonia.Input.Key.Escape)Close();};}
    private void CommitTables(){CriteriaTable.CommitEdit(DataGridEditingUnit.Row,true);LevelsTable.CommitEdit(DataGridEditingUnit.Row,true);}
    private void Cancel(object? sender,RoutedEventArgs e)=>Close();
    private void Save(object? sender,RoutedEventArgs e){CommitTables();if(DataContext is RubricEditorViewModel vm && vm.Save())Close();}
    private async void Preview(object? sender,RoutedEventArgs e)
    {
        CommitTables();if(DataContext is not RubricEditorViewModel vm)return;var text=vm.Preview();
        if(text.Length>0)await new InformationWindow("Vista previa de rúbrica",text).ShowDialog(this);
    }
    private void EditCriterion(object? sender,RoutedEventArgs e){if(sender is Button button){CriteriaTable.SelectedItem=button.DataContext;CriteriaTable.CurrentColumn=CriteriaTable.Columns[0];CriteriaTable.BeginEdit();}}
    private async void RemoveCriterion(object? sender,RoutedEventArgs e)
    {
        if(sender is Button {DataContext:RubricCriterion criterion} && DataContext is RubricEditorViewModel vm && await new ConfirmWindow("¿Eliminar este criterio del borrador?").ShowDialog<bool>(this))vm.Criteria.Remove(criterion);
    }
    private async void RemoveLevel(object? sender,RoutedEventArgs e)
    {
        if(sender is Button {DataContext:RubricLevel level} && DataContext is RubricEditorViewModel vm && await new ConfirmWindow("¿Eliminar este nivel del borrador?").ShowDialog<bool>(this))vm.Levels.Remove(level);
    }
}
