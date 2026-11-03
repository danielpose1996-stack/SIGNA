using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Domain;
using Xunit;
namespace GestorDocente.Application.Tests;

public class AttendanceConsolidatedTests
{
    [Fact]
    public async Task Changes_in_any_cut_update_one_student_and_search_never_reads_storage()
    {
        var store=new Store();var service=new WorkspaceService(store);var main=new MainViewModel(service);var view=main.AttendanceConsolidated;
        var other=view.Rows[1];await service.SetAttendanceAsync(3,"s1",1,AttendanceState.Absent);
        Assert.Equal(1,main.SelectedCut);Assert.Equal(1,view.Rows[0].Absences[3]);Assert.Equal(1,view.Rows[0].Total);Assert.Same(other,view.Rows[1]);
        await service.SetAttendanceAsync(3,"s1",1,AttendanceState.Late);Assert.Equal(0,view.Rows[0].Total);
        await service.SetAttendanceAsync(2,"s1",1,AttendanceState.Absent);await service.SetAttendanceAsync(1,"s1",1,AttendanceState.Absent);Assert.Equal(2,view.Rows[0].Total);
        var loads=store.Loads;var saves=store.Saves;view.Search="No existe";Assert.True(view.IsEmpty);view.Search="DEMO-1";Assert.Single(view.Rows);
        view.Search="";main.Page="Asistencia consolidada";Assert.True(main.IsAttendanceConsolidated);Assert.False(main.ShowCutSelector);
        Assert.Equal(loads,store.Loads);Assert.Equal(saves,store.Saves);
    }
    [Fact]
    public async Task Weeks_cuts_and_students_refresh_the_projection_without_changing_grades()
    {
        var service=new WorkspaceService(new Store());var main=new MainViewModel(service);var view=main.AttendanceConsolidated;
        await service.SetAttendanceAsync(2,"s1",1,AttendanceState.Absent);await service.RemoveWeekAsync(2,1);Assert.Equal(0,view.Rows[0].Total);
        await service.RestoreWeekAsync(2,1);Assert.Equal(1,view.Rows[0].Total);
        await service.RenameCutsAsync(service.Current,new Dictionary<int,string>{{1,"Primero"},{2,"Segundo"},{3,"Final"}});Assert.Equal("Segundo",view.Cuts[1].Label);
        await service.AddCutAsync();Assert.Equal(4,view.Cuts.Count);Assert.Equal(0,view.Rows[0].Absences[4]);
        await service.DeleteCutAsync(service.PreviewCutDeletion(2),true);Assert.Equal(0,view.Rows[0].Total);Assert.Equal(3,view.Cuts.Count);
        await service.AddStudentAsync("Alumno nuevo");Assert.Equal(3,view.Rows.Count);Assert.Equal(0,view.Rows[^1].Total);
        Assert.All(service.Current.Cuts,c=>Assert.Empty(c.Grades));
        view.Refresh(new(){CourseName="Otra materia",Students=[new("otro","","Alumno independiente")],Cuts=[new(){Number=1,Weight=100}]});
        Assert.Single(view.Rows);Assert.Equal("Alumno independiente",view.Rows[0].FullName);Assert.Equal(0,view.Rows[0].Total);
    }
    private sealed class Store:IWorkspaceStore
    {
        public int Loads,Saves;
        private AcademicWorkspace data=new(){Students=[new("s1","DEMO-1","Alumno uno"),new("s2","DEMO-2","Alumno dos")],Cuts=[
            new(){Number=1,Weight=30,Weeks=[new(1)]},new(){Number=2,Weight=30,Weeks=[new(1,WeekKind.Activities)]},new(){Number=3,Weight=40,Weeks=[new(1)]}]};
        public AcademicWorkspace? Load(){Loads++;return data;}
        public void Save(AcademicWorkspace value){Saves++;data=WorkspaceService.Clone(value);}
    }
}
