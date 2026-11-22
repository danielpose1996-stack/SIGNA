using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class SemesterStudentSearchTests
{
    [AvaloniaFact]
    public async Task Importing_a_student_updates_the_catalog_search_through_the_existing_events()
    {
        var path=Path.Combine(Path.GetTempPath(),$"signa-search-refresh-{Guid.NewGuid():N}.db");
        try
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));var term=catalog.CreateSemester(2026,2);
            catalog.CreateCourse(term.Id,"FICT-1","Materia ficticia","Escuela","");
            var model=new MainViewModel(new WorkspaceService(new EmptyStore()),catalog);await model.Home.RefreshAsync();
            model.Home.StudentSearch.SearchInput="Mariana";model.Home.StudentSearch.FlushSearch();Assert.Empty(model.Home.StudentSearch.Results);
            await model.OpenCourseAsync(model.Home.Courses.Single());
            await model.Service.AddStudentAsync("Mariana López Torres","F-01");
            await WaitUntil(()=>!model.Home.IsLoading);
            Assert.Equal("Mariana López Torres",Assert.Single(model.Home.StudentSearch.Results).FullName);
            Assert.Equal(1,model.Home.Courses.Single().StudentCount);
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }

    [AvaloniaFact]
    public async Task Live_search_reuses_the_loaded_period_and_switching_clears_old_results_immediately()
    {
        var store=new FixtureCatalog();var home=new HomeViewModel(new WorkspaceService(new EmptyStore()),new CatalogService(store),false);
        await home.RefreshAsync();var search=home.StudentSearch;
        search.SearchInput="mar";search.FlushSearch();
        Assert.Equal(2,Assert.Single(search.Results).Courses.Count);
        var match=search.Results.Single();var reads=store.Reads;
        search.SearchInput="MARIANA LOPEZ";search.FlushSearch();Assert.Same(match,Assert.Single(search.Results));
        search.SearchInput="sin coincidencias";search.FlushSearch();Assert.True(search.NoMatches);
        search.SearchInput="Mariana";search.FlushSearch();
        Assert.Equal(reads,store.Reads);Assert.Equal(0,store.Opens);Assert.Equal(0,store.Saves);
        home.SelectedSemester=home.Semesters.Single(s=>s.Id=="previous");
        Assert.Empty(search.Results);Assert.Equal("Período 2026-1",search.PeriodLabel);
        await WaitUntil(()=>!home.IsLoading);
        Assert.Equal("old",Assert.Single(Assert.Single(search.Results).Courses).Id);
        Assert.Equal("Mariana",search.SearchInput);
        search.ClearCommand.Execute(null);Assert.Empty(search.Results);Assert.False(search.ShowResults);
        home.SelectedSemester=home.Semesters.Single(s=>s.Id=="current");await WaitUntil(()=>!home.IsLoading);
    }

    [AvaloniaFact]
    public async Task Delayed_response_from_previous_semester_cannot_replace_current_results()
    {
        using var gate=new ManualResetEventSlim();using var entered=new ManualResetEventSlim();
        var store=new FixtureCatalog{Gate=gate,Entered=entered};var search=new SemesterStudentSearchViewModel(new CatalogService(store));
        search.SearchInput="Mariana";
        var oldLoad=search.LoadAsync(new("previous",2026,1));
        try
        {
            Assert.True(await Task.Run(()=>entered.Wait(TimeSpan.FromSeconds(5))));
            Assert.False(oldLoad.IsCompleted);Assert.True(search.IsLoading);
            await search.LoadAsync(new("current",2026,2));
            Assert.Equal(2,Assert.Single(search.Results).Courses.Count);
        }
        finally{gate.Set();}
        await oldLoad.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("Período 2026-2",search.PeriodLabel);
        Assert.Equal(new[]{"a","b"},Assert.Single(search.Results).Courses.Select(c=>c.Id));
    }

    [AvaloniaFact]
    public async Task Failed_load_has_a_retry_and_no_period_never_reads_storage()
    {
        var store=new FixtureCatalog{Fail=true};var search=new SemesterStudentSearchViewModel(new CatalogService(store));
        search.SearchInput="Mariana";search.FlushSearch();await search.LoadAsync(null);
        Assert.Equal(0,store.Reads);Assert.Empty(search.Results);Assert.False(search.HasSemester);
        await search.LoadAsync(new("current",2026,2));Assert.Contains("Reintentar",search.Error);Assert.Empty(search.Results);
        Assert.DoesNotContain("información privada ficticia",search.Error);
        store.Fail=false;search.RetryCommand.Execute(null);await WaitUntil(()=>!search.IsLoading);
        Assert.Empty(search.Error);Assert.Equal(2,Assert.Single(search.Results).Courses.Count);
        search.SearchInput="   ";search.FlushSearch();Assert.False(search.ShowResults);
    }

    [AvaloniaFact]
    public async Task Catalog_refresh_reloads_memberships_and_live_timer_needs_no_search_button()
    {
        var store=new FixtureCatalog();var home=new HomeViewModel(new WorkspaceService(new EmptyStore()),new CatalogService(store),false);
        await home.RefreshAsync();var search=home.StudentSearch;
        search.SearchInput="Mariana";await WaitUntil(()=>search.Results.Count==1);
        Assert.Equal(2,search.Results[0].Courses.Count);
        store.ExcludeSecondCourse=true;await home.RefreshAsync();
        Assert.Single(Assert.Single(search.Results).Courses);
    }

    [AvaloniaTheory]
    [InlineData(1600,1)] [InlineData(1100,1.25)]
    public async Task Search_panel_is_integrated_with_subject_cards_and_remains_readable_when_resized(double width,double scale)
    {
        var store=new FixtureCatalog();var model=new MainViewModel(new WorkspaceService(new EmptyStore()),new CatalogService(store));
        await model.Home.RefreshAsync();model.NavigateCommand.Execute("Materias");
        var window=new MainWindow{DataContext=model,Width=width,Height=950};
        if(scale!=1 && window.Content is Control root){window.Content=null;window.Content=new LayoutTransformControl{LayoutTransform=new ScaleTransform(scale,scale),Child=root};}
        window.Show();
        try
        {
            var searchView=Assert.Single(window.GetVisualDescendants().OfType<SemesterStudentSearchView>());
            var input=searchView.FindControl<TextBox>("SemesterStudentInput")!;
            var list=searchView.FindControl<ListBox>("SemesterStudentResults")!;
            window.UpdateLayout();Capture(window,$"01-buscador-vacio-{scale*100:0}.png");
            input.Text="Mariana";model.Home.StudentSearch.FlushSearch();window.UpdateLayout();
            Assert.Equal(2,Assert.Single(model.Home.StudentSearch.Results).Courses.Count);
            Assert.Single(list.Items);Assert.Equal(2,model.Home.Courses.Count);
            Assert.True(list.Bounds.Height<=340);Assert.Contains(list.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text=="MARIANA LÓPEZ TORRES");
            Capture(window,$"02-resultados-{scale*100:0}.png");
            window.Width=1024;window.Height=760;window.UpdateLayout();Capture(window,$"03-ventana-reducida-{scale*100:0}.png");
            var panel=searchView.FindControl<Border>("SemesterStudentSearchPanel")!;
            Assert.True(input.Bounds.Width>200);Assert.True(panel.Bounds.Width<=window.Bounds.Width);
            Assert.All(searchView.GetVisualDescendants().OfType<TextBlock>().Where(t=>t.IsEffectivelyVisible),t=>Assert.True(t.Bounds.Width<=panel.Bounds.Width+1));
        }
        finally{window.Close();}
    }

    [AvaloniaFact]
    public async Task Broad_search_keeps_a_virtualized_viewport_and_typing_does_not_reload_the_roster()
    {
        var store=new FixtureCatalog{SyntheticStudents=1000};var search=new SemesterStudentSearchViewModel(new CatalogService(store));
        await search.LoadAsync(new("current",2026,2));search.SearchInput="ALUMNO";search.FlushSearch();
        var window=new Window{Width=980,Height=600,Content=new SemesterStudentSearchView{DataContext=search}};window.Show();
        try
        {
            window.UpdateLayout();Assert.Equal(1000,search.Results.Count);
            var list=Assert.Single(window.GetVisualDescendants().OfType<ListBox>());
            Assert.InRange(list.GetVisualDescendants().OfType<ListBoxItem>().Count(),1,50);
            Assert.True(list.Bounds.Height<=340);
            for(var i=0;i<20;i++){search.SearchInput=i%2==0 ? "ficticio" : "alumno";search.FlushSearch();}
            Assert.Equal(1,store.Reads);Assert.Equal(0,store.Opens);Assert.Equal(0,store.Saves);
        }
        finally{window.Close();}
    }

    private static async Task WaitUntil(Func<bool> ready)
    {
        using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(8));
        while(!ready())await Task.Delay(10,timeout.Token);
    }
    private static void Capture(Window window,string file)
    {
        if(Environment.GetEnvironmentVariable("SIGNA_057_PREVIEW") is not {} folder)return;
        Directory.CreateDirectory(folder);using var image=window.CaptureRenderedFrame();image!.Save(Path.Combine(folder,file));
    }
    private sealed class EmptyStore:IWorkspaceStore
    {
        public AcademicWorkspace? Load()=>new(){IsDemo=false,CourseName="",Cuts=AcademicRules.DefaultCuts()};
        public void Save(AcademicWorkspace workspace)=>throw new InvalidOperationException("La búsqueda no debe guardar.");
    }
    private sealed class FixtureCatalog:ICatalogStore
    {
        public int Reads,Opens,Saves,SyntheticStudents;
        public bool Fail,ExcludeSecondCourse;
        public ManualResetEventSlim? Gate,Entered;
        public IReadOnlyList<AcademicSemester> Semesters()=>[new("current",2026,2),new("previous",2026,1)];
        public IReadOnlyList<CourseRecord> Courses(string semesterId)=>semesterId=="current"
            ? [new("a",semesterId,"G-1","GERENCIA DE PROYECTOS INFORMÁTICOS","Escuela","NOCTURNO",1),new("b",semesterId,"T-1","TRABAJO DE GRADO","Escuela","DIURNO",1)]
            : [new("old",semesterId,"OLD","MATERIA DEL SEMESTRE ANTERIOR","Escuela","")];
        public IReadOnlyList<CourseStudentEnrollment> StudentEnrollments(string semesterId)
        {
            Interlocked.Increment(ref Reads);
            if(semesterId=="previous" && Gate is not null){Entered!.Set();Gate.Wait();}
            if(Fail)throw new IOException("información privada ficticia");
            if(SyntheticStudents>0)return Courses(semesterId).SelectMany(c=>Enumerable.Range(1,SyntheticStudents)
                .Select(i=>new CourseStudentEnrollment(semesterId,c.Id,c.Name,c.Code,c.Group,$"{c.Id}-{i}",$"ALUMNO FICTICIO {i:0000}",$"F-{i}"))).ToArray();
            return Courses(semesterId).Where(c=>!ExcludeSecondCourse || c.Id!="b")
                .Select(c=>new CourseStudentEnrollment(semesterId,c.Id,c.Name,c.Code,c.Group,c.Id+"-student","MARIANA LÓPEZ TORRES","F-01")).ToArray();
        }
        public IWorkspaceStore OpenCourse(string id){Opens++;throw new InvalidOperationException("La consulta no debe abrir materias.");}
        public void AddSemester(AcademicSemester semester){Saves++;throw new NotSupportedException();}
        public void AddCourse(CourseRecord course,AcademicWorkspace workspace){Saves++;throw new NotSupportedException();}
        public void UpdateCourse(CourseRecord original,CourseRecord updated){Saves++;throw new NotSupportedException();}
        public void DeleteCourse(string id){Saves++;throw new NotSupportedException();}
        public SemesterDeletionPreview InspectSemesterDeletion(string id)=>throw new NotSupportedException();
        public void DeleteSemester(SemesterDeletionPreview preview){Saves++;throw new NotSupportedException();}
    }
}
