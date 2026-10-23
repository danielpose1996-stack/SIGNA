using System.Security.Cryptography;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class SimpleLocalBackupTests
{
    [Fact]
    public async Task New_backups_need_no_password_and_folder_survives_reopening_and_can_change()
    {
        using var files=new Files();var catalog=Seed(files.Database);
        var settings=new BackupPreferencesService(new SqliteBackupPreferencesStore(files.Database));
        await settings.SetFolderAsync(files.Root);
        Assert.Equal(files.Root,new BackupPreferencesService(new SqliteBackupPreferencesStore(files.Database)).Folder);
        var backup=new LocalBackupService(files.Database,()=>false);
        var first=backup.CreateInFolder(settings.Folder!);var second=backup.CreateInFolder(settings.Folder!);
        Assert.NotEqual(first.Path,second.Path);Assert.Equal(files.Root,Path.GetDirectoryName(first.Path));
        Assert.False(backup.RequiresPassword(first.Path));Assert.Equal(1,backup.Inspect(first.Path).CourseCount);
        var target=Path.Combine(files.Root,"restaurado.db");var restored=new LocalBackupService(target,()=>false).Restore(first.Path);
        Assert.Equal(1,restored.CourseCount);
        var original=catalog.Courses(catalog.Semesters.Single().Id).Single();
        var student=new CatalogService(new SqliteCatalogStore(target)).Open(original.Id).Current.Students.Single();
        Assert.Equal("ficticio@example.test",student.Email);Assert.Equal("3000000000",student.Phone);
        var other=Path.Combine(files.Root,"otra carpeta");Directory.CreateDirectory(other);await settings.SetFolderAsync(other);
        Assert.Equal(other,new SqliteBackupPreferencesStore(files.Database).LoadFolder());
        Directory.Delete(other);Assert.Throws<AcademicValidationException>(()=>backup.CreateInFolder(other));
    }

    [Theory]
    [InlineData("modified")][InlineData("truncated")][InlineData("length")]
    public void Invalid_new_archive_never_replaces_the_current_database(string damage)
    {
        using var files=new Files();Seed(files.Database);var backup=new LocalBackupService(files.Database,()=>false);
        var archive=backup.CreateInFolder(files.Root).Path;var bytes=File.ReadAllBytes(archive);
        if(damage=="modified")bytes[^100]^=0x55;
        if(damage=="truncated")bytes=bytes[..^20];
        if(damage=="length")bytes[8]^=0x33;
        File.WriteAllBytes(archive,bytes);var before=File.ReadAllBytes(files.Database);
        Assert.Throws<AcademicValidationException>(()=>backup.Restore(archive));Assert.Equal(before,File.ReadAllBytes(files.Database));
    }

    [Fact]
    public void Legacy_backup_still_requires_the_original_password_but_recovery_is_unencrypted()
    {
        using var files=new Files();Seed(files.Database);var archive=Path.Combine(files.Root,"antiguo.aulabk");
        const string password="Contraseña ficticia de respaldo antiguo";
        new EncryptedLocalBackupService(files.Database).Create(archive,password);
        var backup=new LocalBackupService(files.Database,()=>false);Assert.True(backup.RequiresPassword(archive));
        Assert.Throws<AcademicValidationException>(()=>backup.Inspect(archive));
        Assert.Throws<AcademicValidationException>(()=>backup.Restore(archive,"incorrecta"));
        var result=backup.Restore(archive,password);Assert.False(backup.RequiresPassword(result.PreviousBackupPath));
        Assert.Equal(1,backup.Inspect(result.PreviousBackupPath).CourseCount);
    }

    [Fact]
    public async Task Confirmed_reset_clears_all_operational_information_but_preserves_every_backup_and_update_file()
    {
        using var files=new Files();Seed(files.Database);
        await new InterfacePreferencesService(new SqliteInterfacePreferencesStore(files.Database)).SaveAsync(new(TeacherName:"Docente ficticio"));
        await new BackupPreferencesService(new SqliteBackupPreferencesStore(files.Database)).SetFolderAsync(files.Root);
        var backup=new LocalBackupService(files.Database,()=>false);var archive=backup.CreateInFolder(files.Root).Path;
        var recovery=Path.Combine(files.Root,"Recovery");Directory.CreateDirectory(recovery);
        var external=Path.Combine(files.Root,"carpeta externa");Directory.CreateDirectory(external);
        var names=new[]{archive,Path.Combine(recovery,"copia.aulabk"),Path.Combine(external,"otra.aulabk"),Path.Combine(files.Root,"backup-con-otra-extension.db"),Path.Combine(files.Root,"Update.exe")};
        foreach(var path in names.Skip(1))File.Copy(archive,path);
        var hashes=names.ToDictionary(p=>p,p=>SHA256.HashData(File.ReadAllBytes(p)));
        var reset=new SqliteLocalDataResetService(files.Database,()=>false);
        Assert.Throws<AcademicValidationException>(()=>reset.Reset(false));Assert.Single(new CatalogService(new SqliteCatalogStore(files.Database)).Semesters);
        Assert.Empty(reset.Reset(true).Warning);Assert.Empty(new CatalogService(new SqliteCatalogStore(files.Database)).Semesters);
        Assert.Equal(new InterfacePreferences(),new SqliteInterfacePreferencesStore(files.Database).Load());
        Assert.Null(new SqliteBackupPreferencesStore(files.Database).LoadFolder());
        foreach(var pair in hashes)Assert.Equal(pair.Value,SHA256.HashData(File.ReadAllBytes(pair.Key)));
        Assert.Equal(1,backup.Restore(archive).CourseCount);Assert.Single(new CatalogService(new SqliteCatalogStore(files.Database)).Semesters);
        Assert.Equal("Docente ficticio",new SqliteInterfacePreferencesStore(files.Database).Load().TeacherName);
    }

    [Fact]
    public void Reset_rejects_other_instances_and_rolls_back_on_sql_failure()
    {
        using var files=new Files();Seed(files.Database);
        Assert.Throws<AcademicValidationException>(()=>new SqliteLocalDataResetService(files.Database,()=>true).Reset(true));
        using(var db=new SqliteConnection($"Data Source={files.Database};Pooling=False"))
        {db.Open();using var cmd=db.CreateCommand();cmd.CommandText="CREATE TRIGGER impedir BEFORE DELETE ON courses BEGIN SELECT RAISE(ABORT,'Fallo ficticio'); END";cmd.ExecuteNonQuery();}
        Assert.Throws<SqliteException>(()=>new SqliteLocalDataResetService(files.Database,()=>false).Reset(true));
        var catalog=new CatalogService(new SqliteCatalogStore(files.Database));Assert.Single(catalog.Semesters);Assert.Single(catalog.Courses(catalog.Semesters[0].Id));
    }

    [Fact]
    public void Active_sqlite_writer_prevents_reset_and_its_pending_records_are_preserved()
    {
        using var files=new Files();Seed(files.Database);
        using(var writer=new SqliteConnection($"Data Source={files.Database};Pooling=False"))
        {
            writer.Open();using var transaction=writer.BeginTransaction();using var command=writer.CreateCommand();command.Transaction=transaction;
            command.CommandText="INSERT INTO semesters VALUES('pendiente',2028,1)";command.ExecuteNonQuery();
            Assert.Throws<AcademicValidationException>(()=>new SqliteLocalDataResetService(files.Database,()=>false).Reset(true));
            transaction.Commit();
        }
        Assert.Equal(2,new CatalogService(new SqliteCatalogStore(files.Database)).Semesters.Count);
    }

    [Fact]
    public void Backup_with_a_folder_from_another_operating_system_remains_portable()
    {
        using var files=new Files();Seed(files.Database);var settings=new SqliteBackupPreferencesStore(files.Database);settings.SaveFolder(files.Root);
        using(var db=new SqliteConnection($"Data Source={files.Database};Pooling=False"))
        {
            db.Open();using var command=db.CreateCommand();command.CommandText="UPDATE backup_preferences SET folder=$folder";
            command.Parameters.AddWithValue("$folder",OperatingSystem.IsWindows() ? "/home/docente/Respaldos" : @"C:\Docente\Respaldos");command.ExecuteNonQuery();
        }
        var backup=new LocalBackupService(files.Database,()=>false);var archive=backup.CreateInFolder(files.Root).Path;
        Assert.Equal(1,backup.Inspect(archive).CourseCount);
        var target=Path.Combine(files.Root,"otro-equipo.db");new LocalBackupService(target,()=>false).Restore(archive);
        Assert.Equal(settings.LoadFolder(),new SqliteBackupPreferencesStore(target).LoadFolder());
    }

    private static CatalogService Seed(string path)
    {
        var catalog=new CatalogService(new SqliteCatalogStore(path));var semester=catalog.CreateSemester(2026,1);
        var course=catalog.CreateCourse(semester.Id,"FIC","Asignatura ficticia","Escuela","01");
        catalog.Open(course.Id).AddStudentAsync("Estudiante ficticio","123","ficticio@example.test","3000000000").GetAwaiter().GetResult();return catalog;
    }
    private sealed class Files:IDisposable
    {
        public string Root{get;}=Path.Combine(Path.GetTempPath(),"signa-backups-045-"+Guid.NewGuid().ToString("N"));
        public string Database=>Path.Combine(Root,"operativa.db");
        public Files()=>Directory.CreateDirectory(Root);
        public void Dispose()=>Directory.Delete(Root,true);
    }
}
