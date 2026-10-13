using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Application.Tests;

public class WeekPresenceTests
{
    [Theory]
    [InlineData(WeekKind.Class)]
    [InlineData(WeekKind.Activities)]
    public async Task New_week_marks_entire_roster_in_one_save_and_one_notification(WeekKind kind)
    {
        var data=Fixture();data.Cuts[0].Weeks.Add(new(1));data.Cuts[0].Attendance.Add(new("s1",1,AttendanceState.Justified));
        data.Cuts[1].Weeks.Add(new(1));data.Cuts[1].Attendance.Add(new("s2",1,AttendanceState.Absent));
        var store=new Store(data);var service=new WorkspaceService(store);List<WorkspaceChange> changes=[];service.ChangedDetailed+=changes.Add;
        await service.AddWeekAsync(1,kind,new DateOnly(2026,10,13),markAllPresent:true);
        Assert.Equal(1,store.Saves);Assert.Equal(WorkspaceChangeKind.Structure,Assert.Single(changes).Kind);
        Assert.Equal(new AttendanceWeek(2,kind,new DateOnly(2026,10,13)),service.Cut(1).Weeks.Last());
        Assert.Equal(service.Current.Students.Select(s=>s.Id),service.Cut(1).Attendance.Where(a=>a.WeekNumber==2).Select(a=>a.StudentId));
        Assert.All(service.Cut(1).Attendance.Where(a=>a.WeekNumber==2),a=>Assert.Equal(AttendanceState.Present,a.State));
        Assert.Equal(AttendanceState.Justified,service.Cut(1).Attendance.First().State);
        Assert.Equal(AttendanceState.Absent,Assert.Single(service.Cut(2).Attendance).State);Assert.Empty(service.Cut(3).Weeks);
        await service.SetAttendanceAsync(1,"s1",2,AttendanceState.Absent);
        var report=AttendancePdfProjection.Build(service.Current,1,"s1");Assert.Equal(0m,report.Percentage);Assert.Equal("Bajo",report.Status);
        Assert.Equal(new AttendanceSummary(0,1,1,0),report.Summary);Assert.Equal("A - Ausente",report.Lines.Last().State);
    }

    [Fact]
    public async Task Unchecked_default_leaves_entries_pending_and_empty_roster_is_supported()
    {
        var service=new WorkspaceService(new Store(Fixture()));service.AddWeek(1);await service.AddWeekAsync(1);
        Assert.Equal(2,service.Cut(1).Weeks.Count);Assert.Empty(service.Cut(1).Attendance);
        var empty=Fixture();empty.Students.Clear();var store=new Store(empty);service=new(store);
        await service.AddWeekAsync(1,markAllPresent:true);Assert.Single(service.Cut(1).Weeks);Assert.Empty(service.Cut(1).Attendance);Assert.Equal(1,store.Saves);
    }

    [Fact]
    public void Synchronous_creation_does_not_reuse_removed_week_or_modify_its_records()
    {
        var data=Fixture();data.Cuts[0].RemovedWeeks.Add(new(4));data.Cuts[0].Attendance.Add(new("s1",4,AttendanceState.Late));
        var store=new Store(data);var service=new WorkspaceService(store);service.AddWeek(1,markAllPresent:true);
        Assert.Equal(5,Assert.Single(service.Cut(1).Weeks).Number);Assert.Equal(1,store.Saves);
        Assert.Equal(AttendanceState.Late,service.Cut(1).Attendance.Single(a=>a.WeekNumber==4).State);
        Assert.Equal(2,service.Cut(1).Attendance.Count(a=>a.WeekNumber==5));
    }

    [Fact]
    public async Task Failed_save_and_week_limit_publish_no_partial_week_or_presences()
    {
        var store=new Store(Fixture()){Fail=true};var service=new WorkspaceService(store);var before=service.Current;var changes=0;service.Changed+=()=>changes++;
        await Assert.ThrowsAsync<IOException>(()=>service.AddWeekAsync(1,markAllPresent:true));
        Assert.Same(before,service.Current);Assert.Empty(service.Cut(1).Weeks);Assert.Empty(service.Cut(1).Attendance);Assert.Equal(0,changes);Assert.False(service.IsSaving);
        store.Fail=false;
        for(var i=0;i<9;i++)await service.AddWeekAsync(1,markAllPresent:true);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.AddWeekAsync(1,markAllPresent:true));
        Assert.Equal(9,service.Cut(1).Weeks.Count);Assert.Equal(18,service.Cut(1).Attendance.Count);Assert.Equal(9,store.Saves);Assert.Equal(9,changes);
    }

    private static AcademicWorkspace Fixture()=>new(){Students=[new("s1","F01","Estudiante ficticio uno"),new("s2","","Estudiante ficticio dos")],Cuts=AcademicRules.DefaultCuts()};
    private sealed class Store(AcademicWorkspace data):IWorkspaceStore
    {
        public int Saves;public bool Fail;
        public AcademicWorkspace? Load()=>WorkspaceService.Clone(data);
        public void Save(AcademicWorkspace value){if(Fail)throw new IOException("Fallo simulado de escritura");data=WorkspaceService.Clone(value);Saves++;}
    }
}
