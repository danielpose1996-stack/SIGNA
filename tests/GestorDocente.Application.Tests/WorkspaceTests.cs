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
        var service=new WorkspaceService(new MemoryStore());service.SetGrade(1,"s1","t1",1);service.SetGrade(2,"s1","t1",4);
        var vm=new MainViewModel(service);vm.SelectedCut=1;Assert.Equal(1m,service.Cut(1).Grades.Single(g=>g.StudentId=="s1" && g.ActivityId=="t1").Value);
        vm.SelectedCut=2;Assert.Equal(4m,service.Cut(2).Grades.Single(g=>g.StudentId=="s1" && g.ActivityId=="t1").Value);
        vm.SelectedCut=3;Assert.Throws<AcademicValidationException>(()=>vm.SelectedCut=4);
    }
    [Fact]
    public void Renaming_subactivity_preserves_existing_grade_and_other_subactivities()
    {
        var service=new WorkspaceService(new MemoryStore());var before=service.Cut(1).Grades.Single(g=>g.StudentId=="s1" && g.ActivityId=="t1").Value;
        var groups=WorkspaceService.Clone(service.Cut(1).Groups);groups[0].Activities[0].Name="Trabajo personalizado";service.ConfigureGroups(1,groups);
        Assert.Equal("Trabajo personalizado",service.Activity(1,"t1").Name);Assert.Equal("Taller 2",service.Activity(1,"t2").Name);
        Assert.Equal(before,service.Cut(1).Grades.Single(g=>g.StudentId=="s1" && g.ActivityId=="t1").Value);
    }
    [Fact]
    public void Failed_store_does_not_confirm_a_change_in_memory()
    {
        var store=new MemoryStore();var service=new WorkspaceService(store);var before=service.Cut(1).Grades.Single(g=>g.StudentId=="s1" && g.ActivityId=="t1").Value;
        store.Fail=true;Assert.Throws<IOException>(()=>service.SetGrade(1,"s1","t1",0));Assert.Equal(before,service.Cut(1).Grades.Single(g=>g.StudentId=="s1" && g.ActivityId=="t1").Value);
    }
    [Fact]
    public void Weeks_and_attendance_are_independent_by_cut()
    {
        var service=new WorkspaceService(new MemoryStore());while(service.Cut(1).Weeks.Count<9)service.AddWeek(1);
        Assert.Throws<AcademicValidationException>(()=>service.AddWeek(1));Assert.Equal(3,service.Cut(2).Weeks.Count);
        service.SetAttendance(1,"s1",1,AttendanceState.Justified);Assert.Equal(AttendanceState.Present,service.Cut(2).Attendance.Single(g=>g.StudentId=="s1" && g.WeekNumber==1).State);
    }
    [Fact]
    public void Selecting_student_updates_detail_and_search_keeps_data()
    {
        var vm=new MainViewModel(new WorkspaceService(new MemoryStore()));vm.SelectedStudent=vm.AttendanceRows[1];
        Assert.Equal(vm.AttendanceRows[1].Document,vm.SelectedStudent.Document);Assert.Equal(3,vm.SelectedStudent.WeeklyDetails.Count);
        vm.Search="no existe";Assert.Empty(vm.AttendanceRows);vm.Search="";Assert.Equal(10,vm.AttendanceRows.Count);
    }
    [Fact]
    public void Canceling_configuration_does_not_modify_saved_activity_or_mode()
    {
        var service=new WorkspaceService(new MemoryStore());var draft=new ConfigurationViewModel(service,1);
        draft.SelectedGroup=draft.Groups[0];draft.SelectedGroup.Name="Cambio sin guardar";draft.UsesSubactivities=false;
        Assert.Equal("Talleres",service.Cut(1).Groups[0].Name);Assert.Equal(EvaluationMode.SubActivities,service.Cut(1).Groups[0].Mode);Assert.Equal(3,service.Cut(1).Groups[0].Activities.Count);
    }
    private sealed class MemoryStore:IWorkspaceStore
    {
        public bool Fail;private AcademicWorkspace? data;
        public AcademicWorkspace? Load()=>data is null ? null : WorkspaceService.Clone(data);
        public void Save(AcademicWorkspace value){if(Fail)throw new IOException();data=WorkspaceService.Clone(value);}
    }
}
