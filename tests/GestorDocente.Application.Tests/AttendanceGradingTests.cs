using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;
namespace GestorDocente.Application.Tests;

public class AttendanceGradingTests
{
    [Fact]
    public async Task Configuration_is_independent_per_cut_and_preserved_when_disabled()
    {
        var store=new Store();var service=new WorkspaceService(store);var grades=JsonSerializer.Serialize(service.Current.Cuts.Select(c=>c.Grades));
        await service.ConfigureAttendanceGradingAsync(1,new(true,5,0,4,1));
        Assert.True(service.Cut(1).AttendanceGrading!.Enabled);Assert.Null(service.Cut(2).AttendanceGrading);
        var first=service.Cut(1).AttendanceGrading!;
        await service.ConfigureAttendanceGradingAsync(1,first with{Enabled=false});Assert.Equal(first with{Enabled=false},service.Cut(1).AttendanceGrading);
        Assert.Null(new AcademicCutReadIndex(service.Cut(1)).AttendanceGrade("s1"));
        Assert.Equal(grades,JsonSerializer.Serialize(service.Current.Cuts.Select(c=>c.Grades)));Assert.Equal(2,store.Saves);
        var before=service.Current;store.Fail=true;
        await Assert.ThrowsAsync<IOException>(()=>service.ConfigureAttendanceGradingAsync(1,first));Assert.Same(before,service.Current);
    }
    [Fact]
    public void Legacy_upgrade_leaves_option_off_and_corrupt_policy_is_rejected()
    {
        var data=DemoWorkspaceFactory.Create();data.SchemaVersion=5;var before=JsonSerializer.Serialize(data.Cuts);
        var upgraded=WorkspaceMigration.Upgrade(data);Assert.Equal(before,JsonSerializer.Serialize(upgraded.Cuts));Assert.All(upgraded.Cuts,c=>Assert.Null(c.AttendanceGrading));
        upgraded.Cuts[0].AttendanceGrading=new(true,5,0,6,1);Assert.Throws<AcademicValidationException>(()=>WorkspaceIntegrity.Validate(upgraded));
    }
    [Fact]
    public async Task Removing_and_restoring_weeks_recomputes_grade_without_reassigning_records()
    {
        var service=new WorkspaceService(new Store());await service.ConfigureAttendanceGradingAsync(1,new(true,5,0,4,1));
        await service.SetAttendanceAsync(1,"s1",1,AttendanceState.Absent);await service.SetAttendanceAsync(1,"s1",2,AttendanceState.Justified);await service.SetAttendanceAsync(1,"s1",3,null);
        Assert.Equal(2m,new AcademicCutReadIndex(service.Cut(1)).AttendanceGrade("s1"));
        await service.RemoveWeekAsync(1,1);Assert.Equal(4m,new AcademicCutReadIndex(service.Cut(1)).AttendanceGrade("s1"));
        Assert.Equal(AttendanceState.Justified,service.Cut(1).Attendance.Single(a=>a.StudentId=="s1" && a.WeekNumber==2).State);
        await service.RestoreWeekAsync(1,1);Assert.Equal(2m,new AcademicCutReadIndex(service.Cut(1)).AttendanceGrade("s1"));
        await service.ConfigureAttendanceGradingAsync(1,new(true,5,0,2,1));Assert.Equal(1m,new AcademicCutReadIndex(service.Cut(1)).AttendanceGrade("s1"));
    }
    private sealed class Store:IWorkspaceStore
    {
        public int Saves;public bool Fail;public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();
        public void Save(AcademicWorkspace data){if(Fail)throw new IOException("Fallo ficticio");Saves++;}
    }
}
