using GestorDocente.Application;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.ViewModels;

public sealed partial class MainViewModel
{
    public BackupPreferencesService BackupPreferences{get;}
    public ILocalDataResetService? DataReset{get;}
    public string? BackupFolder=>BackupPreferences.Folder;
    public string BackupFolderLabel=>BackupFolder ?? "Todavía no has seleccionado una carpeta";
    public string BackupFolderAction=>BackupFolder is null ? "Elegir carpeta" : "Cambiar carpeta";
    public RelayCommand ChooseBackupFolderCommand{get;}
    public RelayCommand ClearLocalDataCommand{get;}
    public event Action? BackupFolderRequested;
    public event Action? LocalDataResetRequested;
    private bool maintainingLocalData;
    public bool IsMaintainingLocalData=>maintainingLocalData;
    public bool CanInteractWithWorkspace=>!maintainingLocalData && Updates.CanUseApplication;
    public async Task SetBackupFolderAsync(string folder)
    {
        await BackupPreferences.SetFolderAsync(folder);
        foreach(var name in new[]{nameof(BackupFolder),nameof(BackupFolderLabel),nameof(BackupFolderAction)})Raise(name);
        CreateBackupCommand.Refresh();
    }
    public IDisposable BeginLocalDataMaintenance()
    {
        if(FileBusy || Service.IsSaving || Appearance.IsBusy)throw new AcademicValidationException("Espera a que termine la operación o el guardado antes de continuar.");
        if(allGradeRows.Any(r=>r.HasPendingEdits) || allAttendanceRows.Any(r=>r.HasPendingEdits))
            throw new AcademicValidationException("Hay notas o asistencias pendientes. Guárdalas o revisa los campos antes de generar, restaurar o limpiar datos.");
        maintainingLocalData=true;FileBusy=true;Raise(nameof(CanInteractWithWorkspace));
        return new UpdateMaintenance(()=>{maintainingLocalData=false;FileBusy=false;Raise(nameof(CanInteractWithWorkspace));});
    }
}
