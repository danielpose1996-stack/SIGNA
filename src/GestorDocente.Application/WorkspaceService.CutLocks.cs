using GestorDocente.Domain;

namespace GestorDocente.Application;

public sealed partial class WorkspaceService
{
    public void EnsureCutEditable(int number)
    {
        if(Cut(number).IsLocked)
            throw new AcademicValidationException($"«{Cut(number).Label}» está bloqueado. Desbloquéalo para modificar sus notas, actividades o asistencias.");
    }

    private void EnsureStudentRecordsEditable(HashSet<string> studentIds)
    {
        foreach(var cut in Current.Cuts.Where(c=>c.IsLocked))
            if(cut.Grades.Any(g=>studentIds.Contains(g.StudentId)) || cut.Attendance.Any(a=>studentIds.Contains(a.StudentId)))
                EnsureCutEditable(cut.Number);
    }

    public Task SetCutLockedAsync(int number,bool locked,AcademicWorkspace? expectedSource=null)
    {
        if(expectedSource is not null && !ReferenceEquals(Current,expectedSource))
            throw new AcademicValidationException("La materia cambió durante la confirmación. Revisa el corte y confirma nuevamente.");
        return ChangeAsync(number,cut=>cut.IsLocked=locked,new(WorkspaceChangeKind.Structure,number),
            cut=>cut.IsLocked==locked,changingLock:true);
    }
}
