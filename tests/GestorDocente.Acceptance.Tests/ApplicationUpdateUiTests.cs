using Avalonia.Headless.XUnit;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.VisualTree;
using Avalonia.Media;
using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Desktop.Controls;
using GestorDocente.Desktop.Styles;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class ApplicationUpdateUiTests
{
    [AvaloniaFact]
    public async Task Automatic_check_runs_once_and_offline_does_not_block_manual_retry()
    {
        var updater=new Updater{Failure=new ApplicationUpdateException(ApplicationUpdateFailure.Offline,"Sin conexión")};
        var vm=new UpdatesViewModel(updater,new Safety());
        await vm.CheckAsync(automatic:true);await vm.CheckAsync(automatic:true);
        Assert.Equal(1,updater.Checks);Assert.False(vm.IsBusy);Assert.True(vm.CanUseApplication);Assert.False(vm.ShowNotification);
        updater.Failure=null;updater.Offer=new("1.7.0",true);await vm.CheckAsync();
        Assert.Equal(2,updater.Checks);Assert.True(vm.ShowNotification);Assert.Equal("1.6.0",vm.CurrentVersion);Assert.Equal("1.7.0",vm.AvailableVersion);
    }

    [AvaloniaFact]
    public async Task Download_safety_and_handoff_run_in_order_and_errors_keep_the_application_open()
    {
        var events=new List<string>();var updater=new Updater(events){Offer=new("1.7.0",true)};var safety=new Safety(events);
        var vm=new UpdatesViewModel(updater,safety,()=>{events.Add("lock");return new Lease(()=>events.Add("unlock"));});
        vm.RestartRequested+=()=>events.Add("restart");await vm.CheckAsync();await vm.InstallAsync();
        Assert.Equal(new[]{"lock","download","verify","handoff","restart","unlock"},events);Assert.Equal(100,vm.Progress);
        Assert.Equal(ApplicationUpdateState.Restarting,vm.State);
        events.Clear();updater=new Updater(events){Offer=new("1.7.0",true),DownloadFailure=new IOException("paquete ficticio dañado")};
        vm=new(updater,safety,()=>new Lease(()=>events.Add("unlock")));vm.RestartRequested+=()=>events.Add("restart");
        await vm.CheckAsync();await vm.InstallAsync();Assert.Equal(new[]{"download","unlock"},events);Assert.True(vm.CanUseApplication);Assert.True(vm.CanInstall);Assert.NotEmpty(vm.Status);
    }

    [AvaloniaFact]
    public async Task Failed_safety_or_pending_edits_prevent_handoff_and_allow_retry()
    {
        var updater=new Updater{Offer=new("1.7.0",true)};var safety=new Safety{Failure=new IOException("SQLite ficticio")};
        var vm=new UpdatesViewModel(updater,safety);vm.RestartRequested+=()=>throw new InvalidOperationException("No debe reiniciar");
        await vm.CheckAsync();await vm.InstallAsync();Assert.Equal(0,updater.Handoffs);Assert.True(vm.CanUseApplication);
        var blocked=new UpdatesViewModel(updater,new Safety(),()=>throw new InvalidOperationException("Nota sin guardar"));
        blocked.RestartRequested+=()=>throw new InvalidOperationException("No debe reiniciar");await blocked.CheckAsync();
        var downloads=updater.Downloads;await blocked.InstallAsync();Assert.Equal(downloads,updater.Downloads);Assert.Equal(0,updater.Handoffs);Assert.True(blocked.CanUseApplication);
    }

    [AvaloniaFact]
    public async Task Legacy_installation_and_no_update_are_reported_honestly_without_downloading()
    {
        var updater=new Updater();var vm=new UpdatesViewModel(updater,new Safety());
        await vm.CheckAsync();Assert.Equal(ApplicationUpdateState.Current,vm.State);Assert.False(vm.HasUpdate);
        updater.Offer=new("1.7.0",false);await vm.CheckAsync();Assert.True(vm.HasUpdate);Assert.False(vm.CanInstall);
        await vm.InstallAsync();Assert.Equal(0,updater.Downloads);Assert.Equal(0,updater.Handoffs);Assert.Contains("instal",vm.CompatibilityMessage);
    }

    [AvaloniaFact]
    public async Task Cancellation_and_double_click_do_not_start_two_downloads_or_restart()
    {
        var updater=new Updater{Offer=new("1.7.0",true),WaitForCancellation=true};var vm=new UpdatesViewModel(updater,new Safety());
        vm.RestartRequested+=()=>throw new InvalidOperationException("No debe reiniciar");await vm.CheckAsync();
        var install=vm.InstallAsync();await vm.InstallAsync();Assert.Equal(1,updater.Downloads);
        vm.Cancel();await install;Assert.Equal(0,updater.Handoffs);Assert.True(vm.CanUseApplication);
    }

    [AvaloniaFact]
    public async Task Academic_pending_inputs_and_running_operations_block_update_without_discarding_edits()
    {
        var updater=new Updater{Offer=new("1.7.0",true)};
        var model=new MainViewModel(new WorkspaceService(new Store()),updater:updater,updateSafety:new Safety());
        model.Updates.RestartRequested+=()=>throw new InvalidOperationException("No debe reiniciar");
        await model.Updates.CheckAsync();var cell=model.GradeRows[0].Cells["t1"];var previous=cell.Input;
        cell.Input="nota inválida";await model.Updates.InstallAsync();
        Assert.Equal(0,updater.Downloads);Assert.Equal("nota inválida",cell.Input);Assert.False(model.FileBusy);
        cell.Input=previous;model.FileBusy=true;await model.Updates.InstallAsync();Assert.Equal(0,updater.Downloads);model.FileBusy=false;
    }

    [AvaloniaFact]
    public async Task Display_rounding_is_not_mistaken_for_unsaved_input_or_written_back_during_update()
    {
        var workspace=DemoWorkspaceFactory.Create();var student=workspace.Students[0];var cut=workspace.Cuts[0];
        cut.Grades.RemoveAll(g=>g.StudentId==student.Id && g.ActivityId=="t1");cut.Grades.Add(new(student.Id,"t1",4.335m));
        var updater=new Updater{Offer=new("1.7.0",true)};
        var model=new MainViewModel(new WorkspaceService(new Store(workspace)),updater:updater,updateSafety:new Safety());
        model.Updates.RestartRequested+=()=>{};
        var cell=model.GradeRows[0].Cells["t1"];Assert.Equal("4,34",cell.Input);Assert.False(cell.HasPendingInput);
        await model.Updates.CheckAsync();await model.Updates.InstallAsync();
        Assert.Equal(1,updater.Handoffs);Assert.Equal(4.335m,new AcademicCutReadIndex(model.Service.Cut(1)).Grade(student.Id,"t1"));
    }

    [AvaloniaTheory]
    [InlineData(1200,1,"#0067F5")]
    [InlineData(1100,1.25,"#FFFFFF")]
    [InlineData(1580,1,"#000000")]
    public async Task Sidebar_notification_and_update_dialog_show_real_versions_and_fit_small_windows(double width,double scale,string accent)
    {
        var updater=new Updater{Offer=new("1.7.0",true)};
        var model=new MainViewModel(new WorkspaceService(new Store()),updater:updater,updateSafety:new Safety());
        InterfaceTheme.Apply(Avalonia.Application.Current!,new(Accent:accent));
        var main=new MainWindow{DataContext=model,Width=width,Height=850};
        if(scale!=1 && main.Content is Control root){main.Content=null;main.Content=new LayoutTransformControl{LayoutTransform=new ScaleTransform(scale,scale),Child=root};}
        main.Show();await model.Updates.CheckAsync();main.UpdateLayout();
        try
        {
            var button=main.FindControl<Button>("CheckUpdatesButton");Assert.NotNull(button);
            Assert.Equal("Buscar actualizaciones de SIGNA",AutomationProperties.GetName(button));
            Assert.Same(model.UpdatesCommand,button.Command);Assert.True(button.IsEffectivelyVisible);
            Assert.True(button.BorderThickness.Left>=1);Assert.True(Assert.IsAssignableFrom<ISolidColorBrush>(button.Background).Color.A>0);
            Assert.Contains(button.GetVisualDescendants().OfType<VectorIcon>(),icon=>icon.Kind=="restore");
            Assert.InRange(button.Bounds.Height,44,64);
            var start=button.TranslatePoint(default,main)!.Value;
            var end=button.TranslatePoint(new Point(button.Bounds.Width,button.Bounds.Height),main)!.Value;
            Assert.InRange(start.X,0,main.Bounds.Width);Assert.InRange(start.Y,0,main.Bounds.Height);
            Assert.InRange(end.X,0,main.Bounds.Width);Assert.InRange(end.Y,0,main.Bounds.Height);
            Assert.True(button.Focus());Assert.True(button.IsFocused);
            Assert.True(model.Updates.ShowNotification);Capture(main,$"aviso-actualizacion-{width}-{scale}.png");
            var dialog=new UpdatesWindow{DataContext=model.Updates,Width=480,Height=460};dialog.Show();dialog.UpdateLayout();
            try
            {
                Assert.Contains(dialog.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text=="1.7.0");
                Assert.Contains(dialog.GetVisualDescendants().OfType<Button>(),b=>b.Content is "Actualizar SIGNA" && b.IsEnabled);
                Capture(dialog,"actualizaciones.png");
            }
            finally{dialog.Close();}
            model.Updates.DismissCommand.Execute(null);Assert.False(model.Updates.ShowNotification);Assert.True(model.Updates.HasUpdate);
        }
        finally{main.Close();InterfaceTheme.Apply(Avalonia.Application.Current!,new());}
    }
    private static void Capture(Window window,string name)
    {
        if(Environment.GetEnvironmentVariable("SIGNA_UPDATE_PREVIEW") is not {Length:>0} directory)return;
        Directory.CreateDirectory(directory);window.CaptureRenderedFrame()?.Save(Path.Combine(directory,name));
    }
    private sealed class Store(AcademicWorkspace? workspace=null):IWorkspaceStore
    {
        public AcademicWorkspace? Load()=>workspace ?? DemoWorkspaceFactory.Create();public void Save(AcademicWorkspace data)=>throw new NotSupportedException();
    }
    private sealed class Lease(Action release):IDisposable{public void Dispose()=>release();}
    private sealed class Safety(List<string>? events=null):IUpdateSafetyCheck
    {
        public Exception? Failure;
        public Task VerifyAsync(CancellationToken cancellationToken=default){events?.Add("verify");return Failure is {} error ? Task.FromException(error) : Task.CompletedTask;}
    }
    private sealed class Updater(List<string>? events=null):IApplicationUpdater
    {
        public string CurrentVersion=>"1.6.0";public bool IsManagedInstallation=>true;
        public int Checks,Downloads,Handoffs;
        public Exception? Failure,DownloadFailure;
        public bool WaitForCancellation;
        public AvailableApplicationUpdate? Offer;
        public Task<AvailableApplicationUpdate?> CheckAsync(CancellationToken cancellationToken=default)
        {Checks++;return Failure is {} error ? Task.FromException<AvailableApplicationUpdate?>(error) : Task.FromResult(Offer);}
        public async Task DownloadAsync(AvailableApplicationUpdate update,IProgress<int> progress,CancellationToken cancellationToken=default)
        {
            Downloads++;events?.Add("download");if(DownloadFailure is {} error)throw error;
            if(WaitForCancellation)await Task.Delay(Timeout.Infinite,cancellationToken);progress.Report(100);
        }
        public Task PrepareRestartAsync(AvailableApplicationUpdate update,CancellationToken cancellationToken=default){Handoffs++;events?.Add("handoff");return Task.CompletedTask;}
    }
}
