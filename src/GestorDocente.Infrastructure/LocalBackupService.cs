using System.Buffers.Binary;
using System.Security.Cryptography;
using GestorDocente.Application;
using GestorDocente.Domain;
using Microsoft.Data.Sqlite;

namespace GestorDocente.Infrastructure;

/// <summary>Copias nuevas sin cifrado; la contraseña solo se utiliza para leer el formato histórico.</summary>
public sealed class LocalBackupService(string databasePath,Func<bool>? anotherInstance=null):ILocalBackupService
{
    private static readonly byte[] Magic="AULABK02"u8.ToArray();
    private static readonly byte[] LegacyMagic="AULABK01"u8.ToArray();
    private const int HeaderLength=48;
    private readonly string source=Path.GetFullPath(databasePath);
    private readonly Func<bool> anotherInstance=anotherInstance ?? LocalDatabaseMaintenance.HasAnotherInstance;

    public LocalBackupResult CreateInFolder(string folder)
    {
        if(string.IsNullOrWhiteSpace(folder) || !Path.IsPathFullyQualified(folder) || !Directory.Exists(folder))
            throw new AcademicValidationException("La carpeta de respaldos no está disponible. Selecciona otra carpeta; no se guardó en una ubicación diferente.");
        return Create(Path.Combine(folder,$"signa-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.aulabk"));
    }

