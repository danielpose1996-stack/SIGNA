namespace GestorDocente.Application;

public sealed record LocalBackupResult(string Path,int CourseCount,int StudentCount);
public sealed record LocalRestoreResult(string PreviousBackupPath,int CourseCount,int StudentCount,bool PreviousCopyWasRaw=false);

public interface ILocalBackupService
{
    LocalBackupResult Create(string destination,string password);
    LocalBackupResult Inspect(string archive,string password);
    LocalRestoreResult Restore(string archive,string password);
}
