using GestorDocente.Application;

namespace GestorDocente.Desktop.Views;

public partial class MainWindow
{
    private async void DeleteStudentList()
    {
        if(observed is not {} model || !model.CanDeleteStudentList)return;
        try
        {
            var preview=model.PreviewStudentListDeletion();using var operation=model.BeginStudentListDeletion();
            var message=$"Asignatura: {preview.CourseName}\nCódigo: {preview.CourseCode} · Período: {preview.Period} · Grupo: {preview.Group}\n\nSe eliminarán {preview.StudentCount} estudiantes de la lista completa, junto con {preview.GradeCount} notas y {preview.AttendanceCount} registros de asistencia de todos los cortes, incluidos los registros de actividades o semanas retiradas. El filtro de búsqueda no limita esta eliminación.\n\nLa materia, sus cortes, actividades, subactividades, semanas y configuraciones se conservarán. Las demás materias no se modificarán.\n\nSolo podrás recuperar lo eliminado restaurando un respaldo anterior. ¿Eliminar la lista completa?";
            bool confirmed;ModalOverlay.IsVisible=true;
            try
            {
                confirmed=await new ConfirmWindow(message,"Eliminar lista",scrollable:true,destructive:true)
                    {Title="Eliminar lista de estudiantes",Width=660,Height=440}.ShowDialog<bool>(this);
            }
            finally{ModalOverlay.IsVisible=false;}
            if(!confirmed)return;
            model.Message="Eliminando la lista de estudiantes de esta materia…";
            await model.DeleteStudentListAsync(preview,true);
        }
        catch(AcademicRefreshException ex)
        {model.Message="La eliminación se guardó, pero no se pudo actualizar la pantalla. Pulsa Actualizar antes de continuar. "+ex.Message;}
        catch(Exception ex){model.Message=SafeError(ex);}
    }
}
