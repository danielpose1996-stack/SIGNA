using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Domain;
using Xunit;
namespace GestorDocente.Acceptance.Tests;

public class AttendanceConsolidatedUiTests
{
    [AvaloniaTheory]
    [InlineData(1450,900,1)] [InlineData(1100,740,1)] [InlineData(1100,800,1.25)]
    public async Task New_tab_shows_all_cuts_and_live_totals_with_accessible_scrolling(double width,double height,double scale)
    {
        var service=new WorkspaceService(new Store());var main=new MainViewModel(service);var window=new MainWindow{DataContext=main,Width=width,Height=height};
        if(scale!=1){var content=(Control)window.Content!;window.Content=null;window.Content=new LayoutTransformControl{LayoutTransform=new ScaleTransform(scale,scale),Child=content};}
        window.Show();
        try
        {
            var tab=window.GetVisualDescendants().OfType<Button>().Single(b=>b.CommandParameter as string=="Asistencia consolidada");
            Assert.True(tab.Command!.CanExecute(tab.CommandParameter));tab.Command.Execute(tab.CommandParameter);window.UpdateLayout();
            var view=window.FindControl<AttendanceConsolidatedView>("RetainedAttendanceConsolidated")!;var table=view.FindControl<DataGrid>("AttendanceConsolidatedTable")!;
            Assert.True(view.IsVisible);Assert.True(main.IsAttendanceConsolidated);Assert.False(main.ShowCutSelector);Assert.Equal(7,table.Columns.Count);
            Assert.Equal(2,table.FrozenColumnCount);Assert.True(double.IsNaN(table.RowHeight));Assert.True(table.Bounds.Height>250,$"Altura del listado: {table.Bounds.Height}");
            Assert.Equal(25,main.AttendanceConsolidated.Rows.Count);Assert.Equal(1,main.AttendanceConsolidated.Rows[0].Total);
            await service.SetAttendanceAsync(3,"s1",1,AttendanceState.Absent);Assert.Equal(2,main.AttendanceConsolidated.Rows[0].Total);Assert.Equal(1,main.SelectedCut);
            Capture(window,$"asistencia-consolidada-{width}-{scale}.png");table.ScrollIntoView(main.AttendanceConsolidated.Rows[0],table.Columns.Last());window.UpdateLayout();
            Assert.Contains(table.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text=="2");Capture(window,$"asistencia-total-{width}-{scale}.png");
            main.Page="Notas";Assert.False(view.IsVisible);main.Page="Asistencia consolidada";Assert.Same(view,window.FindControl<AttendanceConsolidatedView>("RetainedAttendanceConsolidated"));
        }
        finally{window.Close();}
    }
    [AvaloniaFact]
    public async Task Empty_filtered_and_deleted_rosters_have_no_stale_absence_totals()
    {
        var service=new WorkspaceService(new Store());var main=new MainViewModel(service){Page="Asistencia consolidada"};var window=new MainWindow{DataContext=main,Width=1100,Height=740};window.Show();
        try
        {
            main.AttendanceConsolidated.Search="No existe";window.UpdateLayout();Assert.True(main.AttendanceConsolidated.IsEmpty);Capture(window,"asistencia-busqueda-vacia.png");
            main.AttendanceConsolidated.Search="";await service.DeleteStudentListAsync(service.PreviewStudentListDeletion(),true);
            Assert.Empty(main.AttendanceConsolidated.Rows);Assert.True(main.AttendanceConsolidated.IsEmpty);Assert.Equal(4,main.AttendanceConsolidated.Cuts.Count);
            window.UpdateLayout();Capture(window,"asistencia-sin-estudiantes.png");
        }
        finally{window.Close();}
    }
    private static void Capture(Window window,string name)
    {
        if(Environment.GetEnvironmentVariable("SIGNA_049_PREVIEW") is not {} folder)return;
        Directory.CreateDirectory(folder);window.UpdateLayout();using var image=window.CaptureRenderedFrame();image?.Save(Path.Combine(folder,name));
    }
    private sealed class Store:IWorkspaceStore
    {
        private AcademicWorkspace data=new(){CourseName="Materia ficticia de seguimiento",Students=Enumerable.Range(1,25).Select(i=>new Student($"s{i}",$"FIC-{i}",$"Estudiante ficticio {i:00} con apellidos de demostración")).ToList(),Cuts=[
            new(){Number=1,Name="Primer período de investigación académica",Weight=30,Weeks=[new(1)],Attendance=[new("s1",1,AttendanceState.Absent)]},
            new(){Number=2,Name="Segundo período",Weight=30,Weeks=[new(1,WeekKind.Activities)]},new(){Number=3,Weight=40,Weeks=[new(1)]},new(){Number=4,Name="Evaluación final adicional",Weight=0}]};
        public AcademicWorkspace? Load()=>data;
        public void Save(AcademicWorkspace value)=>data=WorkspaceService.Clone(value);
    }
}
