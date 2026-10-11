using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.ViewModels;
using Xunit;

namespace GestorDocente.Application.Tests;

public class WorkspaceTests
{
    [Fact]
    public void Three_cuts_retain_independent_values()
    {
        var service = new WorkspaceService(new MemoryStore());
        service.SetGrade(1,"s1","t1",1m); service.SetGrade(2,"s1","t1",4m);
        var vm = new MainViewModel(service);
        vm.SelectedCut = 1; Assert.Equal(1m,service.Cut(vm.SelectedCut).Grades.Single(x=>x.StudentId=="s1" && x.ActivityId=="t1").Value);
        vm.SelectedCut = 2; Assert.Equal(4m,service.Cut(vm.SelectedCut).Grades.Single(x=>x.StudentId=="s1" && x.ActivityId=="t1").Value);
        vm.SelectedCut = 3; Assert.Equal(3,vm.SelectedCut);
        Assert.Throws<AcademicValidationException>(() => vm.SelectedCut = 4);
    }

    [Fact]
    public void Editing_rubric_affects_one_activity_and_preserves_existing_grades()
    {
        var service = new WorkspaceService(new MemoryStore());
        var before = service.Cut(1).Grades.Single(x=>x.StudentId=="s1" && x.ActivityId=="t1").Value;
        var activity = WorkspaceService.Clone(service.Activity(1,"t1"));
        activity.Rubric!.Criteria[0].Description="Descripción corregida";
        service.SaveActivity(1,activity);
        Assert.Equal("Descripción corregida", service.Activity(1,"t1").Rubric!.Criteria[0].Description);
        Assert.Null(service.Activity(1,"t2").Rubric);
        Assert.Equal(before,service.Cut(1).Grades.Single(x=>x.StudentId=="s1" && x.ActivityId=="t1").Value);
    }

    [Fact]
    public void Failed_store_does_not_confirm_a_change_in_memory()
    {
        var store = new MemoryStore(); var service = new WorkspaceService(store);
        var before = service.Cut(1).Grades.Single(x=>x.StudentId=="s1" && x.ActivityId=="t1").Value;
        store.Fail=true;
        Assert.Throws<IOException>(()=>service.SetGrade(1,"s1","t1",0m));
        Assert.Equal(before,service.Cut(1).Grades.Single(x=>x.StudentId=="s1" && x.ActivityId=="t1").Value);
    }

    [Fact]
    public void Weeks_and_attendance_are_independent_by_cut()
    {
        var service = new WorkspaceService(new MemoryStore());
        while(service.Cut(1).Weeks.Count<9) service.AddWeek(1);
        Assert.Throws<AcademicValidationException>(()=>service.AddWeek(1));
        Assert.Equal(3,service.Cut(2).Weeks.Count);
        service.SetAttendance(1,"s1",1,AttendanceState.Justified);
        Assert.Equal(AttendanceState.Present,service.Cut(2).Attendance.Single(x=>x.StudentId=="s1" && x.WeekNumber==1).State);
    }

    [Fact]
    public void Selecting_student_updates_detail_and_search_keeps_data()
    {
        var vm = new MainViewModel(new WorkspaceService(new MemoryStore()));
        vm.SelectedStudent = vm.AttendanceRows[1];
        Assert.Equal(vm.AttendanceRows[1].Document,vm.SelectedStudent.Document);
        Assert.Equal(3,vm.SelectedStudent.WeeklyDetails.Count);
        vm.Search="no existe"; Assert.Empty(vm.AttendanceRows);
        vm.Search=""; Assert.Equal(10,vm.AttendanceRows.Count);
    }

    [Fact]
    public void Canceling_draft_does_not_modify_rubric()
    {
        var service = new WorkspaceService(new MemoryStore());
        var draft = new RubricEditorViewModel(service,1,"t1");
        draft.Criteria[0].Name="Cambio sin guardar";
        Assert.NotEqual("Cambio sin guardar",service.Activity(1,"t1").Rubric!.Criteria[0].Name);
    }

    [Fact]
    public void Rubric_assessment_retains_snapshot_after_definition_edit()
    {
        var service=new WorkspaceService(new MemoryStore());
        var rubric=service.Activity(1,"t1").Rubric!;
        service.Evaluate(1,"s1","t1",rubric.Criteria.ToDictionary(x=>x.Id,_=>4m));
        var draft=WorkspaceService.Clone(service.Activity(1,"t1"));
        draft.Rubric!.Criteria[0].Description="Nueva versión"; service.SaveActivity(1,draft);
        var grade=service.Cut(1).Grades.Single(x=>x.StudentId=="s1" && x.ActivityId=="t1");
        Assert.Equal(4m,grade.Value);
        Assert.NotEqual("Nueva versión",grade.Assessment!.Rubric.Criteria[0].Description);
    }

    private sealed class MemoryStore : IWorkspaceStore
    {
        public bool Fail {get;set;}
        private AcademicWorkspace? workspace;
        public AcademicWorkspace? Load()=>workspace is null ? null : WorkspaceService.Clone(workspace);
        public void Save(AcademicWorkspace value) {if(Fail) throw new IOException("Falló el almacenamiento");workspace=WorkspaceService.Clone(value);}
    }
}
