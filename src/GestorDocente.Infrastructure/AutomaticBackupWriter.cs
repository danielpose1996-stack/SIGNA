using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using GestorDocente.Application;
using GestorDocente.Domain;

namespace GestorDocente.Infrastructure;

/// <summary>Rotates only registered automatic files, never archives discovered by a wildcard.</summary>
public sealed partial class AutomaticBackupWriter(ILocalBackupService backup):IAutomaticBackupWriter
{
    private const string IndexName=".signa-automaticos.json";
    private sealed record Entry(string Name,string? Sha256);
    public LocalBackupResult Create(string folder)
    {
        if(!Path.IsPathFullyQualified(folder) || !Directory.Exists(folder))
            throw new AcademicValidationException("La carpeta de respaldos no está disponible. Elige otra desde Respaldos.");
        folder=Path.GetFullPath(folder);
        var lockPath=Path.Combine(folder,".signa-automaticos.lock");RejectLink(lockPath);
        using var lease=new FileStream(lockPath,FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var indexPath=Path.Combine(folder,IndexName);RejectLink(indexPath);
        var entries=ReadIndex(indexPath);
        // Reconcile interrupted reservations, without adopting unregistered files.
        foreach(var entry in entries.ToArray())
        {
            var path=Path.Combine(folder,entry.Name);RejectLink(path);
            if(!File.Exists(path)){entries.Remove(entry);continue;}
            var digest=Hash(path);
            if(entry.Sha256 is not null && digest!=entry.Sha256)
                throw new AcademicValidationException("Una copia automática fue modificada fuera de SIGNA. No se eliminó ningún respaldo. Conserva ese archivo y revisa el índice .signa-automaticos.json de la carpeta.");
            if(entry.Sha256 is null)
            {
                _=backup.Inspect(path);
                entries[entries.IndexOf(entry)]=entry with{Sha256=digest};
            }
        }
        var name=$"signa-auto-{DateTime.UtcNow:yyyyMMdd-HHmmssfffffff}-{Guid.NewGuid():N}.aulabk";
        var reserved=new Entry(name,null);entries.Add(reserved);
        // Persist ownership before creating the archive, so a crash cannot leave an untracked automatic.
        WriteIndex(indexPath,entries);
        var result=backup.Create(Path.Combine(folder,name));
        entries[^1]=reserved with{Sha256=Hash(result.Path)};
        WriteIndex(indexPath,entries);
        while(entries.Count>5)
        {
            var oldest=entries[0];var path=Path.Combine(folder,oldest.Name);RejectLink(path);
            if(File.Exists(path))
            {
                if(Hash(path)!=oldest.Sha256)throw new AcademicValidationException("La copia automática más antigua cambió. La nueva copia se conservó; no se eliminó el archivo modificado.");
                File.Delete(path);
            }
            entries.RemoveAt(0);WriteIndex(indexPath,entries);
        }
        return result;
    }
    private static List<Entry> ReadIndex(string path)
    {
        if(!File.Exists(path))return [];
        if(new FileInfo(path).Length>65536)throw InvalidIndex();
        List<Entry>? entries;
        try{entries=JsonSerializer.Deserialize<List<Entry>>(File.ReadAllBytes(path));}
        catch(JsonException){throw InvalidIndex();}
        if(entries is null || entries.Count>128 || entries.Any(e=>e is null || !AllowedName().IsMatch(e.Name ?? "") ||
            e.Sha256 is not null && !AllowedHash().IsMatch(e.Sha256)) || entries.Select(e=>e.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=entries.Count)
            throw InvalidIndex();
        return entries;
    }
    private static void WriteIndex(string path,List<Entry> entries)
    {
        var temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(var stream=new FileStream(temporary,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {
                if(!OperatingSystem.IsWindows())File.SetUnixFileMode(temporary,UnixFileMode.UserRead|UnixFileMode.UserWrite);
                JsonSerializer.Serialize(stream,entries);stream.Flush(true);
            }
            File.Move(temporary,path,true);
        }
        finally{if(File.Exists(temporary))File.Delete(temporary);}
    }
    private static string Hash(string path)
    {using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);return Convert.ToHexString(SHA256.HashData(stream));}
    private static void RejectLink(string path)
    {
        if(new FileInfo(path).LinkTarget is not null || Directory.Exists(path) || File.Exists(path) && (File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)
            throw new AcademicValidationException("Un archivo de control o respaldo automático es un enlace. No se eliminó ningún respaldo.");
    }
    private static AcademicValidationException InvalidIndex()=>new("El índice de respaldos automáticos no es válido. No se eliminó ningún archivo. Conserva tus copias y selecciona otra carpeta o revisa .signa-automaticos.json.");
    [GeneratedRegex(@"\Asigna-auto-\d{8}-\d{13}-[a-f0-9]{32}\.aulabk\z",RegexOptions.CultureInvariant)]
    private static partial Regex AllowedName();
    [GeneratedRegex(@"\A[A-F0-9]{64}\z",RegexOptions.CultureInvariant)]
    private static partial Regex AllowedHash();
}
