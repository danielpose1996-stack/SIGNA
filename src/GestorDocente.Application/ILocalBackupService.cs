namespace GestorDocente.Application;

public sealed record LocalBackupResult(string Path,int CourseCount,int StudentCount,int SemesterCount=0);
public sealed record LocalRestoreResult(string PreviousBackupPath,int CourseCount,int StudentCount,bool PreviousCopyWasRaw=false,int SemesterCount=0);

public interface ILocalBackupService
{
    LocalBackupResult Create(string destination,string password);
    LocalBackupResult Inspect(string archive,string password);
    LocalRestoreResult Restore(string archive,string password);
}
