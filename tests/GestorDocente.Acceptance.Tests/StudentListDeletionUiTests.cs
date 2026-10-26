using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class StudentListDeletionUiTests
{
    [AvaloniaTheory]
    [InlineData("Notas")][InlineData("Asistencias")]
    public async Task Confirmation_cancels_or_clears_the_whole_course_even_with_a_filter(string page)
    {
        var store=new Store();var vm=new MainViewModel(new WorkspaceService(store),workbookReader:new ExcelStudentReader());
        var window=new MainWindow{DataContext=vm,Width=1580,Height=950};window.Show();await vm.OpenCourseAsync(null);vm.Page=page;
        try
        {
            var total=vm.Service.Current.Students.Count;vm.Search="Ana Torres";Assert.Single(vm.GradeRows);
            var before=JsonSerializer.Serialize(vm.Service.Current);vm.DeleteStudentListCommand.Execute(null);
            var dialog=Assert.Single(window.OwnedWindows.OfType<ConfirmWindow>());
            var text=string.Join(" ",dialog.GetVisualDescendants().OfType<TextBlock>().Select(t=>t.Text));
            Assert.Contains($"{total} estudiantes",text);Assert.Contains(vm.CourseName,text);Assert.Contains("todos los cortes",text);
            Assert.Contains("filtro",text);Assert.False(vm.DeleteStudentListCommand.CanExecute(null));Assert.Equal(0,store.Saves);
            Assert.False(vm.CanInteractWithWorkspace);window.Close();Assert.True(window.IsVisible);
            var action=Assert.Single(dialog.GetVisualDescendants().OfType<Button>(),b=>Equals(b.Content,"Eliminar lista"));
            Assert.Contains("danger",action.Classes);
            if(Environment.GetEnvironmentVariable("SIGNA_047_PREVIEW") is {} folder)
            {Directory.CreateDirectory(folder);dialog.UpdateLayout();using var image=dialog.CaptureRenderedFrame();image?.Save(Path.Combine(folder,$"confirmacion-{page}.png"));}
            dialog.RaiseEvent(new Avalonia.Input.KeyEventArgs{RoutedEvent=Avalonia.Input.InputElement.KeyDownEvent,Key=Avalonia.Input.Key.Escape});await Idle(vm);
            Assert.Equal(before,JsonSerializer.Serialize(vm.Service.Current));Assert.Equal(0,store.Saves);Assert.True(vm.CanInteractWithWorkspace);
            vm.DeleteStudentListCommand.Execute(null);Assert.Single(window.OwnedWindows.OfType<ConfirmWindow>()).Close(true);await Idle(vm);
            Assert.Equal(1,store.Saves);Assert.Empty(vm.Service.Current.Students);Assert.Empty(vm.GradeRows);Assert.Empty(vm.AttendanceRows);
            Assert.Empty(vm.Consolidated.Rows);Assert.Null(vm.SelectedStudent);Assert.Equal(0,vm.GroupAttendanceSummary.Recorded);
            Assert.Equal("",vm.SearchInput);Assert.Contains("Lista eliminada",vm.Message);
            Assert.False(vm.DeleteStudentListCommand.CanExecute(null));Assert.True(vm.AddStudentCommand.CanExecute(null));Assert.True(vm.ImportStudentsCommand.CanExecute(null));
        }
        finally{window.Close();}
    }

    [AvaloniaFact]
    public async Task Failed_write_or_stale_confirmation_is_reported_and_pending_edits_are_not_discarded()
    {
        var store=new Store();var vm=new MainViewModel(new WorkspaceService(store));var window=new MainWindow{DataContext=vm};window.Show();await vm.OpenCourseAsync(null);
        try
        {
            var before=JsonSerializer.Serialize(vm.Service.Current);var savedInput=vm.GradeRows[0].Cells["t1"].Input;vm.GradeRows[0].Cells["t1"].Input="pendiente";
            vm.DeleteStudentListCommand.Execute(null);Assert.Empty(window.OwnedWindows.OfType<ConfirmWindow>());Assert.Contains("pendientes",vm.Message);
            Assert.Equal("pendiente",vm.GradeRows[0].Cells["t1"].Input);Assert.Equal(before,JsonSerializer.Serialize(vm.Service.Current));
            vm.GradeRows[0].Cells["t1"].Input=savedInput;
            vm.DeleteStudentListCommand.Execute(null);var dialog=Assert.Single(window.OwnedWindows.OfType<ConfirmWindow>());
            await vm.Service.AddStudentAsync("Matrícula posterior ficticia");var afterEnrollment=JsonSerializer.Serialize(vm.Service.Current);dialog.Close(true);await Idle(vm);
            Assert.Equal(afterEnrollment,JsonSerializer.Serialize(vm.Service.Current));Assert.Contains("cambió",vm.Message);
            store.Fail=true;vm.DeleteStudentListCommand.Execute(null);Assert.Single(window.OwnedWindows.OfType<ConfirmWindow>()).Close(true);await Idle(vm);
            Assert.Equal(afterEnrollment,JsonSerializer.Serialize(vm.Service.Current));Assert.Contains("no se completó",vm.Message);Assert.True(vm.DeleteStudentListCommand.CanExecute(null));
        }
        finally{window.Close();}
    }

    [AvaloniaTheory]
    [InlineData(1830,1,"Notas")][InlineData(1580,1,"Notas")][InlineData(1100,1,"Notas")][InlineData(1100,1.25,"Notas")]
    [InlineData(1580,1,"Asistencias")][InlineData(1100,1,"Asistencias")][InlineData(1100,1.25,"Asistencias")]
    public async Task Delete_button_fits_the_toolbar_and_remains_accessible(double width,double scale,string page)
    {
        var vm=new MainViewModel(new WorkspaceService(new Store()));var window=new MainWindow{DataContext=vm,Width=width,Height=950};
        if(scale!=1 && window.Content is Control root){window.Content=null;window.Content=new LayoutTransformControl{LayoutTransform=new ScaleTransform(scale,scale),Child=root};}
        window.Show();await vm.OpenCourseAsync(null);vm.Page=page;window.UpdateLayout();
        try
        {
            var view=page=="Notas" ? (Control)window.FindControl<NotesView>("RetainedNotes")! : window.GetVisualDescendants().OfType<AttendanceView>().Single();
            var button=view.FindControl<Button>("DeleteStudentListButton")!;Assert.NotNull(button);Assert.True(button.IsEffectivelyVisible);
            Assert.Equal("Eliminar lista de estudiantes",AutomationProperties.GetName(button));Assert.Same(vm.DeleteStudentListCommand,button.Command);
            var start=button.TranslatePoint(default,view)!.Value;var end=button.TranslatePoint(new(button.Bounds.Width,button.Bounds.Height),view)!.Value;
            Assert.InRange(start.X,0,view.Bounds.Width);Assert.InRange(end.X,0,view.Bounds.Width+1);
            Assert.InRange(button.Bounds.Height,34,46);Assert.True(button.Bounds.Width>=32);
            vm.FileBusy=true;window.UpdateLayout();Assert.False(button.IsEffectivelyEnabled);vm.FileBusy=false;
            if(Environment.GetEnvironmentVariable("SIGNA_047_PREVIEW") is {} folder)
            {Directory.CreateDirectory(folder);window.UpdateLayout();using var image=window.CaptureRenderedFrame();image?.Save(Path.Combine(folder,$"lista-{page}-{width}-{scale}.png"));}
        }
        finally{window.Close();}
    }
    private static async Task Idle(MainViewModel vm)
    {for(var i=0;i<500 && vm.FileBusy;i++)await Task.Delay(10);Assert.False(vm.FileBusy);}
    private sealed class Store:IWorkspaceStore
    {
        public int Saves;public bool Fail;
        public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();
        public void Save(AcademicWorkspace data){if(Fail)throw new IOException("Fallo ficticio");Saves++;}
    }
}
