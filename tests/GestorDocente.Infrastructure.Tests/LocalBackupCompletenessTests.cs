using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class LocalBackupCompletenessTests
{
    private const string Password="Contraseña ficticia portable de prueba";

    [Fact]
    public async Task Whole_catalog_restores_identical_records_in_another_directory()
    {
        using var files=new Files();var source=files.PathFor("equipo-a", "datos.db");
        var catalog=new CatalogService(new SqliteCatalogStore(source));
        var first=catalog.CreateSemester(2026,1);var second=catalog.CreateSemester(2026,2);catalog.CreateSemester(2027,1);
        var course=catalog.CreateCourse(first.Id,"FIC-A","Materia ficticia A","Escuela ficticia","Noche");
        catalog.CreateCourse(first.Id,"FIC-B","Materia ficticia B","Escuela ficticia","Día");
        var other=catalog.CreateCourse(second.Id,"FIC-C","Materia ficticia C","Otra escuela","");
        var workspace=catalog.Open(course.Id);
        await workspace.AddStudentAsync("Estudiante ficticio","0000123","ficticio@example.test","+57 3000000000");
        await workspace.AddStudentAsync("Estudiante sin documento","","otro@example.test","");
        var student=workspace.Current.Students[0].Id;var another=workspace.Current.Students[1].Id;
        await workspace.AddCutAsync();
        await workspace.ConfigureCutWeightsAsync(new Dictionary<int,decimal>{{1,10},{2,20},{3,30},{4,40}});
        foreach(var cut in workspace.Current.Cuts.Select(c=>c.Number).ToArray())
        {
            await workspace.ConfigureGroupsAsync(cut,[new(){Id="direct",Name="Parcial personalizado",Weight=60},
                new(){Id="work",Name="Talleres",Weight=40,Mode=EvaluationMode.SubActivities,
                    Activities=[new(){Id="sub1",Name="Taller 1"},new(){Id="sub2",Name="Taller 2"},new(){Id="sub3",Name="Taller 3"}]}]);
            await workspace.SetGradeAsync(cut,student,"direct",4.65m);
            await workspace.SetGradeAsync(cut,student,"sub1",3.75m);
            await workspace.SetGradeAsync(cut,student,"sub2",2.25m);
            await workspace.AddWeekAsync(cut,WeekKind.Class,new DateOnly(2026,10,12));
            await workspace.AddWeekAsync(cut,WeekKind.Activities);await workspace.AddWeekAsync(cut);
            await workspace.SetAttendanceAsync(cut,student,1,AttendanceState.Present);
            await workspace.SetAttendanceAsync(cut,student,2,AttendanceState.Late);
            await workspace.SetAttendanceAsync(cut,another,1,AttendanceState.Absent);
            await workspace.SetAttendanceAsync(cut,another,2,AttendanceState.Justified);
        }
        await workspace.RemoveWeekAsync(4,3);
        // Un registro histórico «sin clase» también debe conservarse, aunque la UI nueva ya no lo crea.
        var historicalStore=new SqliteCatalogStore(source).OpenCourse(course.Id);var historical=historicalStore.Load()!;
        historical.Cuts[0].Attendance.Add(new(student,3,AttendanceState.NoClass));historicalStore.Save(historical);
        workspace=catalog.Open(course.Id);
        await catalog.Open(other.Id).AddStudentAsync("Estudiante en otro semestre");
        var before=ReadRecords(source);var archive=files.PathFor("copias", "respaldo.aulabk");
        var created=new EncryptedLocalBackupService(source).Create(archive,Password);
        Assert.Equal(3,created.SemesterCount);Assert.Equal(3,created.CourseCount);Assert.Equal(3,created.StudentCount);Assert.Equal(before,ReadRecords(source));

        var target=files.PathFor("equipo-b", "datos.db");
        var previous=new CatalogService(new SqliteCatalogStore(target));previous.CreateSemester(2025,2);
        var previousRecords=ReadRecords(target);
        var result=new EncryptedLocalBackupService(target).Restore(archive,Password);
        Assert.Equal(before,ReadRecords(target));Assert.False(result.PreviousCopyWasRaw);
        var restoredCatalog=new CatalogService(new SqliteCatalogStore(target));Assert.Equal(3,restoredCatalog.Semesters.Count);
        var restored=restoredCatalog.Open(course.Id).Current;
        Assert.Equal(JsonSerializer.Serialize(workspace.Current),JsonSerializer.Serialize(restored));
        Assert.Empty(restored.Students[1].Document);Assert.Equal("+57 3000000000",restored.Students[0].Phone);
        Assert.Equal(GradeCalculator.Final(workspace.Current,student),GradeCalculator.Final(restored,student));
        Assert.DoesNotContain(restored.Cuts.SelectMany(c=>c.Grades),g=>g.ActivityId=="sub3");
        Assert.Single(restored.Cuts[3].RemovedWeeks);
        var recovery=files.PathFor("recuperacion", "datos.db");
        new EncryptedLocalBackupService(recovery).Restore(result.PreviousBackupPath,Password);
        Assert.Equal(previousRecords,ReadRecords(recovery));
    }

    [Fact]
    public void Backup_verification_never_migrates_or_changes_legacy_payloads_or_revisions()
    {
        using var files=new Files();var source=files.PathFor("original", "datos.db");var catalog=Seed(source);
        var course=catalog.Courses(catalog.Semesters.Single().Id).Single();var data=catalog.Open(course.Id).Current;
        data.SchemaVersion=4;data.Cuts.RemoveAt(1);data.Cuts[1].Number=2;data.CutWeightsPending=true;
        using(var db=Open(source))
        {
            using var command=db.CreateCommand();command.CommandText="UPDATE course_workspaces SET schema_version=4,revision=42,payload=$payload";
            command.Parameters.AddWithValue("$payload",JsonSerializer.Serialize(data));command.ExecuteNonQuery();
        }
        var before=ReadRecords(source);var archive=files.PathFor("copias", "historico.aulabk");
        var backup=new EncryptedLocalBackupService(source);backup.Create(archive,Password);backup.Inspect(archive,Password);
        Assert.Equal(before,ReadRecords(source));
        var target=files.PathFor("otro-equipo", "datos.db");new EncryptedLocalBackupService(target).Restore(archive,Password);
        Assert.Equal(before,ReadRecords(target));
    }

    [Theory]
    [InlineData("DELETE FROM course_workspaces")]
    [InlineData("UPDATE courses SET name='Datos que no corresponden'")]
    [InlineData("DELETE FROM semesters")]
    public void Incomplete_catalog_is_rejected_without_publishing_a_backup(string damage)
    {
        using var files=new Files();var source=files.PathFor("original", "datos.db");Seed(source);
        using(var db=Open(source)){using var command=db.CreateCommand();command.CommandText=damage;command.ExecuteNonQuery();}
        var archive=files.PathFor("copias", "invalido.aulabk");var before=File.ReadAllBytes(source);
        Assert.Throws<AcademicValidationException>(()=>new EncryptedLocalBackupService(source).Create(archive,Password));
        Assert.False(File.Exists(archive));Assert.Equal(before,File.ReadAllBytes(source));
        Assert.Empty(Directory.GetFiles(System.IO.Path.GetDirectoryName(source)!,".aula-respaldo-*"));
    }

    [Fact]
    public void Open_wal_backup_includes_committed_changes_and_excludes_uncommitted_records()
    {
        using var files=new Files();var source=files.PathFor("original", "datos.db");Seed(source);
        using var writer=Open(source);using(var command=writer.CreateCommand())
        {
            command.CommandText="PRAGMA journal_mode=WAL; PRAGMA wal_autocheckpoint=0; INSERT INTO semesters VALUES('confirmado',2027,1)";command.ExecuteNonQuery();
        }
        Assert.True(File.Exists(source+"-wal"));var expected=ReadRecords(source);
        using(var transaction=writer.BeginTransaction())
        {
            using var pending=writer.CreateCommand();pending.Transaction=transaction;
            pending.CommandText="INSERT INTO semesters VALUES('pendiente',2028,1)";pending.ExecuteNonQuery();
            new EncryptedLocalBackupService(source).Create(files.PathFor("copias","wal.aulabk"),Password);
            transaction.Rollback();
        }
        var target=files.PathFor("otro-equipo","datos.db");
        new EncryptedLocalBackupService(target).Restore(files.PathFor("copias","wal.aulabk"),Password);
        Assert.Equal(expected,ReadRecords(target));Assert.Equal(2,new CatalogService(new SqliteCatalogStore(target)).Semesters.Count);
    }

    private static CatalogService Seed(string source)
    {
        var catalog=new CatalogService(new SqliteCatalogStore(source));var semester=catalog.CreateSemester(2026,1);
        catalog.CreateCourse(semester.Id,"FIC","Materia ficticia","Escuela ficticia","");return catalog;
    }
    private static SqliteConnection Open(string path)
    {
        var db=new SqliteConnection(new SqliteConnectionStringBuilder{DataSource=path,Pooling=false,ForeignKeys=false}.ToString());db.Open();return db;
    }
    private static string ReadRecords(string path)
    {
        using var db=Open(path);List<string?[]> rows=[];
        foreach(var sql in new[]{"SELECT id,year,term FROM semesters ORDER BY id","SELECT id,semester_id,code,name,school,group_name FROM courses ORDER BY id","SELECT course_id,schema_version,revision,payload FROM course_workspaces ORDER BY course_id"})
        {
            using var command=db.CreateCommand();command.CommandText=sql;using var reader=command.ExecuteReader();
            while(reader.Read())rows.Add(Enumerable.Range(0,reader.FieldCount).Select(i=>reader.GetValue(i).ToString()).ToArray());
            rows.Add(["end-table"]);
        }
        return JsonSerializer.Serialize(rows);
    }
    private sealed class Files:IDisposable
    {
        private readonly string root=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"aula-local-backup-tests",Guid.NewGuid().ToString("N"));
        public string PathFor(string folder,string file){var directory=System.IO.Path.Combine(root,folder);Directory.CreateDirectory(directory);return System.IO.Path.Combine(directory,file);}
        public void Dispose(){if(Directory.Exists(root))Directory.Delete(root,true);}
    }
}
