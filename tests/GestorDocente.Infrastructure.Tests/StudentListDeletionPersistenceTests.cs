using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class StudentListDeletionPersistenceTests
{
    [Fact]
    public async Task Deleting_a_list_preserves_other_courses_preferences_configuration_and_portable_backups()
    {
        using var files=new Files();var writes=new Observer();var catalog=new CatalogService(new SqliteCatalogStore(files.Database,writes));
        var semester=catalog.CreateSemester(2026,1);
        var first=catalog.CreateCourse(semester.Id,"FIC-A","Asignatura ficticia A","Escuela","01");
        var second=catalog.CreateCourse(semester.Id,"FIC-B","Asignatura ficticia B","Escuela","02");
        var a=catalog.Open(first.Id);var b=catalog.Open(second.Id);
        await a.AddStudentAsync("Persona ficticia","123","ejemplo@example.test","3000000000");
        await b.AddStudentAsync("Persona ficticia","123","ejemplo@example.test","3000000000");
        foreach(var cut in a.Current.Cuts.ToArray())
        {
            await a.ConfigureGroupsAsync(cut.Number,[new(){Id="p",Name="Parcial",Weight=60},new(){Id="t",Name="Talleres",Weight=40,Mode=EvaluationMode.SubActivities,UseWeightedAverage=true,Activities=[new(){Id="t1",Name="Teoría",Weight=40},new(){Id="t2",Name="Práctica",Weight=60}]}]);
            await a.SetGradeAsync(cut.Number,a.Current.Students[0].Id,"p",4);
            await a.SetGradeAsync(cut.Number,a.Current.Students[0].Id,"t1",3);
            await a.AddWeekAsync(cut.Number,WeekKind.Activities,new(2026,10,26),markAllPresent:true);
            await a.RemoveWeekAsync(cut.Number,1);
            await a.ConfigureAttendanceGradingAsync(cut.Number,new(true,5,0,4,2));
        }
        var expected=WorkspaceService.Clone(a.Current);expected.Students.Clear();
        foreach(var cut in expected.Cuts){cut.Grades.Clear();cut.Attendance.Clear();}
        var settings=new SqliteInterfacePreferencesStore(files.Database);var profile=new InterfacePreferences(TeacherName:"Docente ficticio");settings.Save(profile);
        var destination=new SqliteBackupPreferencesStore(files.Database);destination.SaveFolder(files.Root);
        var otherBefore=ReadCourse(files.Database,second.Id);var writesBefore=writes.Count;var oldStudentId=a.Current.Students[0].Id;
        await a.DeleteStudentListAsync(a.PreviewStudentListDeletion(),true);
        Assert.Equal(writesBefore+1,writes.Count);
        Assert.Equal(otherBefore,ReadCourse(files.Database,second.Id));Assert.Equal(profile,settings.Load());Assert.Equal(files.Root,destination.LoadFolder());
        Assert.Equal(JsonSerializer.Serialize(expected),JsonSerializer.Serialize(catalog.Open(first.Id).Current));
        var automatic=new AutomaticBackupWriter(new LocalBackupService(files.Database,()=>false)).Create(files.Root);
        var target=Path.Combine(files.Root,"equipo-restaurado.db");new LocalBackupService(target,()=>false).Restore(automatic.Path);
        var restored=new CatalogService(new SqliteCatalogStore(target));
        Assert.Equal(JsonSerializer.Serialize(expected),JsonSerializer.Serialize(restored.Open(first.Id).Current));Assert.Single(restored.Open(second.Id).Current.Students);
        await a.AddStudentAsync("Nuevo listado ficticio","123");var reloaded=catalog.Open(first.Id);
        Assert.Single(reloaded.Current.Students);Assert.Equal("123",reloaded.Current.Students[0].Document);Assert.NotEqual(oldStudentId,reloaded.Current.Students[0].Id);
        Assert.All(reloaded.Current.Cuts,cut=>{Assert.Empty(cut.Grades);Assert.Empty(cut.Attendance);});
    }

    [Fact]
    public async Task Optimistic_conflict_rejects_a_confirmation_made_before_another_instance_saved()
    {
        using var files=new Files();var catalog=new CatalogService(new SqliteCatalogStore(files.Database));var semester=catalog.CreateSemester(2026,1);
        var course=catalog.CreateCourse(semester.Id,"FIC","Materia ficticia","Escuela","01");var current=catalog.Open(course.Id);
        await current.AddStudentAsync("Persona ficticia");var stale=catalog.Open(course.Id);var preview=stale.PreviewStudentListDeletion();
        await current.AddStudentAsync("Matrícula posterior ficticia");var before=ReadCourse(files.Database,course.Id);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>stale.DeleteStudentListAsync(preview,true));
        Assert.Equal(before,ReadCourse(files.Database,course.Id));Assert.Single(stale.Current.Students);Assert.Equal(2,catalog.Open(course.Id).Current.Students.Count);
    }

    [Fact]
    public async Task Failed_sql_transaction_keeps_the_complete_list_payload_revision_and_notifications()
    {
        using var files=new Files();var writes=new Observer();var catalog=new CatalogService(new SqliteCatalogStore(files.Database,writes));var semester=catalog.CreateSemester(2026,1);
        var course=catalog.CreateCourse(semester.Id,"FIC","Materia ficticia","Escuela","01");var workspace=catalog.Open(course.Id);
        await workspace.AddStudentAsync("Persona ficticia");var before=ReadCourse(files.Database,course.Id);var snapshot=workspace.Current;var calls=writes.Count;
        using(var db=new SqliteConnection($"Data Source={files.Database};Pooling=False"))
        {
            db.Open();using var command=db.CreateCommand();command.CommandText="CREATE TRIGGER reject_list_update BEFORE UPDATE ON course_workspaces BEGIN SELECT RAISE(ABORT,'Fallo ficticio'); END;";command.ExecuteNonQuery();
        }
        await Assert.ThrowsAsync<SqliteException>(()=>workspace.DeleteStudentListAsync(workspace.PreviewStudentListDeletion(),true));
        Assert.Same(snapshot,workspace.Current);Assert.False(workspace.IsSaving);Assert.Equal(before,ReadCourse(files.Database,course.Id));Assert.Equal(calls,writes.Count);
    }

    private static (string Payload,long Revision) ReadCourse(string path,string id)
    {
        using var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Pooling=false,Mode=SqliteOpenMode.ReadOnly}.ToString());db.Open();
        using var command=db.CreateCommand();command.CommandText="SELECT payload,revision FROM course_workspaces WHERE course_id=$id";command.Parameters.AddWithValue("$id",id);
        using var row=command.ExecuteReader();Assert.True(row.Read());return(row.GetString(0),row.GetInt64(1));
    }
    private sealed class Observer:ISavedChangesObserver{public int Count;public void Saved()=>Count++;}
    private sealed class Files:IDisposable
    {
        public string Root{get;}=Path.Combine(Path.GetTempPath(),"signa-lista-047-"+Guid.NewGuid().ToString("N"));
        public string Database=>Path.Combine(Root,"operativa.db");
        public Files()=>Directory.CreateDirectory(Root);
        public void Dispose()=>Directory.Delete(Root,true);
    }
}
