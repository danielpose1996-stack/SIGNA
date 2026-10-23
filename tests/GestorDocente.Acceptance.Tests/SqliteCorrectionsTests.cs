using Avalonia.Headless.XUnit;
using Avalonia.Controls;
using Avalonia.Threading;
using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Microsoft.Data.Sqlite;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class SqliteCorrectionsTests
{
    [AvaloniaFact]
    public void Grade_change_replaces_only_its_row_and_preserves_attendance_selection()
    {
        var service=new WorkspaceService(new MemoryStore());var vm=new MainViewModel(service);
        var window=new MainWindow{DataContext=vm};window.Show();vm.OpenCourseCommand.Execute(null);window.UpdateLayout();
        var gradePrior=vm.GradeRows[0];var gradeOther=vm.GradeRows[1];var attendance=vm.AttendanceRows[0];vm.SelectedStudent=attendance;
        service.SetGrade(1,"s1","t1",4.9m);
        Assert.NotSame(gradePrior,vm.GradeRows[0]);
        Assert.Same(gradeOther,vm.GradeRows[1]);
        Assert.Same(attendance,vm.AttendanceRows[0]);
        Assert.Same(attendance,vm.SelectedStudent);
        window.Close();
    }

    [AvaloniaFact]
    public void Post_commit_subscriber_failure_does_not_claim_grade_was_lost()
    {
        var store=new MemoryStore();var service=new WorkspaceService(store);var vm=new MainViewModel(service);
        service.Changed+=()=>throw new IOException("Error ficticio del observador");
        var cell=vm.GradeRows[0].Cells["t1"];cell.Input="4,8";cell.Commit();
        Assert.Contains("se guardó",cell.Error,StringComparison.OrdinalIgnoreCase);
        Assert.Equal(4.8m,new WorkspaceService(store).Cut(1).Grades.Single(g=>g.StudentId=="s1" && g.ActivityId=="t1").Value);
    }

    [AvaloniaFact]
    public async Task Grade_save_does_not_block_ui_and_rejects_a_second_write()
    {
        var store=new DelayedStore();var service=new WorkspaceService(store);var vm=new MainViewModel(service);
        var window=new MainWindow{DataContext=vm};window.Show();vm.OpenCourseCommand.Execute(null);window.UpdateLayout();
        var cell=vm.GradeRows[0].Cells["t1"];cell.Input="4,7";
        try
        {
            var save=cell.CommitAsync();await store.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var responsive=false;await Dispatcher.UIThread.InvokeAsync(()=>responsive=true);
            Assert.True(responsive);Assert.False(save.IsCompleted);Assert.True(cell.IsSaving);
            Assert.Throws<AcademicValidationException>(()=>service.AddWeek(1));
            store.Release.Set();await save.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(cell.IsSaving);Assert.Empty(cell.Error);
            Assert.Equal(4.7m,service.Cut(1).Grades.Single(g=>g.StudentId=="s1" && g.ActivityId=="t1").Value);
        }
        finally{store.Release.Set();window.Close();}
    }

    [AvaloniaFact]
    public async Task Backups_create_directly_in_the_selected_folder_without_a_password_dialog()
    {
        var vm=new MainViewModel(new WorkspaceService(new MemoryStore()),localBackup:new StubBackup());
        var window=new MainWindow{DataContext=vm};window.Show();window.UpdateLayout();
        await vm.SetBackupFolderAsync(Path.GetTempPath());
        vm.NavigateCommand.Execute("Respaldos");vm.CreateBackupCommand.Execute(null);
        for(var i=0;i<100 && vm.FileBusy;i++)await Task.Delay(10);
        await Dispatcher.UIThread.InvokeAsync(()=>{});
        Assert.Empty(window.OwnedWindows.OfType<BackupPasswordWindow>());
        Assert.Contains("Respaldo completo verificado",vm.BackupStatus);
        Assert.False(vm.FileBusy);
        window.Close();
    }

    [AvaloniaFact]
    public async Task Sqlite_writer_lock_times_out_without_blocking_ui_or_losing_grade_draft()
    {
        var folder=Path.Combine(Path.GetTempPath(),"aula-ui-lock-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var path=Path.Combine(folder,"fixture.db");
        try
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));var semester=catalog.CreateSemester(2026,1);
            var course=catalog.CreateCourse(semester.Id,"LOCK","Materia ficticia","Escuela","");
            var service=catalog.Open(course.Id);service.ImportStudents([new("F-1","Estudiante ficticio")]);
            service.ConfigureGroups(1,[new(){Id="p",Name="Parcial",Weight=100}]);
            var vm=new MainViewModel(service,catalog);var window=new MainWindow{DataContext=vm};window.Show();vm.OpenCourseCommand.Execute(null);window.UpdateLayout();
            using var blocker=new SqliteConnection($"Data Source={path};Pooling=False");blocker.Open();
            using var transaction=blocker.BeginTransaction();
            var cell=vm.GradeRows[0].Cells["p"];cell.Input="4,8";
            try
            {
                var save=cell.CommitAsync();
                var responsive=false;await Dispatcher.UIThread.InvokeAsync(()=>responsive=true);
                Assert.True(responsive);Assert.False(save.IsCompleted);
                await save.WaitAsync(TimeSpan.FromSeconds(9));
                Assert.Contains("SqliteException",cell.Error);
                Assert.Contains("SQLite 5/5",cell.Error);
                Assert.Equal("4,8",cell.Input);
                Assert.Empty(service.Cut(1).Grades);
            }
            finally{transaction.Rollback();window.Close();}
        }
        finally{SqliteConnection.ClearAllPools();Directory.Delete(folder,true);}
    }
    [AvaloniaFact]
    public void Damaged_database_startup_has_a_recovery_window()
    {
        var window=new RecoveryStartupWindow("C:\\datos-ficticios\\base.db","Base ficticia dañada");
        window.Show();window.UpdateLayout();
        Assert.True(window.IsVisible);
        Assert.NotNull(window.FindControl<Avalonia.Controls.Button>("RestoreButton"));
        window.Close();
    }

    private sealed class MemoryStore:IWorkspaceStore
    {
        private AcademicWorkspace data=DemoWorkspaceFactory.Create();
        public AcademicWorkspace? Load()=>WorkspaceService.Clone(data);
        public void Save(AcademicWorkspace value)=>data=WorkspaceService.Clone(value);
    }
    private sealed class DelayedStore:IWorkspaceStore
    {
        public TaskCompletionSource Entered{get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ManualResetEventSlim Release{get;}=new(false);
        public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();
        public void Save(AcademicWorkspace value)
        {
            Entered.SetResult();
            if(!Release.Wait(TimeSpan.FromSeconds(10)))throw new TimeoutException("La prueba no liberó el guardado.");
        }
    }
    private sealed class StubBackup:ILocalBackupService
    {
        public LocalBackupResult Create(string destination)=>new(destination,0,0);
        public LocalBackupResult CreateInFolder(string folder)=>new(Path.Combine(folder,"ficticio.aulabk"),0,0);
        public bool RequiresPassword(string archive)=>false;
        public LocalBackupResult Inspect(string archive,string? legacyPassword=null)=>throw new NotSupportedException();
        public LocalRestoreResult Restore(string archive,string? legacyPassword=null)=>throw new NotSupportedException();
    }
}
