using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class SemesterDeletionTests
{
    [AvaloniaFact]
    public async Task Confirmation_describes_related_data_cancel_preserves_it_and_deletion_clears_open_context()
    {
        var directory=Path.Combine(Path.GetTempPath(),"aula-semester-ui-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var catalog=new CatalogService(new SqliteCatalogStore(Path.Combine(directory,"catalog.db")));
            var semester=catalog.CreateSemester(2026,1);var retained=catalog.CreateSemester(2026,2);
            var course=catalog.CreateCourse(semester.Id,"F-01","Materia ficticia","Escuela","");
            var service=catalog.Open(course.Id);service.ImportStudents([new("F-01","Estudiante ficticio")]);
            var vm=new MainViewModel(service,catalog);await vm.Home.RefreshAsync();
            var main=new MainWindow{DataContext=vm};main.Show();
            try
            {
                var preview=catalog.InspectSemesterDeletion(semester.Id);
                var confirmation=main.ConfirmSemesterDeletionAsync(preview);
                var dialog=Assert.Single(main.OwnedWindows);dialog.UpdateLayout();
                Assert.Contains(dialog.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text?.Contains("Materias: 1. Registros de estudiantes: 1. Notas: 0. Asistencias: 0.")==true);
                Assert.True(main.FindControl<Border>("ModalOverlay")!.IsVisible);
                if(Environment.GetEnvironmentVariable("AULA_SEMESTER_CONFIRM_PREVIEW") is {Length:>0} previewPath)dialog.CaptureRenderedFrame()?.Save(previewPath);
                dialog.Close(false);Assert.False(await confirmation);
                Assert.False(main.FindControl<Border>("ModalOverlay")!.IsVisible);
                Assert.Single(catalog.Courses(semester.Id));Assert.True(vm.HasOpenCourse);
                await vm.DeleteSemesterAsync(preview);
                Assert.False(vm.HasOpenCourse);Assert.True(vm.IsCourses);
                Assert.Equal(retained.Id,Assert.Single(vm.Home.Semesters).Id);
                Assert.Empty(vm.Home.Courses);Assert.Null(vm.GroupAttendancePercentage);
            }
            finally{main.Close();}
        }
        finally{Directory.Delete(directory,true);}
    }
}
