using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class NavigationTests
{
    [AvaloniaFact]
    public void Starts_at_home_without_a_course_header_or_sign_in()
    {
        var vm = CreateViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show(); window.UpdateLayout();
        Assert.True(vm.IsHome);
        Assert.False(vm.IsCourseContext);
        Assert.False(window.FindControl<Grid>("CourseHeader")!.IsVisible);
        Assert.Contains("active", window.FindControl<Button>("HomeNavigation")!.Classes);
        Assert.Single(vm.Home.Courses);
        Assert.True(vm.Home.Courses[0].IsDemo);
        window.Close();
    }

    [AvaloniaFact]
    public void Global_pages_hide_course_controls_and_highlight_only_their_menu_item()
    {
        var vm = CreateViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();
        foreach (var (route, name) in new[] { ("Materias", "CoursesNavigation"), ("Respaldos", "BackupsNavigation"), ("Inicio", "HomeNavigation") })
        {
            vm.NavigateCommand.Execute(route); window.UpdateLayout();
            Assert.False(vm.IsCourseContext);
            Assert.False(window.FindControl<Grid>("CourseHeader")!.IsVisible);
            Assert.False(window.FindControl<Border>("CourseTabs")!.IsVisible);
            var active = window.GetVisualDescendants().OfType<Button>()
                .Where(b => b.Classes.Contains("nav") && b.Classes.Contains("active")).ToList();
            Assert.Single(active);
            Assert.Equal(name, active[0].Name);
        }
        window.Close();
    }

    [AvaloniaFact]
    public void Returning_to_course_preserves_views_cut_search_selection_and_saved_data()
    {
        var vm = CreateViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show();
        vm.OpenCourseCommand.Execute(null);
        vm.SelectedCut = 2;
        vm.Page = "Asistencias";
        vm.Search = "Laura";
        var selected = vm.SelectedStudent;
        Assert.NotNull(selected);
        var notes = window.FindControl<NotesView>("RetainedNotes");
        var attendance = window.FindControl<AttendanceView>("RetainedAttendance");
        Assert.NotNull(notes);
        Assert.NotNull(attendance);
        var studentId = vm.Service.Current.Students[0].Id;
        var activityId = vm.Service.Cut(2).Groups[0].Activities[0].Id;
        vm.Service.SetGrade(2, studentId, activityId, 4.7m);
        selected = vm.SelectedStudent;
        foreach (var route in new[] { "Inicio", "Respaldos", "Materias" })
        {
            vm.NavigateCommand.Execute(route);
            vm.OpenCourseCommand.Execute(null);
            window.UpdateLayout();
            Assert.True(vm.IsAttendance);
            Assert.Equal(2, vm.SelectedCut);
            Assert.Equal("Laura", vm.Search);
            Assert.Same(selected, vm.SelectedStudent);
            Assert.Same(notes, window.FindControl<NotesView>("RetainedNotes"));
            Assert.Same(attendance, window.FindControl<AttendanceView>("RetainedAttendance"));
            Assert.Equal(4.7m, vm.Service.Cut(2).Grades.Single(g => g.StudentId == studentId && g.ActivityId == activityId).Value);
        }
        window.Close();
    }

    [AvaloniaFact]
    public void Course_card_and_backup_shortcut_have_working_bound_commands()
    {
        var vm = CreateViewModel();
        var window = new MainWindow { DataContext = vm };
        window.Show(); window.UpdateLayout();
        var open = Assert.Single(window.GetVisualDescendants().OfType<Button>(),
            b => Equals(b.Content, "Abrir materia"));
        Assert.NotNull(open.Command);
        Assert.True(open.Command.CanExecute(open.CommandParameter));
        open.Command.Execute(open.CommandParameter);
        window.UpdateLayout();
        Assert.True(vm.IsNotes);
        Assert.True(window.FindControl<Grid>("CourseHeader")!.IsVisible);
        vm.NavigateCommand.Execute("Inicio");
        var backups = window.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "OpenBackups");
        Assert.NotNull(backups.Command);
        backups.Command.Execute(backups.CommandParameter);
        Assert.True(vm.IsBackups);
        window.Close();
    }

    [AvaloniaFact]
    public void Empty_catalog_does_not_open_an_invented_course()
    {
        var workspace = DemoWorkspaceFactory.Create();
        workspace.CourseName = "";
        var vm = new MainViewModel(new WorkspaceService(new MemoryStore(workspace)));
        Assert.Empty(vm.Home.Courses);
        Assert.True(vm.Home.IsEmpty);
        Assert.False(vm.OpenCourseCommand.CanExecute(null));
        Assert.True(vm.IsHome);
    }

    [AvaloniaFact]
    public void Backup_page_is_informative_and_does_not_save_or_modify_workspace()
    {
        var store = new MemoryStore(DemoWorkspaceFactory.Create());
        var vm = new MainViewModel(new WorkspaceService(store));
        var before = System.Text.Json.JsonSerializer.Serialize(vm.Service.Current);
        vm.NavigateCommand.Execute("Respaldos");
        Assert.True(vm.IsBackups);
        Assert.Equal(0, store.Saves);
        Assert.Equal(before, System.Text.Json.JsonSerializer.Serialize(vm.Service.Current));
        vm.NavigateCommand.Execute("Inicio");
        Assert.True(vm.IsHome);
    }

    private static MainViewModel CreateViewModel() => new(new WorkspaceService(new MemoryStore(DemoWorkspaceFactory.Create())));
    private sealed class MemoryStore(AcademicWorkspace data) : IWorkspaceStore
    {
        public int Saves { get; private set; }
        public AcademicWorkspace? Load() => data;
        public void Save(AcademicWorkspace workspace) { data = workspace; Saves++; }
    }
}
