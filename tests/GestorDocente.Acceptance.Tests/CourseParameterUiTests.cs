using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class CourseParameterUiTests
{
    [AvaloniaFact]
    public async Task Copy_survives_semester_change_and_paste_is_optional_until_creating_the_new_course()
    {
        var fixture=new Fixture();var vm=fixture.Model;var source=fixture.Source;
        await vm.CopyCourseParametersAsync(source);
        Assert.NotNull(vm.CopiedParameters);Assert.Contains(source.Name,vm.CopiedParametersSource);Assert.False(vm.FileBusy);
        await vm.Home.RefreshAsync();vm.Home.SelectedSemester=fixture.Destination;
        var editor=new CatalogEditorViewModel(fixture.Catalog,fixture.Destination,false,
            availableParameters:vm.CopiedParameters,parametersSource:vm.CopiedParametersSource)
            {Code="NEW",Name="Materia destino",School="Escuela B",Group="B"};
        Assert.False(editor.HasPastedParameters);Assert.True(editor.CanPasteParameters);
        editor.PasteParametersCommand.Execute(null);
        Assert.True(editor.HasPastedParameters);Assert.Equal(3,editor.ParameterPreview.Count);
        Assert.Contains("Talleres",editor.ParameterPreview[0].Activities);
        Assert.Equal("Materia destino",editor.Name);Assert.Equal("NEW",editor.Code);Assert.Equal("B",editor.Group);
        Assert.Empty(fixture.Catalog.Courses(fixture.Destination.Id));
        Assert.True(await editor.SaveAsync());
        var saved=fixture.Catalog.Open(editor.SavedCourseId).Current;
        Assert.Equal("Materia destino",saved.CourseName);Assert.Equal("Talleres",saved.Cuts[0].Groups[0].Name);Assert.Empty(saved.Students);
        Assert.Single(fixture.Catalog.Open(source.Id).Current.Students);
    }

    [AvaloniaFact]
    public async Task Paste_can_be_cleared_and_cancelled_and_is_unavailable_during_edit_or_without_copy()
    {
        var fixture=new Fixture();await fixture.Model.CopyCourseParametersAsync(fixture.Source);
        var parameters=fixture.Model.CopiedParameters;
        var editor=new CatalogEditorViewModel(fixture.Catalog,fixture.Destination,false,availableParameters:parameters);
        editor.PasteParametersCommand.Execute(null);Assert.True(editor.HasPastedParameters);
        editor.ClearParametersCommand.Execute(null);Assert.False(editor.HasPastedParameters);Assert.Empty(editor.ParameterPreview);
        editor.PasteParametersCommand.Execute(null);
        var dialog=new CatalogEditorWindow{DataContext=editor};dialog.Show();dialog.UpdateLayout();
        Assert.Contains(dialog.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text?.Contains("Talleres")==true);dialog.Close();
        Assert.Empty(fixture.Catalog.Courses(fixture.Destination.Id));
        var empty=new CatalogEditorViewModel(fixture.Catalog,fixture.Destination,false);
        Assert.False(empty.CanPasteParameters);empty.PasteParametersCommand.Execute(null);Assert.False(empty.HasPastedParameters);
        var editing=new CatalogEditorViewModel(fixture.Catalog,fixture.Destination,false,fixture.Catalog.Courses(fixture.SourceSemester.Id).Single(),parameters);
        Assert.False(editing.ShowParameters);Assert.False(editing.CanPasteParameters);
        editing.PasteParametersCommand.Execute(null);Assert.False(editing.HasPastedParameters);
        Assert.False(new CatalogEditorViewModel(fixture.Catalog,fixture.Destination,true,availableParameters:parameters).ShowParameters);
    }

    [AvaloniaFact]
    public async Task Failed_copy_preserves_previous_snapshot_and_commands_block_simultaneous_operations()
    {
        var fixture=new Fixture();var vm=fixture.Model;await vm.CopyCourseParametersAsync(fixture.Source);
        var parameters=vm.CopiedParameters;var label=vm.CopiedParametersSource;
        vm.FileBusy=true;Assert.False(vm.CopyCourseParametersCommand.CanExecute(fixture.Source));
        await vm.CopyCourseParametersAsync(fixture.Source);Assert.Same(parameters,vm.CopiedParameters);vm.FileBusy=false;
        await vm.CopyCourseParametersAsync(fixture.Source with {Id="missing"});
        Assert.Same(parameters,vm.CopiedParameters);Assert.Equal(label,vm.CopiedParametersSource);
        Assert.NotEmpty(vm.Message);Assert.False(vm.FileBusy);
    }

    [AvaloniaFact]
    public async Task Copy_card_buttons_and_paste_preview_fit_resized_windows()
    {
        var fixture=new Fixture();var vm=fixture.Model;
        for(var i=0;i<5;i++)fixture.Catalog.CreateCourse(fixture.SourceSemester.Id,$"F-{i}",$"Asignatura ficticia {i}","Escuela A","");
        await vm.Home.RefreshAsync();vm.Home.SelectedSemester=vm.Home.Semesters.Single(s=>s.Id==fixture.SourceSemester.Id);
        await vm.Home.RefreshAsync();
        foreach(var width in new[]{1600d,1080d,780d})
        {
            var window=new Window{Content=new HomeView{DataContext=vm},Width=width,Height=950};window.Show();window.UpdateLayout();
            try
            {
                var buttons=window.GetVisualDescendants().OfType<Button>().Where(b=>AutomationProperties.GetName(b)=="Copiar parámetros de la materia").ToArray();
                Assert.Equal(6,buttons.Length);
                Assert.All(buttons,b=>{Assert.True(b.Bounds.Width>100);Assert.True(b.TranslatePoint(new Point(b.Bounds.Width,0),window)!.Value.X<=width);});
                Capture(window,$"materias-{width:0}.png");
            }
            finally{window.Close();}
        }
        await vm.CopyCourseParametersAsync(fixture.Source);
        var editor=new CatalogEditorViewModel(fixture.Catalog,fixture.Destination,false,availableParameters:vm.CopiedParameters,parametersSource:vm.CopiedParametersSource);
        editor.PasteParametersCommand.Execute(null);
        var dialog=new CatalogEditorWindow{DataContext=editor};dialog.Show();dialog.UpdateLayout();
        try
        {
            Assert.Single(dialog.GetVisualDescendants().OfType<Button>(),b=>AutomationProperties.GetName(b)=="Pegar parámetros copiados");
            Capture(dialog,"crear-materia-parametros.png");
            var scroll=dialog.GetVisualDescendants().OfType<ScrollViewer>().First();
            scroll.Offset=new Vector(0,scroll.Extent.Height);dialog.UpdateLayout();
            Assert.True(scroll.Offset.Y>0);Capture(dialog,"resumen-parametros.png");
        }
        finally{dialog.Close();}
    }

    private static void Capture(Window window,string file)
    {
        if(Environment.GetEnvironmentVariable("SIGNA_COPY_PREVIEW_DIR") is not {Length:>0} directory)return;
        Directory.CreateDirectory(directory);window.CaptureRenderedFrame()?.Save(Path.Combine(directory,file));
    }
    private sealed class Fixture
    {
        public CatalogService Catalog{get;}
        public MainViewModel Model{get;}
        public AcademicSemester SourceSemester{get;}
        public AcademicSemester Destination{get;}
        public CourseSummary Source{get;}
        public Fixture()
        {
            Catalog=new(new PresentationCatalogStore());
            SourceSemester=Catalog.CreateSemester(2026,1);Destination=Catalog.CreateSemester(2027,1);
            var course=Catalog.CreateCourse(SourceSemester.Id,"ORI","Álgebra ficticia","Escuela A","A");var workspace=Catalog.Open(course.Id);
            workspace.ConfigureGroups(1,[new(){Name="Talleres",Weight=100}]);workspace.ImportStudents([new("F-1","Estudiante ficticio")]);
            Source=new(course.Name,course.Code,course.Group,SourceSemester.Name,1,false,course.Id,course.School);
            Model=new(workspace,Catalog);
        }
    }

    // Presentation tests use detached in-memory snapshots. SQLite transactions and persistence
    // have separate integration tests; asynchronous catalog notifications must not race a fixture's filesystem cleanup.
    private sealed class PresentationCatalogStore:ICatalogStore
    {
        private readonly object gate=new();
        private readonly List<AcademicSemester> semesters=[];
        private readonly Dictionary<string,(CourseRecord Course,AcademicWorkspace Data)> courses=[];
        public IReadOnlyList<AcademicSemester> Semesters(){lock(gate)return semesters.OrderByDescending(s=>s.Year).ThenByDescending(s=>s.Term).ToArray();}
        public IReadOnlyList<CourseRecord> Courses(string id)
        {
            lock(gate)return courses.Values.Where(v=>v.Course.SemesterId==id)
                .Select(v=>v.Course with {StudentCount=v.Data.Students.Count,CutCount=v.Data.Cuts.Count}).OrderBy(c=>c.Name).ToArray();
        }
        public void AddSemester(AcademicSemester semester){lock(gate)semesters.Add(semester);}
        public void AddCourse(CourseRecord course,AcademicWorkspace data){lock(gate)courses.Add(course.Id,(course,WorkspaceService.Clone(data)));}
        public IWorkspaceStore OpenCourse(string id)=>new PresentationWorkspaceStore(this,id);
        public void UpdateCourse(CourseRecord original,CourseRecord updated)=>throw new NotSupportedException();
        public void DeleteCourse(string id)=>throw new NotSupportedException();
        public SemesterDeletionPreview InspectSemesterDeletion(string id)=>throw new NotSupportedException();
        public void DeleteSemester(SemesterDeletionPreview preview)=>throw new NotSupportedException();
        private sealed class PresentationWorkspaceStore(PresentationCatalogStore catalog,string id):IWorkspaceStore
        {
            public AcademicWorkspace? Load()
            {
                lock(catalog.gate)return catalog.courses.TryGetValue(id,out var value) ? WorkspaceService.Clone(value.Data) :
                    throw new AcademicValidationException("La materia seleccionada no existe.");
            }
            public void Save(AcademicWorkspace data)
            {
                lock(catalog.gate){var original=catalog.courses[id];catalog.courses[id]=(original.Course,WorkspaceService.Clone(data));}
            }
        }
    }
}
