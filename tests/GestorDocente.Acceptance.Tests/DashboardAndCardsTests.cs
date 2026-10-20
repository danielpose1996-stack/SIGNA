using Avalonia.Automation;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class DashboardAndCardsTests
{
    [AvaloniaFact]
    public async Task Deleting_the_open_course_clears_its_context_and_refreshes_cards()
    {
        var directory=Path.Combine(Path.GetTempPath(),"aula-cards-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var catalog=new CatalogService(new SqliteCatalogStore(Path.Combine(directory,"catalog.db")));
            var semester=catalog.CreateSemester(2026,1);
            var course=catalog.CreateCourse(semester.Id,"MAT-1","Materia ficticia","Escuela","");
            var vm=new MainViewModel(new WorkspaceService(new MemoryStore(new AcademicWorkspace{Cuts=AcademicRules.DefaultCuts()})),catalog);
            await vm.Home.RefreshAsync();
            var card=Assert.Single(vm.Home.Courses);
            await vm.OpenCourseAsync(card);
            Assert.Equal(course.Id,vm.Service.Current.CourseId);
            await vm.DeleteCourseAsync(card);
            Assert.False(vm.HasOpenCourse);Assert.True(vm.IsCourses);Assert.Empty(vm.Home.Courses);
            Assert.Empty(catalog.Courses(semester.Id));
        }
        finally{Directory.Delete(directory,true);}
    }
    [AvaloniaFact]
    public async Task Several_real_courses_wrap_into_card_columns_with_delete_actions()
    {
        var directory=Path.Combine(Path.GetTempPath(),"aula-grid-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var catalog=new CatalogService(new SqliteCatalogStore(Path.Combine(directory,"catalog.db")));
            var semester=catalog.CreateSemester(2026,2);
            for(var i=1;i<=3;i++)catalog.CreateCourse(semester.Id,$"MAT-{i}",$"Materia ficticia {i}","Escuela","");
            var vm=new MainViewModel(new WorkspaceService(new MemoryStore(new AcademicWorkspace{Cuts=AcademicRules.DefaultCuts()})),catalog);
            await vm.Home.RefreshAsync();
            var window=new MainWindow{DataContext=vm,Width=1200,Height=800};
            window.Show();vm.NavigateCommand.Execute("Materias");window.UpdateLayout();
            var buttons=window.GetVisualDescendants().OfType<Button>().Where(b=>Equals(b.Content,"Abrir materia")).ToArray();
            Assert.Equal(3,buttons.Length);
            var first=buttons[0].TranslatePoint(new Point(),window)!.Value;
            var second=buttons[1].TranslatePoint(new Point(),window)!.Value;
            Assert.Equal(first.Y,second.Y);Assert.True(second.X>first.X);
            Assert.Equal(3,window.GetVisualDescendants().OfType<Button>().Count(b=>AutomationProperties.GetName(b)=="Eliminar materia"));
            if(Environment.GetEnvironmentVariable("AULA_GRID_PREVIEW") is {Length:>0} path)
                window.CaptureRenderedFrame()?.Save(path);
            window.Close();
        }
        finally{Directory.Delete(directory,true);}
    }
    [AvaloniaFact]
    public async Task Course_cards_reflow_to_three_two_and_one_uniform_columns()
    {
        var directory=Path.Combine(Path.GetTempPath(),"aula-responsive-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var catalog=new CatalogService(new SqliteCatalogStore(Path.Combine(directory,"catalog.db")));
            var semester=catalog.CreateSemester(2026,2);
            for(var i=1;i<=6;i++)catalog.CreateCourse(semester.Id,$"MAT-{i}",$"Materia ficticia {i}","Escuela",$"G{i}");
            foreach(var (width,columns) in new[]{(1580d,3),(1200d,2),(800d,1)})
            {
                var vm=new MainViewModel(new WorkspaceService(new MemoryStore(new AcademicWorkspace{Cuts=AcademicRules.DefaultCuts()})),catalog);
                await vm.Home.RefreshAsync();vm.NavigateCommand.Execute("Materias");
                var window=new MainWindow{DataContext=vm,Width=width,Height=850};
                window.Show();
                try
                {
                    window.UpdateLayout();
                    var cards=window.GetVisualDescendants().OfType<Border>().Where(b=>b.Classes.Contains("course-tile")).ToArray();
                    Assert.Equal(6,cards.Length);
                    var points=cards.Select(c=>c.TranslatePoint(new Point(),window)!.Value).ToArray();
                    Assert.True(columns==points.TakeWhile(p=>Math.Abs(p.Y-points[0].Y)<1).Count(),
                        $"Ancho solicitado {width}, ventana {window.Bounds.Width}, área {window.ClientSize.Width}, tarjetas: {string.Join(";",points.Select(p=>$"{p.X:0},{p.Y:0}"))}");
                    Assert.All(cards,c=>Assert.InRange(c.Bounds.Width,cards[0].Bounds.Width-1,cards[0].Bounds.Width+1));
                    Assert.All(cards,c=>Assert.InRange(c.Bounds.Height,cards[0].Bounds.Height-1,cards[0].Bounds.Height+1));
                    Assert.All(cards,c=>Assert.True(c.TranslatePoint(new Point(c.Bounds.Width,0),window)!.Value.X<=window.Bounds.Width-10));
                    Assert.All(window.GetVisualDescendants().OfType<Button>().Where(b=>b.Content is "Crear semestre" or "Crear materia"),
                        b=>Assert.True(b.TranslatePoint(new Point(b.Bounds.Width,0),window)!.Value.X<=window.Bounds.Width-10));
                    if(Environment.GetEnvironmentVariable("AULA_MATERIAS_PREVIEW_DIR") is {Length:>0} preview)
                    {
                        Directory.CreateDirectory(preview);
                        window.CaptureRenderedFrame()?.Save(Path.Combine(preview,$"materias-{(int)width}.png"));
                    }
                    Assert.Equal(6,window.GetVisualDescendants().OfType<Button>().Count(b=>AutomationProperties.GetName(b)=="Editar materia"));
                }
                finally{window.Close();}
            }
        }
        finally{Directory.Delete(directory,true);}
    }
    [AvaloniaFact]
    public async Task Editing_card_prefills_dialog_and_refreshes_open_course_without_losing_students()
    {
        var directory=Path.Combine(Path.GetTempPath(),"aula-edit-card-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var catalog=new CatalogService(new SqliteCatalogStore(Path.Combine(directory,"catalog.db")));
            var first=catalog.CreateSemester(2026,1);var second=catalog.CreateSemester(2026,2);
            var original=catalog.CreateCourse(first.Id,"ANT","Nombre anterior","Escuela A","G1");
            var opened=catalog.Open(original.Id);opened.ImportStudents([new("F-01","Estudiante ficticio")]);
            var vm=new MainViewModel(opened,catalog);
            var editor=new CatalogEditorViewModel(catalog,first,false,catalog.Courses(first.Id).Single());
            Assert.Equal("ANT",editor.Code);Assert.Equal("Nombre anterior",editor.Name);
            Assert.Equal("G1",editor.Group);Assert.True(editor.IsEditingCourse);
            var dialog=new CatalogEditorWindow{DataContext=editor};dialog.Show();dialog.UpdateLayout();
            Assert.Contains(dialog.GetVisualDescendants().OfType<TextBox>(),box=>box.Text=="Nombre anterior");
            if(Environment.GetEnvironmentVariable("AULA_MATERIAS_EDITOR_PREVIEW") is {Length:>0} editorPreview)
                dialog.CaptureRenderedFrame()?.Save(editorPreview);
            dialog.Close();
            Assert.Equal("ANT",catalog.Open(original.Id).Current.CourseCode);
            editor.SelectedSemester=second;editor.Code="NUE";editor.Name="Nombre nuevo";editor.School="Escuela B";
            Assert.True(await editor.SaveAsync());
            await vm.RefreshEditedCourseAsync(editor.SavedCourseId,editor.SavedSemesterId);
            Assert.Equal("Nombre nuevo",vm.CourseName);Assert.Equal("2026-2",vm.Service.Current.Period);
            Assert.Equal("NUE",vm.Service.Current.CourseCode);
            Assert.Single(vm.Service.Current.Students);
            Assert.Equal(second.Id,vm.Home.SelectedSemester?.Id);
            Assert.Single(vm.Home.Courses);
        }
        finally{Directory.Delete(directory,true);}
    }
    [AvaloniaFact]
    public async Task Home_shows_three_cut_statistics_and_sidebar_omits_students_and_settings()
    {
        var vm=new MainViewModel(new WorkspaceService(new MemoryStore(DemoWorkspaceFactory.Create())));
        await vm.Dashboard.RefreshAsync();
        var window=new MainWindow{DataContext=vm,Width=1200,Height=800};window.Show();window.UpdateLayout();
        Assert.True(vm.IsHome);Assert.Single(vm.Dashboard.Courses);
        Assert.Equal(3,vm.Dashboard.Courses[0].Cuts.Count);
        Assert.Equal("10 estudiantes",vm.Dashboard.Courses[0].StudentsLabel);
        Assert.DoesNotContain(window.GetVisualDescendants().OfType<Button>(),b=>Equals(b.Content,"Ver resultados por estudiante"));
        Assert.Null(window.FindControl<DataGrid>("PerformanceTable"));
        var sidebar=window.GetVisualDescendants().OfType<Button>().Where(b=>b.Classes.Contains("nav")).ToArray();
        Assert.DoesNotContain(sidebar,b=>b.GetVisualDescendants().OfType<TextBlock>().Any(t=>t.Text is "Estudiantes" or "Configuración"));
        if(Environment.GetEnvironmentVariable("AULA_DASHBOARD_PREVIEW") is {Length:>0} path)
            window.CaptureRenderedFrame()?.Save(path);
        vm.NavigateCommand.Execute("Materias");window.UpdateLayout();
        Assert.True(vm.IsCourses);
        Assert.Single(window.GetVisualDescendants().OfType<Button>(),b=>Equals(b.Content,"Abrir materia"));
        if(Environment.GetEnvironmentVariable("AULA_CARDS_PREVIEW") is {Length:>0} cardsPath)
            window.CaptureRenderedFrame()?.Save(cardsPath);
        window.Close();
    }

    [AvaloniaFact]
    public void Configuration_rows_expose_separate_trash_buttons()
    {
        var service=new WorkspaceService(new MemoryStore(DemoWorkspaceFactory.Create()));
        var model=new ConfigurationViewModel(service,1);
        var window=new ConfigurationWindow{DataContext=model};window.Show();window.UpdateLayout();
        Assert.Equal(3,window.FindControl<DataGrid>("GroupsTable")!.Columns.Count);
        model.SelectedGroup=model.Groups.First(g=>g.Mode==EvaluationMode.SubActivities);
        window.UpdateLayout();
        Assert.Equal(3,window.FindControl<DataGrid>("ActivitiesTable")!.Columns.Count);
        Assert.Contains(window.GetVisualDescendants().OfType<Button>(),b=>AutomationProperties.GetName(b)=="Eliminar actividad principal");
        Assert.Contains(window.GetVisualDescendants().OfType<Button>(),b=>AutomationProperties.GetName(b)=="Eliminar subactividad");
        if(Environment.GetEnvironmentVariable("AULA_CONFIG_PREVIEW") is {Length:>0} path)
            window.CaptureRenderedFrame()?.Save(path);
        window.Close();
    }

    [AvaloniaFact]
    public void Attendance_pdf_button_uses_selected_student_and_cut_command()
    {
        var vm=new MainViewModel(new WorkspaceService(new MemoryStore(DemoWorkspaceFactory.Create())),attendancePdfExporter:new AttendancePdfExporter());
        var window=new MainWindow{DataContext=vm};window.Show();
        vm.Page="Asistencias";vm.DetailVisible=true;window.UpdateLayout();
        var button=window.FindControl<AttendanceView>("RetainedAttendance")!.FindControl<Button>("AttendancePdfButton")!;
        Assert.NotNull(vm.SelectedStudent);Assert.NotNull(button.Command);
        Assert.True(button.Command.CanExecute(null));
        // La ruta y el estudiante se toman de la selección vigente al iniciar la exportación.
        Assert.Equal(vm.ExportAttendancePdfCommand,button.Command);
        window.Close();
    }

    private sealed class MemoryStore(AcademicWorkspace initial):IWorkspaceStore
    {
        private AcademicWorkspace data=initial;
        public AcademicWorkspace? Load()=>WorkspaceService.Clone(data);
        public void Save(AcademicWorkspace value)=>data=WorkspaceService.Clone(value);
    }
}
