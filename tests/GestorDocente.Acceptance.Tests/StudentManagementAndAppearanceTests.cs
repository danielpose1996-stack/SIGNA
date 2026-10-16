using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Desktop.Controls;
using GestorDocente.Desktop.Styles;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Acceptance.Tests;
public class StudentManagementAndAppearanceTests
{
    [AvaloniaFact]
    public async Task Editing_refreshes_details_and_all_tables_and_hides_phone_again()
    {
        var service=new WorkspaceService(new Store());var main=new MainViewModel(service);var student=service.Current.Students[0];
        var details=new StudentDetailsViewModel(student,service);var window=new StudentDetailsWindow{DataContext=details};window.Show();
        try
        {
            Assert.True(window.FindControl<Button>("EditStudentButton")!.IsEnabled);
            var editor=details.Editor();Assert.Equal(student.FullName,editor.FullName);Assert.Equal('●',editor.PhoneMask);
            editor.FullName="Nombre ficticio editado";editor.Email="contacto@example.test";editor.Phone="3000000000";
            Assert.True(await editor.SaveAsync());details.Refresh();Assert.False(details.PhoneVisible);Assert.NotEqual(editor.Phone,details.PhoneDisplay);
            Assert.Equal(editor.FullName,details.FullName);Assert.Equal(editor.FullName,main.GradeRows[0].FullName);
            Assert.Equal(editor.FullName,main.AttendanceRows[0].FullName);Assert.Equal(editor.FullName,main.Consolidated.Rows[0].FullName);
            editor.Email="incorrecto";Assert.False(await editor.SaveAsync());Assert.NotEmpty(editor.Error);
        }
        finally{window.Close();}
    }
    [AvaloniaFact]
    public async Task Deletion_opens_confirmation_cancel_preserves_data_and_confirmation_updates_tables()
    {
        var service=new WorkspaceService(new Store());var main=new MainViewModel(service);var details=new StudentDetailsViewModel(service.Current.Students[0],service);
        var window=new StudentDetailsWindow{DataContext=details};window.Show();
        try
        {
            var delete=window.FindControl<Button>("DeleteStudentButton")!;
            delete.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));var confirmation=Assert.Single(window.OwnedWindows.OfType<ConfirmWindow>());
            Assert.Equal(10,service.Current.Students.Count);confirmation.Close(false);
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(()=>{});Assert.Equal(10,service.Current.Students.Count);
            delete.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Single(window.OwnedWindows.OfType<ConfirmWindow>()).Close(true);
            for(var i=0;i<100 && window.IsVisible;i++)await Task.Delay(10);
            Assert.False(window.IsVisible);
            Assert.Equal(9,main.GradeRows.Count);Assert.Equal(9,main.AttendanceRows.Count);Assert.Equal(9,main.Consolidated.Rows.Count);
            Assert.DoesNotContain(main.GradeRows,r=>r.Student.Id==details.Student.Id);
        }
        finally{window.Close();}
    }
    [AvaloniaFact]
    public async Task Color_and_avatar_apply_to_existing_controls_and_redundant_tabs_are_absent()
    {
        var preferences=new InterfacePreferencesService(new MemoryInterfacePreferencesStore());var vm=new MainViewModel(new WorkspaceService(new Store()),appearance:preferences);
        var window=new MainWindow{DataContext=vm};window.Show();vm.NavigateCommand.Execute("Notas");window.UpdateLayout();
        try
        {
            var retained=window.FindControl<NotesView>("RetainedNotes");
            var tabs=window.FindControl<Border>("CourseTabs")!.GetVisualDescendants().OfType<TextBlock>().Select(t=>t.Text).ToArray();
            Assert.Contains("Notas",tabs);Assert.Contains("Asistencias",tabs);Assert.Contains("Nota consolidada",tabs);
            Assert.DoesNotContain("Estudiantes",tabs);Assert.DoesNotContain("Configuración de evaluaciones",tabs);
            Assert.NotNull(vm.ConfigureCommand);Assert.NotNull(vm.ImportStudentsCommand);Assert.NotNull(vm.AddStudentCommand);
            var notes=window.GetVisualDescendants().OfType<Button>().Where(b=>ReferenceEquals(b.Command,vm.ConfigureCommand));Assert.NotEmpty(notes);
            vm.Appearance.HexInput="#6D4ACB";
            vm.Appearance.SelectedAvatar=InterfaceTheme.Avatars.Single(a=>a.Id==ProfileAvatar.Book);
            Assert.True(await vm.Appearance.SaveAsync());window.UpdateLayout();
            Assert.Same(retained,window.FindControl<NotesView>("RetainedNotes"));
            Assert.Equal("book",window.FindControl<VectorIcon>("ProfileAvatarIcon")!.Kind);
            Assert.Equal(Color.Parse("#172B45"),Assert.IsAssignableFrom<ISolidColorBrush>(window.Foreground).Color);
            var dialog=new StudentEditorWindow{DataContext=new StudentEditorViewModel(vm.Service)};dialog.Show(window);dialog.UpdateLayout();
            try{Assert.Equal(Color.Parse("#172B45"),Assert.IsAssignableFrom<ISolidColorBrush>(dialog.Foreground).Color);}finally{dialog.Close();}
            vm.Appearance.ResetDraft();vm.Appearance.SelectedAvatar=InterfaceTheme.Avatars[0];vm.Appearance.ResetDraft();Assert.Equal(ProfileAvatar.Book,vm.Appearance.SelectedAvatar.Id);
        }
        finally{window.Close();InterfaceTheme.Apply(Avalonia.Application.Current!,new());}
    }
    [Fact]
    public async Task Failed_preference_write_does_not_apply_or_change_active_values()
    {
        var service=new InterfacePreferencesService(new FailingPreferences());InterfacePreferences? applied=null;var vm=new AppearanceViewModel(service,p=>applied=p);
        vm.HexInput="#6D4ACB";Assert.Equal("#6D4ACB",applied!.Accent);Assert.False(await vm.SaveAsync());Assert.Equal(new InterfacePreferences(),service.Current);Assert.NotEmpty(vm.Error);
        vm.CancelPreview();Assert.Equal(service.Current.Accent,applied!.Accent);
    }
    [Fact]
    public void Accent_colors_keep_readable_text_and_primary_button_contrast()
    {
        foreach(var accent in new[]{"#0067F5","#007A66","#6D4ACB","#405A70"})
        {
            var brushes=InterfaceTheme.Brushes(new(Accent:accent));
            Assert.True(Contrast(Color.Parse(accent),Colors.White)>=4.5);
            Assert.True(Contrast(((ISolidColorBrush)brushes["Ink"]).Color,((ISolidColorBrush)brushes["Canvas"]).Color)>=7);
            Assert.True(Contrast(((ISolidColorBrush)brushes["Muted"]).Color,((ISolidColorBrush)brushes["Canvas"]).Color)>=4.5);
        }
    }
    private static double Contrast(Color a,Color b)
    {
        static double Linear(byte value){var s=value/255d;return s<=0.04045 ? s/12.92 : Math.Pow((s+0.055)/1.055,2.4);}
        static double Luminance(Color c)=>0.2126*Linear(c.R)+0.7152*Linear(c.G)+0.0722*Linear(c.B);
        var x=Luminance(a);var y=Luminance(b);return (Math.Max(x,y)+0.05)/(Math.Min(x,y)+0.05);
    }
    private sealed class Store:IWorkspaceStore{public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();public void Save(AcademicWorkspace data){} }
    private sealed class FailingPreferences:IInterfacePreferencesStore{public InterfacePreferences Load()=>new();public void Save(InterfacePreferences preferences)=>throw new IOException("Fallo ficticio");}
}