    public LocalBackupResult Create(string destination)
    {
        var target=Path.GetFullPath(destination);
        if(SamePath(source,target) || File.Exists(target))
            throw new AcademicValidationException("Elige un archivo nuevo; no se sobrescriben respaldos ni la base operativa.");
        if(!Directory.Exists(Path.GetDirectoryName(target)))throw new AcademicValidationException("La carpeta de respaldos no existe.");
        var snapshot=TemporaryPath();var staged=target+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            Snapshot(snapshot);var details=SqliteBackupValidator.Validate(snapshot);
            WriteArchive(snapshot,staged);_=Inspect(staged);File.Move(staged,target);
            return new(target,details.Courses,details.Students,details.Semesters);
        }
        finally{DeleteTemporary(snapshot);DeleteTemporary(staged);}
    }

    public bool RequiresPassword(string archive)
    {
        using var input=new FileStream(archive,FileMode.Open,FileAccess.Read,FileShare.Read);
        Span<byte> magic=stackalloc byte[8];
        if(input.Read(magic)!=8)throw InvalidArchive();
        if(magic.SequenceEqual(LegacyMagic))return true;
        if(magic.SequenceEqual(Magic))return false;
        throw InvalidArchive();
    }

    public LocalBackupResult Inspect(string archive,string? legacyPassword=null)
    {
        var snapshot=TemporaryPath();
        try
        {
            Extract(archive,snapshot,legacyPassword);var details=SqliteBackupValidator.Validate(snapshot);
            return new(Path.GetFullPath(archive),details.Courses,details.Students,details.Semesters);
        }
        finally{DeleteTemporary(snapshot);}
    }

    public LocalRestoreResult Restore(string archive,string? legacyPassword=null)
    {
        if(SamePath(source,Path.GetFullPath(archive)))throw new AcademicValidationException("Elige un respaldo distinto de la base operativa.");
        LocalDatabaseMaintenance.RequireIdle(source,anotherInstance);
        var incoming=TemporaryPath();
        try
        {
            Extract(archive,incoming,legacyPassword);var details=SqliteBackupValidator.Validate(incoming);
            LocalDatabaseMaintenance.RequireIdle(source,anotherInstance);
            string previous="";var raw=false;
            if(File.Exists(source))
            {
                var recovery=Path.Combine(Path.GetDirectoryName(source)!,"Recovery");var newRecovery=!Directory.Exists(recovery);Directory.CreateDirectory(recovery);
                if(newRecovery && !OperatingSystem.IsWindows())File.SetUnixFileMode(recovery,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
                previous=Path.Combine(recovery,$"antes-restaurar-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.aulabk");
                raw=CreateRecovery(previous);
            }
            LocalDatabaseMaintenance.RequireIdle(source,anotherInstance);
            File.Move(incoming,source,true);
            return new(previous,details.Courses,details.Students,raw,details.Semesters);
        }
        finally{DeleteTemporary(incoming);}
    }

    private bool CreateRecovery(string target)
    {
        var snapshot=TemporaryPath();var staged=target+".tmp";var verification=TemporaryPath();
        try
        {
            var raw=false;
            try{Snapshot(snapshot);_=SqliteBackupValidator.Validate(snapshot);}
            catch(AcademicValidationException){raw=true;}
            catch(SqliteException ex)when(ex.SqliteErrorCode is 1 or 11 or 26){raw=true;}
            if(!raw){WriteArchive(snapshot,staged);_=Inspect(staged);}
            else
            {
                using var original=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.None);
                WriteArchive(original,staged);Extract(staged,verification,null);original.Position=0;
                using var restored=new FileStream(verification,FileMode.Open,FileAccess.Read,FileShare.Read);
                if(!CryptographicOperations.FixedTimeEquals(SHA256.HashData(original),SHA256.HashData(restored)))throw InvalidArchive();
            }
            File.Move(staged,target);return raw;
        }
        finally{DeleteTemporary(snapshot);DeleteTemporary(staged);DeleteTemporary(verification);}
    }

    private static void WriteArchive(string snapshot,string target)
    {
        using var input=new FileStream(snapshot,FileMode.Open,FileAccess.Read,FileShare.Read);WriteArchive(input,target);
    }
    private static void WriteArchive(Stream input,string target)
    {
        var header=new byte[HeaderLength];Magic.CopyTo(header,0);
        BinaryPrimitives.WriteInt64LittleEndian(header.AsSpan(8,8),input.Length);
        SHA256.HashData(input).CopyTo(header,16);input.Position=0;
        using var output=CreatePrivateFile(target);output.Write(header);input.CopyTo(output);output.Flush(true);
    }
    private void Extract(string archive,string target,string? legacyPassword)
    {
        if(RequiresPassword(archive))
        {
            if(string.IsNullOrEmpty(legacyPassword))throw new AcademicValidationException("Este respaldo antiguo está cifrado: requiere su contraseña original. Los respaldos nuevos no necesitan contraseña.");
            EncryptedLocalBackupService.Decrypt(archive,target,legacyPassword);return;
        }
        try
        {
            using var input=new FileStream(archive,FileMode.Open,FileAccess.Read,FileShare.Read);
            var header=new byte[HeaderLength];input.ReadExactly(header);
            if(!header.AsSpan(0,8).SequenceEqual(Magic))throw InvalidArchive();
            var length=BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(8,8));
            if(length<0 || length!=input.Length-HeaderLength)throw InvalidArchive();
            using(var output=CreatePrivateFile(target)){input.CopyTo(output);output.Flush(true);}
            using var verification=new FileStream(target,FileMode.Open,FileAccess.Read,FileShare.Read);
            if(!CryptographicOperations.FixedTimeEquals(header.AsSpan(16,32),SHA256.HashData(verification)))throw InvalidArchive();
        }
        catch(EndOfStreamException){throw InvalidArchive();}
    }
    private static FileStream CreatePrivateFile(string path)
    {
        var result=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None);
        try{if(!OperatingSystem.IsWindows())File.SetUnixFileMode(path,UnixFileMode.UserRead|UnixFileMode.UserWrite);return result;}
        catch{result.Dispose();throw;}
    }
    private void Snapshot(string path)=>new EncryptedLocalBackupService(source).Snapshot(path);
    private string TemporaryPath()=>Path.Combine(Path.GetDirectoryName(source)!,".signa-respaldo-"+Guid.NewGuid().ToString("N")+".db");
    private static bool SamePath(string left,string right)=>string.Equals(left,right,OperatingSystem.IsWindows()?StringComparison.OrdinalIgnoreCase:StringComparison.Ordinal);
    private static void DeleteTemporary(string path){if(File.Exists(path))File.Delete(path);}
    private static AcademicValidationException InvalidArchive()=>new("El respaldo no es compatible o está incompleto o alterado. Los datos actuales se conservaron.");
}
