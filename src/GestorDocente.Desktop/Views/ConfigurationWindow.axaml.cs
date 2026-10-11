using Avalonia.Controls;
using Avalonia.Interactivity;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Domain;
namespace GestorDocente.Desktop.Views;
public partial class ConfigurationWindow : Window
{
    public ConfigurationWindow(){InitializeComponent();Controls.WindowLayout.Configure(this);KeyDown+=(_,e)=>{if(e.Key==Avalonia.Input.Key.Escape)Close();};}
    private ConfigurationViewModel? Model=>DataContext as ConfigurationViewModel;
    private void Cancel(object? sender,RoutedEventArgs e)=>Close();
    private void Save(object? sender,RoutedEventArgs e){GroupsTable.CommitEdit(DataGridEditingUnit.Row,true);ActivitiesTable.CommitEdit(DataGridEditingUnit.Row,true);if(Model?.Save()==true)Close();}
    private void AddGroup(object? sender,RoutedEventArgs e)=>Model?.AddGroup();
    private void AddActivity(object? sender,RoutedEventArgs e)=>Model?.AddActivity();
    private async void RemoveGroup(object? sender,RoutedEventArgs e){if(Model?.SelectedGroup is {} group && await new ConfirmWindow("¿Eliminar este grupo del borrador? Si tiene notas, el guardado será rechazado.").ShowDialog<bool>(this))Model.RemoveGroup(group);}
    private async void RemoveActivity(object? sender,RoutedEventArgs e){if(ActivitiesTable.SelectedItem is EvaluationActivity activity && await new ConfirmWindow("¿Eliminar esta actividad del borrador? Si tiene notas, el guardado será rechazado.").ShowDialog<bool>(this))Model?.RemoveActivity(activity);}
}
