using System.Security.Cryptography;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class AutomaticBackupStorageTests
{
    [Fact]
    public async Task Sixth_copy_rotates_only_owned_automatic_archives_and_can_restore_on_another_computer()
    {
        using var files=new Files();var catalog=new CatalogService(new SqliteCatalogStore(files.Database));
        var semester=catalog.CreateSemester(2026,1);var course=catalog.CreateCourse(semester.Id,"FIC","Materia ficticia","Escuela","01");
        var workspace=catalog.Open(course.Id);await workspace.AddStudentAsync("Persona ficticia","123","prueba@example.test","3000000000");
        var backup=new LocalBackupService(files.Database,()=>false);
        var manual=backup.CreateInFolder(files.Root).Path;var original=File.ReadAllBytes(manual);
        var unknown=Path.Combine(files.Root,"signa-auto-desconocido.aulabk");File.Copy(manual,unknown);
        var first=new AutomaticBackupWriter(backup).Create(files.Root).Path;
        string last="";for(var i=0;i<5;i++)last=new AutomaticBackupWriter(backup).Create(files.Root).Path;
        Assert.False(File.Exists(first));Assert.Equal(6,Directory.GetFiles(files.Root,"signa-auto-*.aulabk").Length);
        Assert.Equal(original,File.ReadAllBytes(manual));Assert.Equal(original,File.ReadAllBytes(unknown));
        var destination=Path.Combine(files.Root,"otro-equipo.db");new LocalBackupService(destination,()=>false).Restore(last);
        var restored=new CatalogService(new SqliteCatalogStore(destination));var student=restored.Open(course.Id).Current.Students.Single();
        Assert.Equal("prueba@example.test",student.Email);Assert.Equal("3000000000",student.Phone);
    }

    [Fact]
    public void Failed_snapshot_or_corrupt_index_does_not_prune_and_modified_files_are_protected()
    {
        using var files=new Files();new CatalogService(new SqliteCatalogStore(files.Database));var backup=new LocalBackupService(files.Database,()=>false);
        var writer=new AutomaticBackupWriter(backup);var first=writer.Create(files.Root).Path;
        for(var i=0;i<4;i++)writer.Create(files.Root);
        var paths=Directory.GetFiles(files.Root,"*.aulabk");
        Assert.Throws<AcademicValidationException>(()=>new AutomaticBackupWriter(new LocalBackupService(Path.Combine(files.Root,"missing.db"))).Create(files.Root));
        Assert.All(paths,path=>Assert.True(File.Exists(path)));
        File.AppendAllText(first,"cambio externo");var bytes=File.ReadAllBytes(first);
        Assert.Throws<AcademicValidationException>(()=>writer.Create(files.Root));Assert.Equal(bytes,File.ReadAllBytes(first));
        File.WriteAllText(Path.Combine(files.Root,".signa-automaticos.json"),"{invalid");
        Assert.Throws<AcademicValidationException>(()=>writer.Create(files.Root));Assert.Equal(bytes,File.ReadAllBytes(first));
    }

    [Fact]
    public async Task All_production_stores_notify_only_successful_writes_not_reads_or_rollbacks()
    {
        using var files=new Files();var observer=new Observer();var store=new SqliteCatalogStore(files.Database,observer);
        var catalog=new CatalogService(store);Assert.Equal(0,observer.Count);
        var semester=catalog.CreateSemester(2026,1);Assert.Equal(1,observer.Count);
        var course=catalog.CreateCourse(semester.Id,"FIC","Materia ficticia","Escuela","01");Assert.Equal(2,observer.Count);
        var workspace=catalog.Open(course.Id);await workspace.AddStudentAsync("Persona ficticia");Assert.Equal(3,observer.Count);
        _=store.Semesters();_=store.Courses(semester.Id);_=store.InspectSemesterDeletion(semester.Id);_=workspace.Current;
        Assert.Equal(3,observer.Count);
        Assert.Throws<AcademicValidationException>(()=>store.AddSemester(semester));Assert.Equal(3,observer.Count);
        new SqliteInterfacePreferencesStore(files.Database,observer).Save(new(TeacherName:"Docente ficticio"));Assert.Equal(4,observer.Count);
        new SqliteBackupPreferencesStore(files.Database,observer).SaveFolder(files.Root);Assert.Equal(5,observer.Count);
        new SqliteBackupPreferencesStore(files.Database,observer).LoadFolder();Assert.Equal(5,observer.Count);
        store.DeleteCourse(course.Id);Assert.Equal(6,observer.Count);
        store.DeleteSemester(store.InspectSemesterDeletion(semester.Id));Assert.Equal(7,observer.Count);
    }

    [Fact]
    public void Interrupted_reservation_is_recovered_but_unregistered_and_unsafe_paths_are_never_deleted()
    {
        using var files=new Files();new SqliteCatalogStore(files.Database);var backup=new LocalBackupService(files.Database,()=>false);
        var name=$"signa-auto-20261023-1200000000000-{Guid.NewGuid():N}.aulabk";
        var reserved=backup.Create(Path.Combine(files.Root,name)).Path;
        var index=Path.Combine(files.Root,".signa-automaticos.json");
        File.WriteAllText(index,System.Text.Json.JsonSerializer.Serialize(new[]{new{Name=name,Sha256=(string?)null}}));
        var writer=new AutomaticBackupWriter(backup);
        for(var i=0;i<5;i++)writer.Create(files.Root);
        Assert.False(File.Exists(reserved));Assert.Equal(5,Directory.GetFiles(files.Root,"*.aulabk").Length);
        var protectedBytes=File.ReadAllBytes(Directory.GetFiles(files.Root,"*.aulabk")[0]);
        File.WriteAllText(index,"[{\"Name\":\"../manual.aulabk\",\"Sha256\":null}]");
        Assert.Throws<AcademicValidationException>(()=>writer.Create(files.Root));
        Assert.Equal(5,Directory.GetFiles(files.Root,"*.aulabk").Length);
        Assert.Contains(Directory.GetFiles(files.Root,"*.aulabk"),path=>File.ReadAllBytes(path).SequenceEqual(protectedBytes));
    }

    [Fact]
    public void Concurrent_automatic_writer_does_not_prune_but_manual_backup_still_works()
    {
        using var files=new Files();new SqliteCatalogStore(files.Database);var backup=new LocalBackupService(files.Database,()=>false);
        using var locked=new FileStream(Path.Combine(files.Root,".signa-automaticos.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        Assert.Throws<IOException>(()=>new AutomaticBackupWriter(backup).Create(files.Root));
        var manual=backup.CreateInFolder(files.Root);Assert.True(File.Exists(manual.Path));Assert.Equal(0,backup.Inspect(manual.Path).CourseCount);
    }

    private sealed class Observer:ISavedChangesObserver{public int Count;public void Saved()=>Count++;}
    private sealed class Files:IDisposable
    {
        public string Root{get;}=Path.Combine(Path.GetTempPath(),"signa-auto-046-"+Guid.NewGuid().ToString("N"));
        public string Database=>Path.Combine(Root,"operativa.db");
        public Files()=>Directory.CreateDirectory(Root);
        public void Dispose()=>Directory.Delete(Root,true);
    }
}
