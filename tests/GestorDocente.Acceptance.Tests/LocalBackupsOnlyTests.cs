using Avalonia.Controls;
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
            Assert.True(vm.CreateBackupCommand.CanExecute(null));Assert.True(vm.RestoreBackupCommand.CanExecute(null));
            vm.FileBusy=true;Assert.False(vm.CreateBackupCommand.CanExecute(null));Assert.False(vm.RestoreBackupCommand.CanExecute(null));
            Assert.NotNull(vm.ExportAttendancePdfCommand);Assert.NotNull(vm.AddStudentCommand);
        }
        finally{window.Close();}
    }
    private sealed class Store:IWorkspaceStore{public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();public void Save(AcademicWorkspace data){} }
    private sealed class Backup:ILocalBackupService
    {
        public LocalBackupResult Create(string destination,string password)=>throw new NotSupportedException();
        public LocalBackupResult Inspect(string archive,string password)=>throw new NotSupportedException();
        public LocalRestoreResult Restore(string archive,string password)=>throw new NotSupportedException();
    }
}
