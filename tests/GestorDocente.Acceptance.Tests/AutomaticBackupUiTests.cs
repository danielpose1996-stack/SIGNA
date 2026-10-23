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

public class AutomaticBackupUiTests
{
    [AvaloniaTheory]
    [InlineData(1100)][InlineData(1580)]
    public async Task Status_is_visible_and_replacing_model_stops_the_old_worker(int width)
    {
        var writer=new Writer();using var worker=new AutomaticBackupCoordinator(writer,()=>Path.GetTempPath());
        var model=new MainViewModel(new WorkspaceService(new Store()),automaticBackups:worker);
        var window=new MainWindow{DataContext=model,Width=width,Height=1000};window.Show();model.NavigateCommand.Execute("Respaldos");window.UpdateLayout();
        try
        {
            var label=window.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.Name=="AutomaticBackupStatus");
            Assert.Equal(model.AutomaticBackupStatus,label.Text);Assert.True(label.Bounds.Width>400);
            if(Environment.GetEnvironmentVariable("SIGNA_046_PREVIEW") is {} directory)
            {Directory.CreateDirectory(directory);using var image=window.CaptureRenderedFrame();image?.Save(Path.Combine(directory,$"respaldos-{width}.png"));}
            worker.Saved();window.DataContext=new MainViewModel(new WorkspaceService(new Store()));
            await worker.StopAsync(false);worker.Saved();await worker.StopAsync(true);Assert.Equal(0,writer.Count);
        }
        finally{window.Close();}
    }

    [AvaloniaFact]
    public async Task Local_maintenance_pauses_automatic_work_and_normal_closing_flushes_pending_changes()
    {
        var writer=new Writer();using var worker=new AutomaticBackupCoordinator(writer,()=>Path.GetTempPath());
        var model=new MainViewModel(new WorkspaceService(new Store()),automaticBackups:worker);
        worker.Saved();using(model.BeginLocalDataMaintenance())
        {await model.WaitForAutomaticBackupAsync();Assert.False(model.CanInteractWithWorkspace);Assert.Equal(0,writer.Count);}
        var window=new MainWindow{DataContext=model};window.Show();window.Close();
        for(var i=0;i<100 && window.IsVisible;i++)await Task.Delay(10);
        Assert.False(window.IsVisible);Assert.Equal(1,writer.Count);
    }
    [AvaloniaFact]
    public async Task Failed_close_backup_is_reported_and_cancelled_close_remains_usable()
    {
        var writer=new FailingWriter();using var worker=new AutomaticBackupCoordinator(writer,()=>Path.GetTempPath());
        var model=new MainViewModel(new WorkspaceService(new Store()),automaticBackups:worker);
        var window=new MainWindow{DataContext=model};window.Show();worker.Saved();window.Close();
        for(var i=0;i<100 && !window.OwnedWindows.OfType<ConfirmWindow>().Any();i++)await Task.Delay(10);
        var confirmation=Assert.Single(window.OwnedWindows.OfType<ConfirmWindow>());Assert.False(model.CanInteractWithWorkspace);
        confirmation.Close(false);for(var i=0;i<100 && !model.CanInteractWithWorkspace;i++)await Task.Delay(10);
        Assert.True(window.IsVisible);Assert.True(model.CanInteractWithWorkspace);Assert.Contains("No se pudo",model.AutomaticBackupStatus);
        writer.Fail=false;window.Close();for(var i=0;i<100 && window.IsVisible;i++)await Task.Delay(10);
        Assert.False(window.IsVisible);
    }
    private sealed class FailingWriter:IAutomaticBackupWriter
    {public bool Fail=true;public LocalBackupResult Create(string folder)=>Fail ? throw new IOException("Fallo ficticio") : new("ficticio.aulabk",0,0);}
    private sealed class Writer:IAutomaticBackupWriter
    {public int Count;public LocalBackupResult Create(string folder){Interlocked.Increment(ref Count);return new("ficticio.aulabk",0,0);}}
    private sealed class Store:IWorkspaceStore
    {public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();public void Save(AcademicWorkspace data){} }
}
