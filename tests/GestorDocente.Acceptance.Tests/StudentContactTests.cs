using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using Xunit;
namespace GestorDocente.Acceptance.Tests;
public class StudentContactTests
{
    [AvaloniaFact]
    public void Details_hide_phone_until_the_eye_is_used_and_missing_contacts_are_supported()
    {
        var phone="+57 300 000 0000";var vm=new StudentDetailsViewModel(new("f","001","Estudiante ficticio","ficticio@example.com",phone));
        var window=new StudentDetailsWindow{DataContext=vm};window.Show();window.UpdateLayout();
        try
        {
            Assert.False(vm.PhoneVisible);Assert.DoesNotContain(window.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text==phone);
            vm.TogglePhoneCommand.Execute(null);window.UpdateLayout();Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text==phone);
            vm.TogglePhoneCommand.Execute(null);Assert.NotEqual(phone,vm.PhoneDisplay);
            var empty=new StudentDetailsViewModel(new("f","","Solo nombre"));Assert.Equal("No registrado",empty.Document);Assert.False(empty.TogglePhoneCommand.CanExecute(null));
        }
        finally{window.Close();}
    }
    [AvaloniaFact]
    public async Task Manual_registration_updates_all_tables_and_sidebar_keeps_attendance_pdf()
    {
        var service=new WorkspaceService(new Store());var vm=new MainViewModel(service){Page="Notas"};var main=new MainWindow{DataContext=vm};main.Show();
        try
        {
            var editor=new StudentEditorViewModel(service){FullName="Matrícula ficticia tardía"};Assert.True(await editor.SaveAsync());
            Assert.Equal(11,vm.GradeRows.Count);Assert.Equal(11,vm.AttendanceRows.Count);Assert.Equal(11,vm.Consolidated.Rows.Count);
            var students=main.GetVisualDescendants().OfType<Button>().Where(b=>ReferenceEquals(b.Command,vm.ViewStudentDetailsCommand)).ToList();Assert.NotEmpty(students);
            var details=Assert.IsType<Student>(students.First().CommandParameter);Assert.NotNull(details);
            Assert.DoesNotContain(main.GetVisualDescendants().OfType<Button>(),b=>b.CommandParameter as string=="Reportes");
            Assert.NotNull(vm.ExportAttendancePdfCommand);
            editor.FullName="";Assert.False(await editor.SaveAsync());Assert.Contains("nombre",editor.Error);
        }
        finally{main.Close();}
    }
    private sealed class Store:IWorkspaceStore{public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();public void Save(AcademicWorkspace data){} }
}
