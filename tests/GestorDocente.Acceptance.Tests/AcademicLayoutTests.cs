using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using Xunit;
namespace GestorDocente.Acceptance.Tests;

public class AcademicLayoutTests
{
    [AvaloniaTheory]
    [InlineData("Notas","RetainedNotes","GradesTable","Filtrar tabla de notas","Agregar estudiante",1100,700,1)]
    [InlineData("Notas","RetainedNotes","GradesTable","Filtrar tabla de notas","Agregar estudiante",1580,960,1)]
    [InlineData("Notas","RetainedNotes","GradesTable","Filtrar tabla de notas","Agregar estudiante",1100,850,1.25)]
    [InlineData("Asistencias","RetainedAttendance","AttendanceTable","Filtrar tabla de asistencia","Gestionar semanas de asistencia",1100,700,1)]
    [InlineData("Asistencias","RetainedAttendance","AttendanceTable","Filtrar tabla de asistencia","Gestionar semanas de asistencia",1100,850,1.25)]
    [InlineData("Nota consolidada","RetainedConsolidated","ConsolidatedTable","Filtrar nota consolidada","Configurar porcentajes",1100,700,1)]
    [InlineData("Asistencia consolidada","RetainedAttendanceConsolidated","AttendanceConsolidatedTable","Filtrar asistencia consolidada",null,1100,700,1)]
    public void Course_options_make_room_for_the_table_and_keep_the_filter_during_navigation(
        string page,string viewName,string tableName,string searchName,string? actionName,double width,double height,double scale)
    {
        var data=DemoWorkspaceFactory.Create();
        data.Students=Enumerable.Range(1,40).Select(i=>new Student($"s{i}",$"9000{i:000}",$"Estudiante de prueba {i:000}")).ToList();
        var vm=new MainViewModel(new WorkspaceService(new MemoryStore(data)));
        var window=new MainWindow{DataContext=vm,Width=width,Height=height};
        if(scale!=1 && window.Content is Control root)
        {
            window.Content=null;
            window.Content=new LayoutTransformControl{LayoutTransform=new ScaleTransform(scale,scale),Child=root};
        }
        window.Show();
        try
        {
            vm.OpenCourseCommand.Execute(null);
            vm.Page=page;
            window.UpdateLayout();
            var view=window.FindControl<UserControl>(viewName)!;
            var table=view.FindControl<DataGrid>(tableName)!;
            var search=view.GetVisualDescendants().OfType<TextBox>().Single(box=>AutomationProperties.GetName(box)==searchName);
            var action=view.GetVisualDescendants().OfType<Button>().FirstOrDefault(button=>
                actionName is not null && (AutomationProperties.GetName(button)==actionName || Equals(button.Content,actionName)));
            var breadcrumb=window.FindControl<Border>("WorkspaceBreadcrumb")!;
            var trail=window.FindControl<StackPanel>("BreadcrumbTrail")!;
            var header=window.FindControl<Grid>("CourseHeader")!;
            var tabs=window.FindControl<Border>("CourseTabs")!;
            var cuts=window.FindControl<UserControl>("CourseCutSelector")!;
            var collapse=FindButton(window,"Contraer sección");
            var toggleBounds=ToggleLayoutBounds(collapse,window);
            var expandedHeight=table.Bounds.Height;
            var usesCuts=vm.ShowCutSelector;
            var selectedCut=vm.SelectedCut;

            Assert.True(breadcrumb.IsEffectivelyVisible);
            Assert.True(header.IsEffectivelyVisible);
            Assert.True(tabs.IsEffectivelyVisible);
            Assert.Equal(usesCuts,cuts.IsEffectivelyVisible);
            if(actionName is not null) Assert.True(action!.IsEffectivelyVisible);
            SaveCourseOptionsPreview(window,$"{tableName}-{width}x{height}-expandida.png");
            search.Text="Estudiante";

            Click(window,collapse);

            Assert.True(breadcrumb.IsEffectivelyVisible);
            Assert.False(trail.IsEffectivelyVisible);
            Assert.False(header.IsEffectivelyVisible);
            Assert.False(tabs.IsEffectivelyVisible);
            Assert.False(cuts.IsEffectivelyVisible);
            Assert.True(search.IsEffectivelyVisible);
            Assert.Equal("Estudiante",search.Text);
            Assert.True(table.Bounds.Height>=expandedHeight+90,$"{page}: {expandedHeight} → {table.Bounds.Height}");
            if(action is not null) Assert.False(action.IsEffectivelyVisible);
            if(page=="Notas") Assert.False(view.FindControl<Button>("NotesPdfButton")!.IsEffectivelyVisible);
            var expand=FindButton(window,"Expandir sección");
            Assert.Same(collapse,expand);
            Assert.Equal(toggleBounds,ToggleLayoutBounds(expand,window));
            Assert.DoesNotContain(view.GetVisualDescendants().OfType<Button>(),
                button=>button.IsEffectivelyVisible && AutomationProperties.GetName(button)=="Expandir sección");
            Assert.True(expand.Focusable);
            var bottomRight=expand.TranslatePoint(new Point(expand.Bounds.Width,expand.Bounds.Height),window)!.Value;
            Assert.InRange(bottomRight.X,1,window.ClientSize.Width);
            Assert.InRange(bottomRight.Y,1,window.ClientSize.Height);
            SaveCourseOptionsPreview(window,$"{tableName}-{width}x{height}-contraida.png");

            vm.NavigateCommand.Execute("Materias");
            window.UpdateLayout();
            Assert.True(breadcrumb.IsEffectivelyVisible);
            Assert.False(expand.IsEffectivelyVisible);
            vm.NavigateCommand.Execute("Académico");
            window.UpdateLayout();

            Assert.Equal(page,vm.Page);
            Assert.True(expand.IsEffectivelyVisible);
            Assert.Equal("Estudiante",search.Text);
            Click(window,expand);
            Assert.Equal("Contraer sección",AutomationProperties.GetName(expand));
            Assert.Equal(toggleBounds,ToggleLayoutBounds(collapse,window));
            Assert.True(collapse.IsEffectivelyVisible);
            Assert.True(trail.IsEffectivelyVisible);
            Assert.True(tabs.IsEffectivelyVisible);
            Assert.Equal(selectedCut,vm.SelectedCut);
            Assert.Equal(expandedHeight,table.Bounds.Height);
            if(action is not null) Assert.True(action.IsEffectivelyVisible);
        }
        finally{window.Close();}
    }

