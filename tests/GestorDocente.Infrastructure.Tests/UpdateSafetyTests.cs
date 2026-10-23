using System.Security.Cryptography;
using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class UpdateSafetyTests
{
    [Fact]
    public async Task Replacing_application_files_preserves_complete_catalog_preferences_and_unencrypted_backups()
    {
        using var fixture=new Fixture();var catalog=new CatalogService(new SqliteCatalogStore(fixture.Database));
        var term=catalog.CreateSemester(2026,1);var course=catalog.CreateCourse(term.Id,"F-01","Materia ficticia","Escuela","A");
        var workspace=catalog.Open(course.Id);await workspace.AddStudentAsync("Estudiante ficticio","F-1","ficticio@example.invalid","1234567");
        workspace.ConfigureGroups(1,[new(){Id="a",Name="Parcial",Weight=100}]);
        workspace.SetGrade(1,workspace.Current.Students[0].Id,"a",4.5m);workspace.AddWeek(1,WeekKind.Activities,new(2026,10,21),true);
        var preferences=new SqliteInterfacePreferencesStore(fixture.Database);var appearance=new InterfacePreferences(TeacherName:"Docente ficticio",Accent:"#123456");preferences.Save(appearance);
        var backup=Path.Combine(fixture.Directory,"backup.aulabk");new LocalBackupService(fixture.Database,()=>false).Create(backup);
        var backupHash=SHA256.HashData(File.ReadAllBytes(backup));var before=JsonSerializer.Serialize(workspace.Current);
        var databaseHash=SHA256.HashData(File.ReadAllBytes(fixture.Database));
        var guard=new SqliteUpdateSafetyCheck(fixture.Database,()=>fixture.Installation,()=>false);
        await guard.VerifyAsync();
        Assert.Equal(databaseHash,SHA256.HashData(File.ReadAllBytes(fixture.Database)));
        File.WriteAllText(Path.Combine(fixture.Installation,"SIGNA.exe"),"nueva versión ficticia");
        Assert.Equal(before,JsonSerializer.Serialize(new CatalogService(new SqliteCatalogStore(fixture.Database)).Open(course.Id).Current));
        Assert.Equal(appearance,new SqliteInterfacePreferencesStore(fixture.Database).Load());Assert.Equal(backupHash,SHA256.HashData(File.ReadAllBytes(backup)));
    }

    [Theory]
    [InlineData("database-inside")][InlineData("backup-inside")][InlineData("another-instance")][InlineData("corrupt")]
    public async Task Unsafe_locations_backups_other_instances_or_corrupt_data_prevent_update(string reason)
    {
        using var fixture=new Fixture();_ = new SqliteCatalogStore(fixture.Database);
        var path=fixture.Database;
        if(reason=="database-inside"){path=Path.Combine(fixture.Installation,"datos.db");File.Copy(fixture.Database,path);}
        if(reason=="backup-inside")File.WriteAllText(Path.Combine(fixture.Installation,"respaldo.aulabk"),"archivo ficticio");
        if(reason=="corrupt")File.WriteAllBytes(path,[1,2,3]);
        var before=File.ReadAllBytes(path);
        var guard=new SqliteUpdateSafetyCheck(path,()=>fixture.Installation,()=>reason=="another-instance");
        await Assert.ThrowsAsync<ApplicationUpdateException>(()=>guard.VerifyAsync());Assert.Equal(before,File.ReadAllBytes(path));
    }

    [Fact]
    public async Task Live_wal_data_is_validated_without_checkpointing_or_changing_academic_records()
    {
        using var fixture=new Fixture();var catalog=new CatalogService(new SqliteCatalogStore(fixture.Database));
        var term=catalog.CreateSemester(2026,2);var course=catalog.CreateCourse(term.Id,"WAL","Materia ficticia WAL","Escuela","A");
        using var connection=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=fixture.Database,Pooling=false}.ToString());connection.Open();
        using(var command=connection.CreateCommand()){command.CommandText="PRAGMA journal_mode=WAL; PRAGMA wal_autocheckpoint=0; INSERT INTO semesters VALUES('wal',2027,1)";command.ExecuteNonQuery();}
        var before=JsonSerializer.Serialize(catalog.Open(course.Id).Current);var main=ReadShared(fixture.Database);
        var wal=ReadShared(fixture.Database+"-wal");Assert.NotEmpty(wal);
        await new SqliteUpdateSafetyCheck(fixture.Database,()=>fixture.Installation,()=>false).VerifyAsync();
        Assert.Equal(main,ReadShared(fixture.Database));Assert.Equal(wal,ReadShared(fixture.Database+"-wal"));
        Assert.Equal(before,JsonSerializer.Serialize(new CatalogService(new SqliteCatalogStore(fixture.Database)).Open(course.Id).Current));
        Assert.Equal(2,catalog.Semesters.Count);
    }

    [Fact]
    public async Task Similar_path_prefixes_are_safe_and_cancellation_does_not_touch_files()
    {
        using var fixture=new Fixture();_ = new SqliteCatalogStore(fixture.Database);
        var guard=new SqliteUpdateSafetyCheck(fixture.Database,()=>fixture.Installation,()=>false);
        await guard.VerifyAsync();
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>guard.VerifyAsync(cancellation.Token));
    }

    private sealed class Fixture:IDisposable
    {
        public string Directory{get;}=Path.Combine(Path.GetTempPath(),"signa-update-"+Guid.NewGuid().ToString("N"));
        public string Installation=>Path.Combine(Directory,"SIGNA");
        public string Database=>Path.Combine(Directory,"SIGNA-datos","catalog.db");
        public Fixture(){System.IO.Directory.CreateDirectory(Installation);System.IO.Directory.CreateDirectory(Path.GetDirectoryName(Database)!);}
        public void Dispose()=>System.IO.Directory.Delete(Directory,true);
    }
    private static byte[] ReadShared(string path)
    {
        using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete);
        using var result=new MemoryStream();stream.CopyTo(result);return result.ToArray();
    }
}
