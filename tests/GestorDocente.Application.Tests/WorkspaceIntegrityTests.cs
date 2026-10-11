using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Application.Tests;

public class WorkspaceIntegrityTests
{
    [Theory]
    [InlineData("duplicate-grade")]
    [InlineData("duplicate-week")]
    [InlineData("ten-weeks")]
    [InlineData("invalid-attendance")]
    [InlineData("null-students")]
    [InlineData("orphan-grade")]
    [InlineData("duplicate-document")]
    public void Invalid_loaded_workspace_is_rejected_without_writing(string fault)
    {
        var data=DemoWorkspaceFactory.Create();var cut=data.Cuts[0];
        switch(fault)
        {
            case "duplicate-grade": cut.Grades.Add(cut.Grades[0]);break;
            case "duplicate-week": cut.Weeks.Add(cut.Weeks[0]);break;
            case "ten-weeks": cut.Weeks=Enumerable.Range(1,10).Select(n=>new AttendanceWeek(n)).ToList();break;
            case "invalid-attendance": cut.Attendance[0]=cut.Attendance[0] with {State=(AttendanceState)99};break;
            case "null-students": data.Students=null!;break;
            case "orphan-grade": cut.Grades.Add(new("s1","no-existe",4));break;
            case "duplicate-document": data.Students.Add(new("new","DEMO-001","Estudiante ficticio"));break;
        }
        var store=new MemoryStore(data);
        Assert.Throws<AcademicValidationException>(()=>new WorkspaceService(store));
        Assert.Equal(0,store.Saves);
    }

    [Fact]
    public void Archived_grade_and_attendance_remain_valid_and_restorable()
    {
        var store=new MemoryStore(DemoWorkspaceFactory.Create());var service=new WorkspaceService(store);
        service.RemoveActivity(1,"t1");service.RemoveWeek(1,1);
        var reopened=new WorkspaceService(store);
        reopened.RestoreActivity(1,"t1");reopened.RestoreWeek(1,1);
        Assert.Contains(reopened.Cut(1).Grades,x=>x.ActivityId=="t1");
        Assert.Contains(reopened.Cut(1).Attendance,x=>x.WeekNumber==1);
    }

    [Fact]
    public void Invalid_legacy_structure_is_rejected_before_migration_write()
    {
        var data=DemoWorkspaceFactory.Create();data.SchemaVersion=1;data.Cuts[0].Groups=null!;
        var store=new MemoryStore(data);
        Assert.Throws<AcademicValidationException>(()=>new WorkspaceService(store));
        Assert.Equal(0,store.Saves);
    }

    [Fact]
    public void Invalid_new_snapshot_does_not_replace_current_or_store()
    {
        var store=new MemoryStore(DemoWorkspaceFactory.Create());var service=new WorkspaceService(store);
        var before=service.Current;var invalid=WorkspaceService.Clone(service.Cut(1).Groups);
        invalid[0].Activities[0].Id="q1";
        Assert.Throws<AcademicValidationException>(()=>service.ConfigureGroups(1,invalid));
        Assert.Same(before,service.Current);
        Assert.Equal("t1",store.Load()!.Cuts[0].Groups[0].Activities[0].Id);
    }

    private sealed class MemoryStore(AcademicWorkspace data):IWorkspaceStore
    {
        private AcademicWorkspace current=data;
        public int Saves {get;private set;}
        public AcademicWorkspace? Load()=>current;
        public void Save(AcademicWorkspace workspace){current=WorkspaceService.Clone(workspace);Saves++;}
    }
}
