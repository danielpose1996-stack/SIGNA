using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Application.Tests;

public class WeekEditingTests
{
    [Fact]
    public async Task Visible_numbers_are_consecutive_without_reassigning_attendance_keys()
    {
        var store=new Store();var service=new WorkspaceService(store);
        var other=JsonSerializer.Serialize(service.Cut(2));
        await service.RemoveWeekAsync(1,1);
        Assert.Equal(new[]{1,2},service.Cut(1).DisplayWeeks.Select(w=>w.DisplayNumber));
        Assert.Equal(new[]{2,3},service.Cut(1).DisplayWeeks.Select(w=>w.Number));
        Assert.Equal(new[]{"A - Ausente","T - Tarde"},AttendancePdfProjection.Build(service.Current,1,"s1").Lines.Select(l=>l.State));
        Assert.Equal(new[]{1,2},AttendancePdfProjection.Build(service.Current,1,"s1").Lines.Select(l=>l.Week));
        await service.AddWeekAsync(1,WeekKind.Activities);
        Assert.Equal(3,service.Cut(1).DisplayWeeks.Last().DisplayNumber);
        Assert.DoesNotContain(service.Cut(1).Attendance,a=>a.WeekNumber==service.Cut(1).Weeks.Last().Number);
        await service.RestoreWeekAsync(1,1);
        Assert.Equal(new[]{1,2,3,4},service.Cut(1).DisplayWeeks.Select(w=>w.DisplayNumber));
        Assert.Equal(AttendanceState.Present,service.Cut(1).Attendance.Single(a=>a.WeekNumber==1).State);
        Assert.Equal(other,JsonSerializer.Serialize(service.Cut(2)));
        var reopened=new WorkspaceService(store);
        Assert.Equal(new[]{1,2,3,4},reopened.Cut(1).DisplayWeeks.Select(w=>w.DisplayNumber));
    }

    [Fact]
    public async Task Edit_changes_only_type_and_optional_date_in_one_commit_and_can_clear_date()
    {
        var store=new Store();var service=new WorkspaceService(store);var original=JsonSerializer.Serialize(service.Cut(1).Attendance);
        var changes=0;service.Changed+=()=>changes++;
        await service.EditWeekAsync(1,2,WeekKind.Activities,new DateOnly(2026,10,14));
        Assert.Equal(1,store.Saves);Assert.Equal(1,changes);
        var week=service.Cut(1).Weeks.Single(w=>w.Number==2);Assert.Equal(WeekKind.Activities,week.Kind);Assert.NotNull(week.Date);
        var line=AttendancePdfProjection.Build(service.Current,1,"s1").Lines[1];Assert.Equal("A - Ausente",line.State);Assert.Equal(week.Date,line.Date);
        await service.EditWeekAsync(1,2,WeekKind.Class,null);
        Assert.Null(service.Cut(1).Weeks.Single(w=>w.Number==2).Date);
        Assert.Equal(original,JsonSerializer.Serialize(service.Cut(1).Attendance));
        Assert.Null(new WorkspaceService(store).Cut(1).Weeks.Single(w=>w.Number==2).Date);
    }

    [Fact]
    public async Task Failed_edit_or_invalid_target_never_publishes_partial_data()
    {
        var store=new Store();var service=new WorkspaceService(store);var before=service.Current;store.Fail=true;
        await Assert.ThrowsAsync<IOException>(()=>service.EditWeekAsync(1,2,WeekKind.Activities,new DateOnly(2026,10,14)));
        Assert.Same(before,service.Current);Assert.False(service.IsSaving);
        store.Fail=false;
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.EditWeekAsync(1,2,(WeekKind)99,null));
        await service.RemoveWeekAsync(1,2);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.EditWeekAsync(1,2,WeekKind.Class,null));
        Assert.Single(service.Cut(1).RemovedWeeks);
    }

    [Fact]
    public async Task Removing_all_weeks_then_creating_one_starts_at_one_without_reusing_states()
    {
        var service=new WorkspaceService(new Store());
        foreach(var week in service.Cut(1).Weeks.ToArray())await service.RemoveWeekAsync(1,week.Number);
        await service.AddWeekAsync(1);
        var created=Assert.Single(service.Cut(1).DisplayWeeks);Assert.Equal(1,created.DisplayNumber);Assert.Equal(4,created.Number);
        Assert.DoesNotContain(service.Cut(1).Attendance,a=>a.WeekNumber==created.Number);
    }

    private sealed class Store:IWorkspaceStore
    {
        private AcademicWorkspace data=new(){Students=[new("s1","F01","Estudiante ficticio")],Cuts=AcademicRules.DefaultCuts()};
        public int Saves;public bool Fail;
        public Store(){data.Cuts[0].Weeks=[new(1),new(2),new(3)];data.Cuts[0].Attendance=[new("s1",1,AttendanceState.Present),new("s1",2,AttendanceState.Absent),new("s1",3,AttendanceState.Late)];}
        public AcademicWorkspace? Load()=>WorkspaceService.Clone(data);
        public void Save(AcademicWorkspace value){if(Fail)throw new IOException("Fallo ficticio");data=WorkspaceService.Clone(value);Saves++;}
    }
}
