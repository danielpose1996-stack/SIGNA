using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using Xunit;
namespace GestorDocente.Acceptance.Tests;

public class AcademicLayoutTests
{
    [AvaloniaFact]
    public void Both_tables_prioritize_roster_at_small_window_and_detail_can_collapse()
    {
        var data=DemoWorkspaceFactory.Create();
        data.Students=Enumerable.Range(1,100).Select(i=>new Student($"s{i}",$"9000{i:000}",$"Estudiante de prueba {i:000}")).ToList();
        var vm=new MainViewModel(new WorkspaceService(new MemoryStore(data)));
        var window=new MainWindow{DataContext=vm,Width=1100,Height=700};window.Show();
        vm.OpenCourseCommand.Execute(null);window.UpdateLayout();
        var notes=window.FindControl<NotesView>("RetainedNotes")!;
        var notesTable=notes.FindControl<DataGrid>("GradesTable")!;
        Assert.Equal(100,vm.GradeRows.Count);Assert.True(notesTable.Bounds.Height>=330,$"Notas: {notesTable.Bounds.Height}");
        Assert.Equal(4,notesTable.FrozenColumnCount);Assert.True(double.IsNaN(notesTable.RowHeight));
        Assert.All(notesTable.GetVisualDescendants().OfType<DataGridRow>().Where(r=>r.Bounds.Height>0),r=>Assert.InRange(r.Bounds.Height,30,45));
        vm.Page="Asistencias";window.UpdateLayout();
        var attendance=window.FindControl<AttendanceView>("RetainedAttendance")!;
        var table=attendance.FindControl<DataGrid>("AttendanceTable")!;
        Assert.True(table.Bounds.Height>=350,$"Asistencias: {table.Bounds.Height}");
        var width=table.Bounds.Width;var selected=vm.SelectedStudent;
        vm.ToggleDetailCommand.Execute(null);window.UpdateLayout();Assert.True(table.Bounds.Width<width);Assert.Same(selected,vm.SelectedStudent);
        vm.ToggleDetailCommand.Execute(null);window.UpdateLayout();Assert.Equal(width,table.Bounds.Width);Assert.Same(selected,vm.SelectedStudent);
        window.Close();
    }
    [AvaloniaFact]
    public void Configuration_creates_direct_activity_then_optional_subactivities()
    {
        var service=new WorkspaceService(new MemoryStore(DemoWorkspaceFactory.Create()));
        var vm=new ConfigurationViewModel(service,1);vm.AddGroup();vm.SelectedGroup!.Weight=100;var fresh=vm.SelectedGroup;
        foreach(var group in vm.Groups.Where(g=>g!=fresh).ToList())vm.RemoveGroup(group);
        Assert.False(vm.UsesSubactivities);Assert.Empty(vm.Activities);Assert.True(vm.Save());
        service.SetGrade(1,"s1",fresh.Id,4);
        vm.UsesSubactivities=true;vm.AddActivity();Assert.Equal(2,vm.Activities.Count());Assert.True(vm.Save());
        Assert.Equal(EvaluationMode.SubActivities,service.Cut(1).Groups[0].Mode);
        Assert.Equal(4m,service.Cut(1).Grades.Single(g=>g.StudentId=="s1" && g.ActivityId==vm.Activities.First().Id).Value);
        var dialog=new ConfigurationWindow{DataContext=vm};dialog.Show();dialog.UpdateLayout();Assert.True(dialog.FindControl<DataGrid>("ActivitiesTable")!.IsVisible);dialog.Close();
    }
    [AvaloniaFact]
    public void Weeks_and_recovery_commands_work_even_at_nine_weeks()
    {
        var service=new WorkspaceService(new MemoryStore(DemoWorkspaceFactory.Create()));
        var vm=new MainViewModel(service);var weeks=new WeeksViewModel(service,1);
        weeks.SelectedKind=weeks.Kinds[1];weeks.Add();Assert.Equal(WeekKind.Activities,service.Cut(1).Weeks.Last().Kind);
        var removed=weeks.SelectedWeek!;weeks.Remove();var recovery=new RecoveryViewModel(service,1);Assert.Single(recovery.Items);
        recovery.Restore();Assert.Empty(service.Cut(1).RemovedWeeks);
        while(service.Cut(1).Weeks.Count<9)service.AddWeek(1);
        Assert.False(vm.AddWeekCommand.CanExecute(null));Assert.True(vm.ManageWeeksCommand.CanExecute(null));
    }
    [AvaloniaFact]
    public void Direct_and_subactivity_cells_edit_grades_and_refresh_totals_without_evaluation_dialog()
    {
        var service=new WorkspaceService(new MemoryStore(DemoWorkspaceFactory.Create()));
        service.ConfigureGroups(1,[new(){Id="direct",Name="Examen",Weight=50},new(){Id="work",Name="Parcial",Weight=50,Mode=EvaluationMode.SubActivities,Activities=[new(){Id="a",Name="Parte A"},new(){Id="b",Name="Parte B"}]}]);
        var vm=new MainViewModel(service);var window=new MainWindow{DataContext=vm};window.Show();vm.OpenCourseCommand.Execute(null);window.UpdateLayout();
        Assert.DoesNotContain(window.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text?.Contains("rúbrica",StringComparison.OrdinalIgnoreCase)==true);
        vm.GradeRows[0].Cells["direct"].Input="5,0";vm.GradeRows[0].Cells["direct"].Commit();
        vm.GradeRows[0].Cells["a"].Input="3,0";vm.GradeRows[0].Cells["a"].Commit();
        vm.GradeRows[0].Cells["b"].Input="4,0";vm.GradeRows[0].Cells["b"].Commit();
        Assert.Equal(4.25m,GradeCalculator.Cut(service.Cut(1),"s1"));Assert.Equal("4,25",vm.GradeRows[0].Total);
        var cell=vm.GradeRows[0].Cells["direct"];cell.Input="6";cell.Commit();Assert.NotEmpty(cell.Error);Assert.Equal(5m,service.Cut(1).Grades.Single(g=>g.StudentId=="s1" && g.ActivityId=="direct").Value);
        window.Close();
    }
    private sealed class MemoryStore(AcademicWorkspace data):IWorkspaceStore
    {
        public AcademicWorkspace? Load()=>data;
        public void Save(AcademicWorkspace value)=>data=WorkspaceService.Clone(value);
    }
}

