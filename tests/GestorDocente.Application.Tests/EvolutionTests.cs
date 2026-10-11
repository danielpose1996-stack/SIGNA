using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Application.Tests;

public class EvolutionTests
{
    [Fact]
    public void Agreed_twenty_twenty_sixty_example_produces_three_point_nine()
    {
        var service=Service();var groups=WorkspaceService.Clone(service.Cut(1).Groups);
        groups.RemoveAll(g=>g.Name=="Proyecto");groups[0].Weight=20;groups[1].Weight=20;groups[2].Weight=60;
        service.ConfigureGroups(1,groups);
        foreach(var (id,value) in new[]{("t1",4m),("t2",3m),("t3",5m),("q1",3m),("q2",4m),("p",4m)})service.SetGrade(1,"s1",id,value);
        Assert.Equal(3.9m,GradeCalculator.Cut(service.Cut(1),"s1"));
    }
    [Fact]
    public void Removing_and_restoring_subactivity_updates_simple_average()
    {
        var service = Service();
        service.SetGrade(1,"s1","t1",4m); service.SetGrade(1,"s1","t2",3m); service.SetGrade(1,"s1","t3",5m);
        Assert.Equal(4m, GradeCalculator.Group(service.Cut(1),service.Cut(1).Groups[0],"s1"));
        service.RemoveActivity(1,"t2");
        Assert.Equal(30m,service.Cut(1).Groups[0].Weight);
        Assert.Equal(4.5m, GradeCalculator.Group(service.Cut(1),service.Cut(1).Groups[0],"s1"));
        service.RestoreActivity(1,"t2");
        Assert.Equal(3m,service.Cut(1).Grades.Single(x=>x.StudentId=="s1" && x.ActivityId=="t2").Value);
        Assert.Equal(4m, GradeCalculator.Group(service.Cut(1),service.Cut(1).Groups[0],"s1"));
    }
    [Fact]
    public void Empty_group_is_pending_and_other_cuts_are_untouched()
    {
        var service=Service();
        foreach(var id in new[]{"t1","t2","t3"})service.RemoveActivity(1,id);
        Assert.Null(GradeCalculator.Cut(service.Cut(1),"s1"));
        Assert.Equal(3,service.Cut(2).Groups[0].Activities.Count);
        Assert.Equal(3,service.Cut(1).RemovedActivities.Count);
    }
    [Fact]
    public void Removed_week_excludes_states_and_new_week_does_not_reuse_them()
    {
        var service=Service();
        service.SetAttendance(1,"s1",1,AttendanceState.Justified);
        service.RemoveWeek(1,1);
        service.AddWeek(1,WeekKind.Activities);
        var week=service.Cut(1).Weeks.Last();
        Assert.Equal(WeekKind.Activities,week.Kind);
        Assert.NotEqual(1,week.Number);
        Assert.DoesNotContain(service.Cut(1).Attendance,x=>x.WeekNumber==week.Number);
        service.RestoreWeek(1,1);
        Assert.Equal(AttendanceState.Justified,service.Cut(1).Attendance.Single(x=>x.StudentId=="s1" && x.WeekNumber==1).State);
    }
    [Fact]
    public void Restoring_a_week_respects_total_limit_for_both_types()
    {
        var service=Service(); service.RemoveWeek(1,1);
        while(service.Cut(1).Weeks.Count<9)service.AddWeek(1,WeekKind.Activities);
        Assert.Throws<AcademicValidationException>(()=>service.RestoreWeek(1,1));
        Assert.Single(service.Cut(1).RemovedWeeks);
    }
    [Fact]
    public void Failed_removal_is_atomic()
    {
        var store=new MemoryStore(Workspace()); var service=new WorkspaceService(store);store.Fail=true;
        Assert.Throws<IOException>(()=>service.RemoveActivity(1,"t1"));
        Assert.Equal(3,service.Cut(1).Groups[0].Activities.Count);Assert.Empty(service.Cut(1).RemovedActivities);
    }
    [Fact]
    public void Configuring_removed_group_preserves_grades_for_recovery()
    {
        var service=Service();var groups=WorkspaceService.Clone(service.Cut(1).Groups);
        var before=service.Cut(1).Grades.Single(x=>x.StudentId=="s1" && x.ActivityId=="t1").Value;
        groups.RemoveAt(0);groups[0].Weight+=30;service.ConfigureGroups(1,groups);
        Assert.Single(service.Cut(1).RemovedGroups);
        Assert.Equal(before,service.Cut(1).Grades.Single(x=>x.StudentId=="s1" && x.ActivityId=="t1").Value);
    }
    private static AcademicWorkspace Workspace()
    {
        var workspace=DemoWorkspaceFactory.Create();workspace.IsDemo=false;
        return workspace;
    }
    private static WorkspaceService Service()=>new(new MemoryStore(Workspace()));
    private sealed class MemoryStore(AcademicWorkspace data):IWorkspaceStore
    {
        public bool Fail{get;set;}
        public AcademicWorkspace? Load()=>WorkspaceService.Clone(data);
        public void Save(AcademicWorkspace value){if(Fail)throw new IOException("Fallo simulado");data=WorkspaceService.Clone(value);}
    }
}

