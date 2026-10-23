namespace GestorDocente.Application;

public sealed record LocalBackupResult(string Path,int CourseCount,int StudentCount,int SemesterCount=0);
public sealed record LocalRestoreResult(string PreviousBackupPath,int CourseCount,int StudentCount,bool PreviousCopyWasRaw=false,int SemesterCount=0);

public interface ILocalBackupService
{
    LocalBackupResult Create(string destination);
    LocalBackupResult CreateInFolder(string folder);
    bool RequiresPassword(string archive);
    LocalBackupResult Inspect(string archive,string? legacyPassword=null);
    LocalRestoreResult Restore(string archive,string? legacyPassword=null);
}
