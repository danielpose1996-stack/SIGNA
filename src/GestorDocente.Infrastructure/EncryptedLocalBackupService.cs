using System.Buffers.Binary;
using System.Security.Cryptography;
using GestorDocente.Application;
using GestorDocente.Domain;
using Microsoft.Data.Sqlite;

namespace GestorDocente.Infrastructure;

/// <summary>Respaldo SQLite consistente y archivo portable autenticado, sin almacenar la contraseña.</summary>
public sealed class EncryptedLocalBackupService(string databasePath):ILocalBackupService
{
    private const int ChunkSize=1024*1024;
    private const int Iterations=600_000;
    private static readonly byte[] Magic="AULABK01"u8.ToArray();
    private readonly string source=Path.GetFullPath(databasePath);

    public LocalBackupResult Create(string destination,string password)
    {
        ValidatePassword(password);
        var target=Path.GetFullPath(destination);
        if(string.Equals(source,target,StringComparison.OrdinalIgnoreCase) || File.Exists(target))
            throw new AcademicValidationException("Elige un archivo de respaldo nuevo; no se sobrescriben archivos existentes.");
        if(!Directory.Exists(Path.GetDirectoryName(target)))
            throw new AcademicValidationException("La carpeta elegida para el respaldo no existe.");
        var snapshot=TemporaryDatabasePath();
        var staged=target+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            Snapshot(snapshot);
            var details=ValidateDatabase(snapshot);
            Encrypt(snapshot,staged,password);
            File.Move(staged,target);
            return new(target,details.Courses,details.Students);
        }
        finally{DeleteTemporary(snapshot);DeleteTemporary(staged);}
    }

    public LocalBackupResult Inspect(string archive,string password)
    {
        ValidatePassword(password);
        var snapshot=TemporaryDatabasePath();
        try
        {
            Decrypt(Path.GetFullPath(archive),snapshot,password);
            var details=ValidateDatabase(snapshot);
            return new(Path.GetFullPath(archive),details.Courses,details.Students);
        }
        finally{DeleteTemporary(snapshot);}
    }

    public LocalRestoreResult Restore(string archive,string password)
    {
        ValidatePassword(password);
        var incoming=TemporaryDatabasePath();
        try
        {
            Decrypt(Path.GetFullPath(archive),incoming,password);
            var details=ValidateDatabase(incoming);
            if(File.Exists(source+"-wal") || File.Exists(source+"-shm") || File.Exists(source+"-journal"))
                throw new AcademicValidationException("Cierra otras instancias y vuelve a intentar la restauración; hay archivos SQLite activos.");
            string previous="";
            var raw=false;
            if(File.Exists(source))
            {
                var recovery=Path.Combine(Path.GetDirectoryName(source)!,"Recovery");
                var newRecovery=!Directory.Exists(recovery);
                Directory.CreateDirectory(recovery);
                if(newRecovery && !OperatingSystem.IsWindows())File.SetUnixFileMode(recovery,UnixFileMode.UserRead|UnixFileMode.UserWrite|UnixFileMode.UserExecute);
                previous=Path.Combine(recovery,$"antes-restaurar-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.aulabk");
                raw=CreateRecovery(previous,password);
            }
            File.Move(incoming,source,true);
            return new(previous,details.Courses,details.Students,raw);
        }
        finally{DeleteTemporary(incoming);}
    }

    private bool CreateRecovery(string target,string password)
    {
        var snapshot=TemporaryDatabasePath();var staged=target+".tmp";
        try
        {
            var raw=false;
            try{Snapshot(snapshot);_=ValidateDatabase(snapshot);}
            catch(AcademicValidationException){raw=true;}
            catch(SqliteException ex)when(ex.SqliteErrorCode is 1 or 11 or 26){raw=true;}
            if(!raw)
            {
                Encrypt(snapshot,staged,password);
                _=Inspect(staged,password);
                File.Move(staged,target);return false;
            }
            // La base existente ya está dañada. Conservar sus bytes exactos bajo
            // acceso exclusivo; esta copia de emergencia no se anuncia como DB válida.
            DeleteTemporary(snapshot);
            using var original=new FileStream(source,FileMode.Open,FileAccess.Read,FileShare.None);
            Encrypt(original,staged,password);
            var verification=TemporaryDatabasePath();
            try
            {
                Decrypt(staged,verification,password);
                original.Position=0;
                using var reopened=new FileStream(verification,FileMode.Open,FileAccess.Read,FileShare.Read);
                if(!CryptographicOperations.FixedTimeEquals(SHA256.HashData(original),SHA256.HashData(reopened)))
                    throw new AcademicValidationException("La copia preventiva no superó la verificación; la restauración se canceló.");
            }
            finally{DeleteTemporary(verification);}
            File.Move(staged,target);return true;
        }
        finally{DeleteTemporary(snapshot);DeleteTemporary(staged);}
    }

    private string TemporaryDatabasePath()=>Path.Combine(Path.GetDirectoryName(source)!,
        ".aula-respaldo-"+Guid.NewGuid().ToString("N")+".db");

    private void Snapshot(string target)
    {
        if(!File.Exists(source))throw new AcademicValidationException("No existe una base local para respaldar.");
        if(!OperatingSystem.IsWindows())
        {
            using(var created=new FileStream(target,FileMode.CreateNew,FileAccess.Write,FileShare.None)){}
            File.SetUnixFileMode(target,UnixFileMode.UserRead|UnixFileMode.UserWrite);
        }
        using var input=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=source,Mode=SqliteOpenMode.ReadOnly,Pooling=false,DefaultTimeout=5}.ToString());
        using var output=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=target,Mode=SqliteOpenMode.ReadWriteCreate,Pooling=false,DefaultTimeout=5}.ToString());
        input.Open();output.Open();input.BackupDatabase(output);
    }

    private static (int Courses,int Students) ValidateDatabase(string path)
    {
        using(var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Mode=SqliteOpenMode.ReadOnly,Pooling=false}.ToString()))
        {
            db.Open();
            using var command=db.CreateCommand();
            command.CommandText="PRAGMA user_version";
            if(Convert.ToInt32(command.ExecuteScalar())!=1)throw new AcademicValidationException("La versión de la base de respaldo no es compatible.");
            command.CommandText="PRAGMA integrity_check";
            if(!string.Equals(command.ExecuteScalar()?.ToString(),"ok",StringComparison.OrdinalIgnoreCase))
                throw new AcademicValidationException("La base del respaldo no supera la verificación de integridad.");
            command.CommandText="PRAGMA foreign_key_check";
            using var foreignKeys=command.ExecuteReader();
            if(foreignKeys.Read())throw new AcademicValidationException("El respaldo contiene relaciones incompletas.");
        }
        var catalog=new CatalogService(new SqliteCatalogStore(path));
        var courses=0;var students=0;
        foreach(var semester in catalog.Semesters)
            foreach(var course in catalog.Courses(semester.Id))
            {
                var workspace=catalog.Open(course.Id).Current;
                courses++;students+=workspace.Students.Count;
            }
        return(courses,students);
    }

    private static void Encrypt(string snapshot,string destination,string password)
    {
        using var input=new FileStream(snapshot,FileMode.Open,FileAccess.Read,FileShare.Read);
        Encrypt(input,destination,password);
    }
    private static void Encrypt(Stream input,string destination,string password)
    {
        var header=new byte[36];Magic.CopyTo(header,0);
        RandomNumberGenerator.Fill(header.AsSpan(8,24));
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(32,4),Iterations);
        var key=Rfc2898DeriveBytes.Pbkdf2(password,header.AsSpan(8,16),Iterations,HashAlgorithmName.SHA256,32);
        try
        {
            using var aes=new AesGcm(key,16);
            using var output=new FileStream(destination,FileMode.CreateNew,FileAccess.Write,FileShare.None);
            if(!OperatingSystem.IsWindows())File.SetUnixFileMode(destination,UnixFileMode.UserRead|UnixFileMode.UserWrite);
            output.Write(header);
            var plain=new byte[ChunkSize];var cipher=new byte[ChunkSize];var tag=new byte[16];
            for(uint sequence=0;;sequence++)
            {
                var length=input.Read(plain);
                var aad=AssociatedData(header,sequence,length);
                var nonce=Nonce(header,sequence);
                aes.Encrypt(nonce,plain.AsSpan(0,length),cipher.AsSpan(0,length),tag,aad);
                output.Write(aad.AsSpan(^4));output.Write(cipher.AsSpan(0,length));output.Write(tag);
                if(length==0)break;
                if(sequence==uint.MaxValue)throw new AcademicValidationException("La base supera el tamaño admitido para este formato de respaldo.");
            }
            output.Flush(true);
        }
        finally{CryptographicOperations.ZeroMemory(key);}
    }

    private static void Decrypt(string archive,string destination,string password)
    {
        try
        {
            using var input=new FileStream(archive,FileMode.Open,FileAccess.Read,FileShare.Read);
            var header=new byte[36];input.ReadExactly(header);
            if(!header.AsSpan(0,8).SequenceEqual(Magic))throw new AcademicValidationException("El archivo no tiene un formato de respaldo compatible.");
            var iterations=BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(32,4));
            if(iterations is <200_000 or >2_000_000)throw new AcademicValidationException("El formato de clave del respaldo no es compatible.");
            var key=Rfc2898DeriveBytes.Pbkdf2(password,header.AsSpan(8,16),iterations,HashAlgorithmName.SHA256,32);
            try
            {
                using var aes=new AesGcm(key,16);
                using var output=new FileStream(destination,FileMode.CreateNew,FileAccess.Write,FileShare.None);
                if(!OperatingSystem.IsWindows())File.SetUnixFileMode(destination,UnixFileMode.UserRead|UnixFileMode.UserWrite);
                var cipher=new byte[ChunkSize];var plain=new byte[ChunkSize];var tag=new byte[16];var lengthBytes=new byte[4];
                for(uint sequence=0;;sequence++)
                {
                    input.ReadExactly(lengthBytes);
                    var length=BinaryPrimitives.ReadInt32LittleEndian(lengthBytes);
                    if(length is <0 or >ChunkSize)throw new InvalidDataException();
                    input.ReadExactly(cipher.AsSpan(0,length));input.ReadExactly(tag);
                    aes.Decrypt(Nonce(header,sequence),cipher.AsSpan(0,length),tag,plain.AsSpan(0,length),AssociatedData(header,sequence,length));
                    if(length==0)
                    {
                        if(input.ReadByte()!=-1)throw new InvalidDataException();
                        output.Flush(true);break;
                    }
                    output.Write(plain.AsSpan(0,length));
                    if(sequence==uint.MaxValue)throw new InvalidDataException();
                }
            }
            finally{CryptographicOperations.ZeroMemory(key);}
        }
        catch(Exception ex) when(ex is CryptographicException or EndOfStreamException or InvalidDataException)
        {throw new AcademicValidationException("Contraseña incorrecta o archivo de respaldo incompleto o alterado.");}
    }

    private static byte[] Nonce(byte[] header,uint sequence)
    {
        var nonce=new byte[12];header.AsSpan(24,8).CopyTo(nonce);
        BinaryPrimitives.WriteUInt32LittleEndian(nonce.AsSpan(8,4),sequence);
        return nonce;
    }
    private static byte[] AssociatedData(byte[] header,uint sequence,int length)
    {
        var aad=new byte[44];header.CopyTo(aad,0);
        BinaryPrimitives.WriteUInt32LittleEndian(aad.AsSpan(36,4),sequence);
        BinaryPrimitives.WriteInt32LittleEndian(aad.AsSpan(40,4),length);
        return aad;
    }
    private static void ValidatePassword(string password)
    {
        if(string.IsNullOrWhiteSpace(password) || password.Length<12)
            throw new AcademicValidationException("Usa una contraseña de al menos 12 caracteres y consérvala para restaurar en otro equipo.");
    }
    private static void DeleteTemporary(string path)
    {
        if(File.Exists(path))File.Delete(path);
    }
}
