using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class RuntimeResponsivenessTests
{
    [AvaloniaFact]
    public void Search_input_batches_typing_and_paste_uses_the_final_visible_order()
    {
        var store=new Store();var service=new WorkspaceService(store);var vm=new MainViewModel(service);
        var original=vm.GradeRows[0];original.Cells["t1"].Input="4,99";
        vm.SearchInput="C";vm.SearchInput="Ca";vm.SearchInput="Carlos";
        Assert.Equal(10,vm.GradeRows.Count);string[]? visible=null;
        vm.GradePasteRequested+=_=>visible=vm.GradeRows.Select(r=>r.Student.Id).ToArray();
        vm.PasteGradesCommand.Execute("t1");Assert.Equal(new[]{"s2"},visible);
        vm.SearchInput="";Assert.Equal(10,vm.GradeRows.Count);Assert.Same(original,vm.GradeRows[0]);Assert.Equal("4,99",original.Cells["t1"].Input);
        Assert.Equal(0,store.Saves);
    }

    [AvaloniaFact]
    public async Task Selecting_a_cut_asynchronously_updates_both_tables_without_persisting()
    {
        var store=new Store();var service=new WorkspaceService(store);var before=System.Text.Json.JsonSerializer.Serialize(service.Current);var vm=new MainViewModel(service);
        vm.Search="Ana";await vm.SelectCutAsync(2);
        Assert.Equal(2,vm.SelectedCut);Assert.Single(vm.GradeRows);Assert.Single(vm.AttendanceRows);Assert.False(vm.FileBusy);
        Assert.Equal(before,System.Text.Json.JsonSerializer.Serialize(service.Current));Assert.Equal(0,store.Saves);
        await vm.SelectCutAsync(99);Assert.Equal(2,vm.SelectedCut);Assert.NotEmpty(vm.Message);
    }

    [AvaloniaFact]
    public async Task Startup_is_visible_while_loading_and_does_not_reopen_after_the_user_closes_it()
    {
        var startup=new StartupWindow();startup.Show();var gate=new TaskCompletionSource<Window>();Window? active=null;
        var loading=startup.LoadAsync(()=>gate.Task,w=>active=w,_=>new Window());
        Assert.True(startup.IsVisible);Assert.Null(active);Assert.False(loading.IsCompleted);
        startup.Close();var next=new Window();gate.SetResult(next);await loading;Assert.Null(active);Assert.False(next.IsVisible);
        var second=new StartupWindow();second.Show();var recovered=new Window();
        await second.LoadAsync(()=>Task.FromException<Window>(new IOException("Fallo ficticio")),w=>active=w,_=>recovered);
        Assert.Same(recovered,active);Assert.True(recovered.IsVisible);Assert.False(second.IsVisible);recovered.Close();
    }

    [AvaloniaFact]
    public async Task Dashboard_reuses_unchanged_revisions_and_force_refresh_reads_again()
    {
        var store=new CatalogStore();var vm=new DashboardViewModel(new CatalogService(store),DemoWorkspaceFactory.Create,loadImmediately:false);
        await vm.RefreshAsync();Assert.Equal(2,store.Opens);Assert.Equal(2,vm.Courses.Count);
        await vm.RefreshAsync();Assert.Equal(2,store.Opens);
        store.Revision=1;await vm.RefreshAsync();Assert.Equal(3,store.Opens);
        await vm.RefreshAsync(force:true);Assert.Equal(5,store.Opens);
    }

    [AvaloniaFact]
    public async Task Constructing_the_shell_does_not_load_hidden_course_pages()
    {
        var store=new CatalogStore();var vm=new MainViewModel(new WorkspaceService(new Store()),new CatalogService(store));
        Assert.Equal(0,store.Opens);Assert.Empty(vm.Home.Semesters);Assert.Empty(vm.Dashboard.Courses);
        var window=new MainWindow{DataContext=vm};window.Show();
        try{await vm.Dashboard.RefreshAsync();Assert.Equal(2,vm.Dashboard.Courses.Count);Assert.Empty(vm.Home.Semesters);}
        finally{window.Close();}
    }

    [AvaloniaFact]
    public async Task Obsolete_dashboard_load_stops_before_opening_more_courses()
    {
        using var gate=new ManualResetEventSlim();using var entered=new ManualResetEventSlim();
        var store=new CatalogStore(gate,entered);var vm=new DashboardViewModel(new CatalogService(store),DemoWorkspaceFactory.Create,loadImmediately:false);
        var first=vm.RefreshAsync();Assert.True(await Task.Run(()=>entered.Wait(TimeSpan.FromSeconds(5))));
        try{await vm.RefreshAsync();Assert.Equal(2,vm.Courses.Count);}
        finally{gate.Set();await first;}
        Assert.Equal(3,store.Opens);Assert.False(vm.IsLoading);Assert.Empty(vm.Error);
    }

    private sealed class Store:IWorkspaceStore
    {
        public int Saves;
        public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();
        public void Save(AcademicWorkspace workspace)=>Saves++;
    }
    private sealed class CatalogStore(ManualResetEventSlim? gate=null,ManualResetEventSlim? entered=null):ICatalogStore
    {
        private int opens;
        public int Opens=>opens;
        public long Revision;
        public IReadOnlyList<AcademicSemester> Semesters()=>[new("term",2026,1)];
        public IReadOnlyList<CourseRecord> Courses(string id)=>[new("a","term","A","Materia ficticia A","Escuela",""){Revision=Revision},new("b","term","B","Materia ficticia B","Escuela",""){Revision=0}];
        public IWorkspaceStore OpenCourse(string id)
        {
            if(Interlocked.Increment(ref opens)==1 && gate is not null){entered!.Set();gate.Wait();}
            return new Store();
        }
        public void AddSemester(AcademicSemester s)=>throw new NotSupportedException();
        public void AddCourse(CourseRecord c,AcademicWorkspace w)=>throw new NotSupportedException();
        public void UpdateCourse(CourseRecord a,CourseRecord b)=>throw new NotSupportedException();
        public void DeleteCourse(string id)=>throw new NotSupportedException();
        public SemesterDeletionPreview InspectSemesterDeletion(string id)=>throw new NotSupportedException();
        public void DeleteSemester(SemesterDeletionPreview s)=>throw new NotSupportedException();
    }
}
