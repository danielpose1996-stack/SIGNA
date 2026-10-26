using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Application.Tests;

public class StudentListDeletionTests
{
    [Fact]
    public async Task Confirmed_list_deletion_is_one_save_and_preserves_every_other_workspace_field()
    {
        var source=DemoWorkspaceFactory.Create();
        source.Cuts[0].RemovedWeeks.Add(new(7,WeekKind.Activities,new(2026,10,26)));
        source.Cuts[0].Attendance.Add(new(source.Students[0].Id,7,AttendanceState.Present));
        source.Cuts[0].AttendanceGrading=new(true,5,0,4,2);
        var removed=new EvaluationGroup{Id="removed",Name="Actividad retirada",Weight=10};
        source.Cuts[0].RemovedGroups.Add(removed);source.Cuts[0].Grades.Add(new(source.Students[0].Id,removed.Id,4));
        var store=new Store(source);var service=new WorkspaceService(store);var before=JsonSerializer.Serialize(service.Current);
        var preview=service.PreviewStudentListDeletion();
        Assert.Equal(source.Students.Count,preview.StudentCount);
        Assert.Equal(source.Cuts.Sum(c=>c.Grades.Count),preview.GradeCount);
        Assert.Equal(source.Cuts.Sum(c=>c.Attendance.Count),preview.AttendanceCount);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.DeleteStudentListAsync(preview));
        Assert.Equal(0,store.Saves);Assert.Equal(before,JsonSerializer.Serialize(service.Current));
        var expected=WorkspaceService.Clone(service.Current);expected.Students.Clear();
        foreach(var cut in expected.Cuts){cut.Grades.Clear();cut.Attendance.Clear();}
        WorkspaceChange? change=null;service.ChangedDetailed+=value=>change=value;
        await service.DeleteStudentListAsync(preview,true);
        Assert.Equal(1,store.Saves);Assert.Equal(JsonSerializer.Serialize(expected),JsonSerializer.Serialize(service.Current));
        Assert.Equal(WorkspaceChangeKind.Students,change?.Kind);
        Assert.Throws<AcademicValidationException>(()=>service.PreviewStudentListDeletion());
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.DeleteStudentListAsync(preview,true));
        await service.AddStudentAsync("Nuevo estudiante ficticio");Assert.Single(service.Current.Students);
        Assert.All(service.Current.Cuts,cut=>{Assert.Empty(cut.Grades);Assert.Empty(cut.Attendance);});
    }

    [Fact]
    public async Task Stale_or_foreign_confirmation_and_write_failure_never_replace_the_snapshot()
    {
        var store=new Store(DemoWorkspaceFactory.Create());var service=new WorkspaceService(store);
        var preview=service.PreviewStudentListDeletion();
        await service.AddStudentAsync("Matrícula posterior ficticia");var afterSave=JsonSerializer.Serialize(service.Current);
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.DeleteStudentListAsync(preview,true));
        var foreign=new WorkspaceService(new Store(DemoWorkspaceFactory.Create()));
        await Assert.ThrowsAsync<AcademicValidationException>(()=>service.DeleteStudentListAsync(foreign.PreviewStudentListDeletion(),true));
        Assert.Equal(afterSave,JsonSerializer.Serialize(service.Current));
        store.Fail=true;var snapshot=service.Current;
        await Assert.ThrowsAsync<IOException>(()=>service.DeleteStudentListAsync(service.PreviewStudentListDeletion(),true));
        Assert.Same(snapshot,service.Current);Assert.False(service.IsSaving);Assert.Equal(afterSave,JsonSerializer.Serialize(service.Current));
    }

    [Fact]
    public async Task A_second_delete_is_rejected_while_the_first_save_is_in_progress()
    {
        using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        var store=new Store(DemoWorkspaceFactory.Create()){OnSave=()=>{entered.Set();Assert.True(release.Wait(TimeSpan.FromSeconds(5)));}};
        var service=new WorkspaceService(store);var preview=service.PreviewStudentListDeletion();
        var deleting=service.DeleteStudentListAsync(preview,true);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
            await Assert.ThrowsAsync<AcademicValidationException>(()=>service.DeleteStudentListAsync(preview,true));
            Assert.NotEmpty(service.Current.Students);
        }
        finally{release.Set();await deleting;}
        Assert.Empty(service.Current.Students);Assert.Equal(1,store.Saves);
    }

    private sealed class Store(AcademicWorkspace data):IWorkspaceStore
    {
        public int Saves;public bool Fail;public Action? OnSave;
        public AcademicWorkspace? Load()=>data;
        public void Save(AcademicWorkspace workspace){OnSave?.Invoke();if(Fail)throw new IOException("Fallo ficticio");Saves++;}
    }
}
