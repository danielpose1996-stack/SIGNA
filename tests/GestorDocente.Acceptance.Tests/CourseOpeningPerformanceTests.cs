using Avalonia.Headless.XUnit;
using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class CourseOpeningPerformanceTests
{
    [AvaloniaFact]
    public async Task Opening_a_course_waiting_on_storage_keeps_the_current_screen_usable()
    {
        using var gate=new ManualResetEventSlim();
        using var entered=new ManualResetEventSlim();
        var catalog=new CatalogService(new WaitingCatalogStore(gate,entered));
        var initial=new WorkspaceService(new StaticStore(new AcademicWorkspace
        {
            IsDemo=false,CourseName="",CourseCode="",GroupName="",Period="",
            Cuts=AcademicRules.DefaultCuts()
        }));
        var vm=new MainViewModel(initial,catalog);
        var course=new CourseSummary("Materia ficticia","F-1","","2026-1",0,false,"course","Escuela");
        var opening=vm.OpenCourseAsync(course);
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
            Assert.False(opening.IsCompleted);
            Assert.Same(initial,vm.Service);
            Assert.True(vm.FileBusy);
        }
        finally{gate.Set();}
        await opening.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("course",vm.Service.Current.CourseId);
        Assert.True(vm.IsAcademicNavigation);
        Assert.False(vm.FileBusy);
    }
    [AvaloniaFact]
    public async Task Failed_course_open_keeps_the_previous_workspace_and_reports_the_error()
    {
        using var gate=new ManualResetEventSlim(true);
        using var entered=new ManualResetEventSlim();
        var catalog=new CatalogService(new WaitingCatalogStore(gate,entered,true));
        var original=new WorkspaceService(new StaticStore(new AcademicWorkspace
        {
            IsDemo=false,CourseName="",CourseCode="",GroupName="",Period="",
            Cuts=AcademicRules.DefaultCuts()
        }));
        var vm=new MainViewModel(original,catalog);
        await vm.OpenCourseAsync(new CourseSummary("Materia ficticia","F-1","","2026-1",0,false,"course","Escuela"));
        Assert.Same(original,vm.Service);
        Assert.False(vm.FileBusy);
        Assert.Contains("No se pudo abrir la materia",vm.Message);
    }
    [AvaloniaFact]
    public async Task Catalog_refresh_keeps_the_exact_student_count_after_an_import()
    {
        var path=Path.Combine(Path.GetTempPath(),$"aula-catalog-fixture-{Guid.NewGuid():N}.db");
        try
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));
            var semester=catalog.CreateSemester(2026,1);
            var course=catalog.CreateCourse(semester.Id,"F-1","Materia ficticia","Escuela","");
            var service=catalog.Open(course.Id);
            service.ImportStudents([new("F-1","Estudiante ficticio 1")]);
            var initial=new WorkspaceService(new StaticStore(new AcademicWorkspace
            {
                IsDemo=false,CourseName="",CourseCode="",GroupName="",Period="",
                Cuts=AcademicRules.DefaultCuts()
            }));
            var vm=new MainViewModel(initial,catalog);
            await vm.Home.RefreshAsync();
            Assert.Equal(1,Assert.Single(vm.Home.Courses).StudentCount);
            service.ImportStudents([new("F-2","Estudiante ficticio 2")]);
            await vm.Home.RefreshAsync();
            Assert.Equal(2,Assert.Single(vm.Home.Courses).StudentCount);
            Assert.Equal(semester.Id,vm.Home.SelectedSemester?.Id);
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }

    private sealed class StaticStore(AcademicWorkspace data):IWorkspaceStore
    {
        public AcademicWorkspace? Load()=>data;
        public void Save(AcademicWorkspace value)=>throw new InvalidOperationException();
    }
    private sealed class WaitingCatalogStore(ManualResetEventSlim gate,ManualResetEventSlim entered,bool fail=false):ICatalogStore
    {
        public IReadOnlyList<AcademicSemester> Semesters()=>[new("term",2026,1)];
        public IReadOnlyList<CourseRecord> Courses(string semesterId)=>[new("course","term","F-1","Materia ficticia","Escuela","")];
        public void AddSemester(AcademicSemester semester)=>throw new NotSupportedException();
        public void AddCourse(CourseRecord course,AcademicWorkspace workspace)=>throw new NotSupportedException();
        public void UpdateCourse(CourseRecord original,CourseRecord updated)=>throw new NotSupportedException();
        public SemesterDeletionPreview InspectSemesterDeletion(string semesterId)=>throw new NotSupportedException();
        public void DeleteSemester(SemesterDeletionPreview confirmed)=>throw new NotSupportedException();
        public void DeleteCourse(string courseId)=>throw new NotSupportedException();
        public IWorkspaceStore OpenCourse(string id)=>new WaitingStore(gate,entered,fail);
    }
    private sealed class WaitingStore(ManualResetEventSlim gate,ManualResetEventSlim entered,bool fail):IWorkspaceStore
    {
        public AcademicWorkspace? Load()
        {
            entered.Set();gate.Wait();
            if(fail)throw new IOException("Fallo de almacenamiento ficticio");
            return new AcademicWorkspace{IsDemo=false,CourseId="course",SemesterId="term",CourseName="Materia ficticia",
                CourseCode="F-1",GroupName="",School="Escuela",Period="2026-1",
                Cuts=AcademicRules.DefaultCuts()};
        }
        public void Save(AcademicWorkspace value)=>throw new InvalidOperationException();
    }
}
