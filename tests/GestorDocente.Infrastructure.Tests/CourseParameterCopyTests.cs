using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class CourseParameterCopyTests
{
    [Fact]
    public async Task Copy_across_semesters_persists_all_cuts_with_new_identity_and_no_personal_records()
    {
        using var fixture=new Fixture();var catalog=fixture.Catalog;var source=fixture.ConfigureSource();
        var before=JsonSerializer.Serialize(catalog.Open(source.Id).Current);
        var parameters=await catalog.CopyParametersAsync(source.Id);
        var destination=catalog.CreateSemester(2027,2);
        var first=await catalog.CreateCourseAsync(destination.Id,"NEW-1","Materia nueva","Escuela B","B",parameters);
        var second=await catalog.CreateCourseAsync(destination.Id,"NEW-2","Otra materia","Escuela C","C",parameters);
        var reopened=new CatalogService(new SqliteCatalogStore(fixture.Path));
        var data=reopened.Open(first.Id).Current;
        Assert.NotEqual(source.Id,data.CourseId);Assert.Equal(first.Id,data.CourseId);Assert.False(data.IsDemo);
        Assert.Equal(destination.Id,data.SemesterId);Assert.Equal("2027-2",data.Period);
        Assert.Equal("NEW-1",data.CourseCode);Assert.Equal("Materia nueva",data.CourseName);
        Assert.Equal("Escuela B",data.School);Assert.Equal("B",data.GroupName);Assert.Empty(data.Students);
        Assert.Equal(4,data.Cuts.Count);Assert.Equal(4,reopened.Courses(destination.Id).First(c=>c.Id==first.Id).CutCount);
        Assert.All(data.Cuts,c=>{Assert.Empty(c.Grades);Assert.Empty(c.Attendance);Assert.Empty(c.Weeks);Assert.Empty(c.RemovedGroups);Assert.Empty(c.RemovedActivities);Assert.Empty(c.RemovedWeeks);});
        Assert.Equal(new[]{25m,25m,25m,25m},data.Cuts.Select(c=>c.Weight));
        Assert.True(data.Cuts[0].Groups[0].UseWeightedAverage);
        Assert.Equal(new decimal?[]{40m,60m},data.Cuts[0].Groups[0].Activities.Select(a=>a.Weight));
        Assert.Equal(new AttendanceGradePolicy(true,5,0,4,2),data.Cuts[0].AttendanceGrading);
        Assert.Null(data.Cuts[1].AttendanceGrading);
        var ws=reopened.Open(first.Id);var changed=ws.Current.Cuts[0].Groups;
        changed[0].Name="Cambio independiente";await ws.ConfigureGroupsAsync(1,changed);
        Assert.Equal("Parcial",reopened.Open(second.Id).Current.Cuts[0].Groups[0].Name);
        Assert.Equal(before,JsonSerializer.Serialize(reopened.Open(source.Id).Current));
        Assert.NotEqual(data.Cuts[0].Groups[0].Id,reopened.Open(second.Id).Current.Cuts[0].Groups[0].Id);
        fixture.AssertIntegrity();
    }

    [Fact]
    public async Task Copy_is_read_only_and_invalid_or_deleted_sources_are_rejected()
    {
        using var fixture=new Fixture();var source=fixture.ConfigureSource();
        fixture.Execute("CREATE TRIGGER no_copy_write BEFORE UPDATE ON course_workspaces BEGIN SELECT RAISE(ABORT, 'no writes allowed'); END");
        var parameters=await fixture.Catalog.CopyParametersAsync(source.Id);Assert.Equal(4,parameters.Cuts.Count);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>fixture.Catalog.CopyParametersAsync("missing"));
        await Assert.ThrowsAsync<AcademicValidationException>(()=>fixture.Catalog.CopyParametersAsync(""));
        Assert.Single(fixture.Catalog.Courses(source.SemesterId));
    }

    [Fact]
    public async Task Failure_to_insert_configured_workspace_rolls_back_course_and_leaves_source_intact()
    {
        using var fixture=new Fixture();var source=fixture.ConfigureSource();
        var before=JsonSerializer.Serialize(fixture.Catalog.Open(source.Id).Current);
        var parameters=await fixture.Catalog.CopyParametersAsync(source.Id);
        var term=fixture.Catalog.CreateSemester(2028,1);
        fixture.Execute("CREATE TRIGGER reject_workspace BEFORE INSERT ON course_workspaces BEGIN SELECT RAISE(ABORT, 'simulated write failure'); END");
        await Assert.ThrowsAsync<AcademicValidationException>(()=>fixture.Catalog.CreateCourseAsync(term.Id,"FAIL","Fallida","Escuela","",parameters));
        Assert.Empty(fixture.Catalog.Courses(term.Id));
        fixture.Execute("DROP TRIGGER reject_workspace");
        var success=await fixture.Catalog.CreateCourseAsync(term.Id,"FAIL","Correcta","Escuela","",parameters);
        Assert.Equal(4,fixture.Catalog.Open(success.Id).Current.Cuts.Count);
        Assert.Equal(before,JsonSerializer.Serialize(fixture.Catalog.Open(source.Id).Current));fixture.AssertIntegrity();
    }

    [Fact]
    public async Task Optional_copy_preserves_pending_weights_and_default_creation_keeps_initial_configuration()
    {
        using var fixture=new Fixture();var source=fixture.ConfigureSource();
        var store=fixture.Store.OpenCourse(source.Id);var data=store.Load()!;
        data.Cuts[0].Weight=0;data.CutWeightsPending=true;store.Save(data);
        var parameters=await fixture.Catalog.CopyParametersAsync(source.Id);
        var copied=await fixture.Catalog.CreateCourseAsync(source.SemesterId,"COPY","Copiada","Escuela","",parameters);
        var copiedData=fixture.Catalog.Open(copied.Id).Current;
        Assert.True(copiedData.CutWeightsPending);Assert.Equal(75m,copiedData.Cuts.Sum(c=>c.Weight));
        var plain=await fixture.Catalog.CreateCourseAsync(source.SemesterId,"PLAIN","Sin plantilla","Escuela","");
        var plainData=fixture.Catalog.Open(plain.Id).Current;
        Assert.Equal(new[]{30m,30m,40m},plainData.Cuts.Select(c=>c.Weight));Assert.All(plainData.Cuts,c=>Assert.Empty(c.Groups));
    }

    private sealed class Fixture:IDisposable
    {
        private readonly string directory=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"signa-copy-"+Guid.NewGuid().ToString("N"));
        public string Path=>System.IO.Path.Combine(directory,"catalog.db");
        public SqliteCatalogStore Store {get;}
        public CatalogService Catalog {get;}
        public Fixture(){Directory.CreateDirectory(directory);Store=new(Path);Catalog=new(Store);}
        public CourseRecord ConfigureSource()
        {
            var term=Catalog.CreateSemester(2026,1);var course=Catalog.CreateCourse(term.Id,"SOURCE","Origen ficticio","Escuela A","A");
            var store=Store.OpenCourse(course.Id);var data=store.Load()!;
            data.Students=[new("private-id","private-document","Estudiante ficticio","private@example.invalid","1234567")];
            data.Cuts=Enumerable.Range(1,4).Select(n=>new AcademicCut{Number=n,Weight=25}).ToList();
            data.Cuts[0].Groups=[new(){Id="original-group",Name="Parcial",Weight=100,Mode=EvaluationMode.SubActivities,UseWeightedAverage=true,
                Activities=[new(){Id="original-child",Name="Teoría",Weight=40},new(){Name="Práctica",Weight=60}]}];
            data.Cuts[0].AttendanceGrading=new(true,5,0,4,2);data.Cuts[1].AttendanceGrading=new(false,1,2,3,4);
            data.Cuts[0].Grades=[new("private-id","original-child",4.25m)];
            data.Cuts[0].Weeks=[new(1,WeekKind.Activities,new(2026,10,21))];data.Cuts[0].Attendance=[new("private-id",1,AttendanceState.Present)];
            store.Save(data);return course;
        }
        public void Execute(string sql){using var db=new SqliteConnection($"Data Source={Path};Pooling=False");db.Open();using var command=db.CreateCommand();command.CommandText=sql;command.ExecuteNonQuery();}
        public void AssertIntegrity()
        {
            using var db=new SqliteConnection($"Data Source={Path};Pooling=False");db.Open();using var command=db.CreateCommand();
            command.CommandText="PRAGMA integrity_check";Assert.Equal("ok",command.ExecuteScalar());
            command.CommandText="PRAGMA foreign_key_check";using var reader=command.ExecuteReader();Assert.False(reader.Read());
        }
        public void Dispose()=>Directory.Delete(directory,true);
    }
}
