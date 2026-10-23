using GestorDocente.Domain;

namespace GestorDocente.Application;

public interface IBackupPreferencesStore
{
    string? LoadFolder();
    void SaveFolder(string folder);
}

public sealed class BackupPreferencesService(IBackupPreferencesStore store)
{
    public string? Folder {get;private set;}=store.LoadFolder();
    public async Task SetFolderAsync(string folder)
    {
        if(string.IsNullOrWhiteSpace(folder))throw new AcademicValidationException("Selecciona una carpeta para tus respaldos.");
        await Task.Run(()=>store.SaveFolder(folder));Folder=folder;
    }
}

public sealed class MemoryBackupPreferencesStore:IBackupPreferencesStore
{
    private string? folder;
    public string? LoadFolder()=>folder;
    public void SaveFolder(string value)=>folder=value;
}

public sealed record LocalDataResetResult(string Warning="");
public interface ILocalDataResetService
{
    LocalDataResetResult Reset(bool confirmed);
}
