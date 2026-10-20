using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class StudentToolbarPresentationTests
{
    [AvaloniaTheory]
    [InlineData(1580,1)] [InlineData(1100,1)] [InlineData(1400,1.25)] [InlineData(1100,1.25)]
    public async Task Student_actions_remain_visible_accessible_and_bound_to_existing_commands(double width,double scale)
    {
        var service=new WorkspaceService(new Store());var vm=new MainViewModel(service);
        var window=new MainWindow{DataContext=vm,Width=width,Height=850};
        if(scale!=1 && window.Content is Control root){window.Content=null;window.Content=new LayoutTransformControl{LayoutTransform=new ScaleTransform(scale,scale),Child=root};}
        window.Show();vm.OpenCourseCommand.Execute(null);window.UpdateLayout();
        try
        {
            var view=window.FindControl<NotesView>("RetainedNotes")!;
            var bar=view.FindControl<Border>("StudentActionsBar")!;Assert.NotNull(bar);
            var definitions=new[]{("AddStudentButton",vm.AddStudentCommand,"Agregar estudiante"),("ImportStudentsButton",vm.ImportStudentsCommand,"Importar Excel"),
                ("ConfigureActivitiesButton",vm.ConfigureCommand,"Actividades y porcentajes"),("RecoverRowsButton",vm.RecoveryCommand,"Recuperar retirados")};
            foreach(var (name,command,label) in definitions)
            {
                var button=view.FindControl<Button>(name)!;Assert.NotNull(button);Assert.True(button.IsEffectivelyVisible);
                Assert.Same(command,button.Command);Assert.Equal(label,AutomationProperties.GetName(button));
                var start=button.TranslatePoint(default,bar)!.Value;var end=button.TranslatePoint(new Point(button.Bounds.Width,button.Bounds.Height),bar)!.Value;
                Assert.InRange(start.X,0,bar.Bounds.Width);Assert.InRange(end.X,0,bar.Bounds.Width+1);
                Assert.InRange(end.Y,0,bar.Bounds.Height+1);
            }
            Assert.InRange(bar.Bounds.Height,40,64);
            var count=service.Current.Students.Count;var requested=0;vm.StudentEditorRequested+=()=>requested++;
            view.FindControl<Button>("AddStudentButton")!.Command!.Execute(null);Assert.Equal(1,requested);Assert.Equal(count,service.Current.Students.Count);
            // The real editor can be cancelled without persisting data.
            foreach(var dialog in window.OwnedWindows.ToArray())dialog.Close();
            await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);
            vm.FileBusy=true;window.UpdateLayout();
            foreach(var (name,command,_) in definitions)Assert.Equal(command.CanExecute(null),view.FindControl<Button>(name)!.IsEffectivelyEnabled);
            vm.FileBusy=false;window.UpdateLayout();
            if(Environment.GetEnvironmentVariable("SIGNA_038_PREVIEW") is {} folder)
            {
                Directory.CreateDirectory(folder);using var bitmap=window.CaptureRenderedFrame();bitmap?.Save(Path.Combine(folder,$"barra-estudiantes-{width}-{scale}.png"));
            }
        }
        finally{window.Close();}
    }
    private sealed class Store:IWorkspaceStore
    {
        public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();
        public void Save(AcademicWorkspace data){}
    }
}