    private static Button FindButton(Control root,string label)=>root.GetVisualDescendants().OfType<Button>()
        .Single(button=>button.IsEffectivelyVisible && AutomationProperties.GetName(button)==label);

    // Fluent animates the pressed button. Compare its layout independently of that temporary transform.
    private static Rect ToggleLayoutBounds(Button button,Window window)=>new(
        button.GetVisualParent()!.TranslatePoint(button.Bounds.TopLeft,window)!.Value,button.Bounds.Size);

    private static void Click(Window window,Button button)
    {
        var position=button.TranslatePoint(new Point(button.Bounds.Width/2,button.Bounds.Height/2),window)!.Value;
        window.MouseDown(position,MouseButton.Left);
        window.MouseUp(position,MouseButton.Left);
        window.UpdateLayout();
    }

    private static void SaveCourseOptionsPreview(Window window,string fileName)
    {
        if(Environment.GetEnvironmentVariable("SIGNA_COURSE_OPTIONS_PREVIEW") is not { Length:>0 } folder)return;
        Directory.CreateDirectory(folder);
        window.MouseMove(new Point(5,5));
        foreach(var control in window.GetVisualDescendants().OfType<Control>().Where(ToolTip.GetIsOpen))
            ToolTip.SetIsOpen(control,false);
        window.UpdateLayout();
        using var image=window.CaptureRenderedFrame();
        image!.Save(Path.Combine(folder,fileName));
    }

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

