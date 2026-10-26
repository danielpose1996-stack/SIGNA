using GestorDocente.Application;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;

public sealed partial class MainViewModel
{
    public RelayCommand DeleteStudentListCommand{get;}
    public event Action? StudentListDeletionRequested;
    private bool deletingStudentList;
    public bool IsDeletingStudentList=>deletingStudentList;
    public bool CanDeleteStudentList=>HasOpenCourse && Service.Current.Students.Count>0 && !FileBusy && !Service.IsSaving;
    public IDisposable BeginStudentListDeletion()
    {
        if(!CanDeleteStudentList)throw new AcademicValidationException("Espera a que termine la operación antes de eliminar la lista.");
        EnsureStudentListHasNoPendingEdits();
        deletingStudentList=true;FileBusy=true;Raise(nameof(CanInteractWithWorkspace));
        return new UpdateMaintenance(()=>{deletingStudentList=false;FileBusy=false;Raise(nameof(CanInteractWithWorkspace));});
    }
    public StudentListDeletionPreview PreviewStudentListDeletion()
    {
        if(!CanDeleteStudentList)throw new AcademicValidationException("Abre una materia con estudiantes y espera a que terminen las operaciones antes de eliminar la lista.");
        EnsureStudentListHasNoPendingEdits();
        return Service.PreviewStudentListDeletion();
    }
    public async Task DeleteStudentListAsync(StudentListDeletionPreview preview,bool confirmed)
    {
        EnsureStudentListHasNoPendingEdits();
        await Service.DeleteStudentListAsync(preview,confirmed);
        Search="";
        Message=$"Lista eliminada: {preview.StudentCount} estudiantes, {preview.GradeCount} notas y {preview.AttendanceCount} registros de asistencia. La materia y su configuración se conservaron. Puedes importar o agregar un nuevo listado.";
    }
    private void EnsureStudentListHasNoPendingEdits()
    {
        if(allGradeRows.Any(r=>r.HasPendingEdits) || allAttendanceRows.Any(r=>r.HasPendingEdits))
            throw new AcademicValidationException("Hay notas o asistencias pendientes de guardar. Guárdalas o corrige los campos antes de eliminar la lista.");
    }
}
