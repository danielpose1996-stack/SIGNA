using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using Xunit;
[assembly:AvaloniaTestApplication(typeof(GestorDocente.Acceptance.Tests.TestAppBuilder))]
namespace GestorDocente.Acceptance.Tests;
public sealed class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()=>AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions());
}
public class WindowTests
{
    [AvaloniaFact]
    public async Task Configuration_opens_as_owned_modal_and_clears_overlay_on_close()
    {
        var service=new WorkspaceService(new MemoryStore());var main=new MainWindow{DataContext=new MainViewModel(service)};main.Show();
        var dialog=new ConfigurationWindow{DataContext=new ConfigurationViewModel(service,1)};var task=main.ShowAcademicDialog(dialog);
        Assert.True(dialog.IsVisible);Assert.Equal(main,dialog.Owner);Assert.True(main.FindControl<Border>("ModalOverlay")!.IsVisible);
        dialog.Close();await task;Assert.False(main.FindControl<Border>("ModalOverlay")!.IsVisible);main.Close();
    }
    [AvaloniaFact]
    public void Both_views_load_and_remain_attached_during_navigation()
    {
        var vm=new MainViewModel(new WorkspaceService(new MemoryStore()));var window=new MainWindow{DataContext=vm};window.Show();
        vm.Page="Asistencias";window.UpdateLayout();Assert.True(vm.IsAttendance);
        vm.Page="Notas";window.Width=1100;window.Height=700;window.UpdateLayout();Assert.True(vm.IsNotes);Assert.NotNull(window.Content);window.Close();
    }
    [AvaloniaFact]
    public void Invalid_main_percentages_do_not_save_or_close_configuration()
    {
        var service=new WorkspaceService(new MemoryStore());var vm=new ConfigurationViewModel(service,1);
        var dialog=new ConfigurationWindow{DataContext=vm};dialog.Show();vm.Groups[0].Weight=1;
        Assert.False(vm.Save());Assert.Contains("100",vm.Error);Assert.True(dialog.IsVisible);Assert.Equal(30m,service.Cut(1).Groups[0].Weight);dialog.Close();
    }
    private sealed class MemoryStore:IWorkspaceStore
    {
        private AcademicWorkspace? data;
        public AcademicWorkspace? Load()=>data;
        public void Save(AcademicWorkspace value)=>data=WorkspaceService.Clone(value);
    }
}
