using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;

public sealed partial class MainViewModel
{
    public bool SelectedCutLocked=>Service.Cut(SelectedCut).IsLocked;
    public bool CanEditSelectedCut=>HasOpenCourse && !SelectedCutLocked && !FileBusy && !Service.IsSaving;
    public string CutLockLabel=>SelectedCutLocked ? "Bloqueado" : "Desbloqueado";
    public string CutLockIcon=>SelectedCutLocked ? "lock" : "unlock";
    public string CutLockHint=>SelectedCutLocked ? "Desbloquear este corte para corregir notas y asistencias" : "Bloquear este corte para proteger sus notas y asistencias";
    public RelayCommand ToggleCutLockCommand {get;}
    public RelayCommand CopyGradesCommand {get;}
    public event Action<int>? CutLockRequested;
    public event Action<string?>? GradeCopyRequested;

    private bool CanCopyGrades(object? target)=>HasOpenCourse && !FileBusy && !Service.IsSaving &&
        GradeRows.Count>0 && (target is string || target is null && AttendanceGradingEnabled);

    public string GetGradeClipboardText(string? targetId)
    {
        FlushSearch();
        if(targetId is null)
        {
            if(!AttendanceGradingEnabled)throw new AcademicValidationException("La nota de asistencia está desactivada en este corte.");
            return string.Join(Environment.NewLine,AttendanceRows.Select(row=>
                row.AttendanceGradeValue?.ToString("0.00",AcademicFormatting.Culture) ?? ""));
        }
        return string.Join(Environment.NewLine,GradeRows.Select(row=>
        {
            if(!row.Cells.TryGetValue(targetId,out var cell))
                throw new AcademicValidationException("Selecciona una actividad o subactividad de este corte.");
            if(string.IsNullOrWhiteSpace(cell.Input))return "";
            if(!AcademicFormatting.TryDecimal(cell.Input,out var value) || value is <0 or >5)
                throw new AcademicValidationException($"Revisa la nota de la fila {row.Number} antes de copiar la columna.");
            return value.ToString("0.00",AcademicFormatting.Culture);
        }));
    }

    private void RefreshCutPermissions()
    {
        foreach(var name in new[]{nameof(SelectedCutLocked),nameof(CanEditSelectedCut),nameof(CutLockLabel),nameof(CutLockIcon),nameof(CutLockHint)})
            Raise(name);
        ToggleCutLockCommand?.Refresh();CopyGradesCommand?.Refresh();
        Raise(nameof(CanAddWeek));AddWeekCommand.Refresh();
        PasteGradesCommand.Refresh();AssignGradeToAllCommand.Refresh();ConfigureCommand.Refresh();
        ConfigureAttendanceGradingCommand.Refresh();ManageWeeksCommand.Refresh();RecoveryCommand.Refresh();
    }
}
