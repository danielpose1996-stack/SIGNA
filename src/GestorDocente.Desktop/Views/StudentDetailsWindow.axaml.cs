using Avalonia.Controls;
using Avalonia.Interactivity;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;
public partial class StudentDetailsWindow:Window
{
    public StudentDetailsWindow(){InitializeComponent();Controls.WindowLayout.Configure(this);Closing+=(_,e)=>{if(DataContext is StudentDetailsViewModel{IsBusy:true})e.Cancel=true;};}
    private void CloseDialog(object? sender,RoutedEventArgs e)=>Close();
    private async void EditInformation(object? sender,RoutedEventArgs e)
    {
        if(DataContext is not StudentDetailsViewModel model || !model.CanManage)return;
        model.IsBusy=true;
        try{await new StudentEditorWindow{DataContext=model.Editor()}.ShowDialog(this);model.Refresh();}
        catch(Exception ex){model.ReportError(ex);}
        finally{model.IsBusy=false;}
    }
    private async void DeleteStudent(object? sender,RoutedEventArgs e)
    {
        if(DataContext is not StudentDetailsViewModel model || !model.CanManage)return;
        try
        {
            var preview=model.PreviewDeletion();model.IsBusy=true;bool confirmed;
            try{confirmed=await new ConfirmWindow($"Se eliminará «{preview.Student.FullName}» de esta materia, junto con {preview.GradeCount} notas y {preview.AttendanceCount} registros de asistencia de todos sus cortes. Esta acción no se puede deshacer. ¿Eliminar al estudiante?","Eliminar estudiante",scrollable:true){Width=540,Height=290}.ShowDialog<bool>(this);}
            finally{model.IsBusy=false;}
            if(confirmed && await model.DeleteAsync(preview,true))Close();
        }
        catch(Exception ex){model.ReportError(ex);}
    }
}
