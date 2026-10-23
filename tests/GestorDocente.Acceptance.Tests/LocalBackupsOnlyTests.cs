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

public class LocalBackupsOnlyTests
{
    [AvaloniaFact]
    public void Backups_only_show_local_operations_and_preserve_other_commands()
    {
        var vm=new MainViewModel(new WorkspaceService(new Store()),localBackup:new Backup());
        var window=new MainWindow{DataContext=vm};window.Show();vm.NavigateCommand.Execute("Respaldos");window.UpdateLayout();
        try
        {
            var page=window.GetVisualDescendants().OfType<BackupsView>().Single();
            var texts=string.Join(" ",page.GetVisualDescendants().OfType<TextBlock>().Select(t=>t.Text));
            Assert.DoesNotContain("Drive",texts,StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("OAuth",texts,StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(typeof(MainViewModel).GetProperties(),p=>p.Name=="Drive");
            Assert.Contains(page.GetVisualDescendants().OfType<Button>(),b=>ReferenceEquals(b.Command,vm.CreateBackupCommand));
            Assert.Contains(page.GetVisualDescendants().OfType<Button>(),b=>ReferenceEquals(b.Command,vm.RestoreBackupCommand));
            Assert.False(vm.CreateBackupCommand.CanExecute(null));Assert.True(vm.ChooseBackupFolderCommand.CanExecute(null));Assert.True(vm.RestoreBackupCommand.CanExecute(null));
            vm.FileBusy=true;Assert.False(vm.CreateBackupCommand.CanExecute(null));Assert.False(vm.RestoreBackupCommand.CanExecute(null));
            Assert.NotNull(vm.ExportAttendancePdfCommand);Assert.NotNull(vm.AddStudentCommand);
        }
        finally{window.Close();}
    }
    private sealed class Store:IWorkspaceStore{public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();public void Save(AcademicWorkspace data){} }
    [AvaloniaTheory]
    [InlineData(1100)][InlineData(1580)]
    public async Task Backup_folder_and_generation_need_no_password_and_cleanup_requires_cancellable_confirmation(int width)
    {
        var folder=new MemoryBackupPreferencesStore();folder.SaveFolder(Path.GetTempPath());var reset=new ResetRecorder();
        var model=new MainViewModel(new WorkspaceService(new Store()),localBackup:new Backup(),backupPreferences:new(folder),dataReset:reset);
        var window=new MainWindow{DataContext=model,Width=width,Height=1000};window.Show();model.NavigateCommand.Execute("Respaldos");window.UpdateLayout();
        try
        {
            Assert.True(model.CreateBackupCommand.CanExecute(null));
            model.ClearLocalDataCommand.Execute(null);
            var confirmation=Assert.Single(window.OwnedWindows.OfType<ConfirmWindow>());
            Assert.Equal(0,reset.Calls);Assert.False(model.CanInteractWithWorkspace);confirmation.Close(false);
            for(var i=0;i<100 && model.FileBusy;i++)await Task.Delay(10);
            Assert.Equal(0,reset.Calls);Assert.True(model.CanInteractWithWorkspace);Assert.Contains("cancelada",model.BackupStatus);
            model.CreateBackupCommand.Execute(null);
            for(var i=0;i<100 && model.FileBusy;i++)await Task.Delay(10);
            Assert.Contains("Respaldo completo verificado",model.BackupStatus);Assert.Empty(window.OwnedWindows.OfType<BackupPasswordWindow>());
            var page=window.GetVisualDescendants().OfType<BackupsView>().Single();
            Assert.Equal("Generar respaldo",page.FindControl<Button>("CreateLocalBackupButton")!.Content);
            Assert.Equal(model.BackupFolderLabel,page.FindControl<TextBlock>("BackupFolderPath")!.Text);
            Assert.NotNull(page.FindControl<Button>("ChooseBackupFolderButton"));Assert.NotNull(page.FindControl<Button>("ClearLocalDataButton"));
            if(Environment.GetEnvironmentVariable("SIGNA_045_PREVIEW") is {} directory)
            {
                Directory.CreateDirectory(directory);window.UpdateLayout();using var image=window.CaptureRenderedFrame();image?.Save(Path.Combine(directory,$"respaldos-{width}.png"));
            }
        }
        finally{window.Close();}
    }
    [AvaloniaFact]
    public void Pending_grade_draft_prevents_maintenance_and_does_not_discard_the_input()
    {
        var model=new MainViewModel(new WorkspaceService(new Store()));var cell=model.GradeRows[0].Cells["t1"];cell.Input="pendiente";
        Assert.Throws<AcademicValidationException>(()=>model.BeginLocalDataMaintenance());
        Assert.Equal("pendiente",cell.Input);Assert.False(model.FileBusy);Assert.True(model.CanInteractWithWorkspace);
    }
    private sealed class ResetRecorder:ILocalDataResetService
    {
        public int Calls;
        public LocalDataResetResult Reset(bool confirmed){Calls++;throw new InvalidOperationException("La cancelación no debe limpiar datos");}
    }
    [AvaloniaFact]
    public void Window_waits_for_local_data_io_before_closing()
    {
        var model=new MainViewModel(new WorkspaceService(new Store()));var window=new MainWindow{DataContext=model};window.Show();
        using(var operation=model.BeginLocalDataMaintenance()){window.Close();Assert.True(window.IsVisible);}
        window.Close();Assert.False(window.IsVisible);
    }
    private sealed class Backup:ILocalBackupService
    {
        public LocalBackupResult Create(string destination)=>new(destination,0,0);
        public LocalBackupResult CreateInFolder(string folder)=>new(Path.Combine(folder,"ficticio.aulabk"),0,0);
        public bool RequiresPassword(string archive)=>false;
        public LocalBackupResult Inspect(string archive,string? legacyPassword=null)=>throw new NotSupportedException();
        public LocalRestoreResult Restore(string archive,string? legacyPassword=null)=>throw new NotSupportedException();
    }
}
