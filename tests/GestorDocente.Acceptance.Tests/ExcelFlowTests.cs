using Avalonia.Controls;
using System.Collections.Specialized;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Infrastructure;
using Xunit;
namespace GestorDocente.Acceptance.Tests;
public class ExcelFlowTests
{
    [AvaloniaFact]
    public void Import_shows_preview_automatically_and_cancel_does_not_change_workspace()
    {
        var service=new WorkspaceService(new MemoryStore());var count=service.Current.Students.Count;
        var vm=new StudentImportViewModel(service,Book(),"Listado.xlsx");var dialog=new StudentImportWindow{DataContext=vm};dialog.Show();dialog.UpdateLayout();
        Assert.Single(vm.Rows);Assert.True(vm.CanImport);Assert.Equal("Ana Ficticia",vm.Rows[0].Name);
        Assert.False(dialog.FindControl<Expander>("ColumnSettings")!.IsExpanded);
        dialog.Close();Assert.Equal(count,service.Current.Students.Count);
        vm.DocumentColumn=vm.Columns[1];Assert.Empty(vm.Rows);Assert.False(vm.HasPreview);
    }
    [AvaloniaFact]
    public void Confirming_preview_refreshes_both_tables_and_preserves_existing_grade()
    {
        var service=new WorkspaceService(new MemoryStore());var main=new MainViewModel(service,workbookReader:new XlsxStudentReader(),reportExporter:new XlsxReportExporter());
        var vm=new StudentImportViewModel(service,Book(),"Listado.xlsx");Assert.True(vm.Save());
        Assert.Equal(11,main.GradeRows.Count);Assert.Equal(11,main.AttendanceRows.Count);Assert.Equal(1,vm.ImportedCount);
        int requested=0;main.ExportRequested+=()=>requested++;Assert.True(main.ExportReportCommand.CanExecute(null));main.ExportReportCommand.Execute(null);Assert.Equal(1,requested);
        main.FileBusy=true;Assert.False(main.ImportStudentsCommand.CanExecute(null));Assert.False(main.CutCommand.CanExecute(2));main.FileBusy=false;Assert.True(main.ImportStudentsCommand.CanExecute(null));
    }
    [AvaloniaFact]
    public void Headerless_batch_imports_all_students_and_updates_both_tables_without_manual_selection()
    {
        var service=new WorkspaceService(new MemoryStore());var main=new MainViewModel(service);
        var rows=Enumerable.Range(1,40).Select(i=>new WorkbookRow(i,new Dictionary<int,WorkbookCell>{{0,new($"Estudiante Ficticio {Letters(i)}")},{1,new($"0099{i:0000}")},{2,new("ignorado@example.test")}})).ToList();
        var vm=new StudentImportViewModel(service,new([new("Portada",[]),new("Listado",rows)]),"Listado.xlsx");
        Assert.Equal("Listado",vm.SelectedSheet!.Name);Assert.Equal(40,vm.Rows.Count);Assert.Equal(0,vm.HeaderRow);Assert.True(vm.CanImport);
        Assert.True(vm.Save());Assert.Equal(40,vm.ImportedCount);Assert.Equal(50,main.GradeRows.Count);Assert.Equal(50,main.AttendanceRows.Count);
        vm=new StudentImportViewModel(service,new([new("Listado",rows)]),"Listado.xlsx");Assert.False(vm.CanImport);Assert.False(vm.Save());Assert.Equal(50,service.Current.Students.Count);
    }
    [AvaloniaFact]
    public void Rebuilding_preview_notifies_the_table_once_for_the_whole_batch()
    {
        var service=new WorkspaceService(new MemoryStore());
        var rows=new[]{new WorkbookRow(1,new Dictionary<int,WorkbookCell>{{0,new("Nombre completo")},{1,new("Documento")}})}
            .Concat(Enumerable.Range(1,300).Select(i=>new WorkbookRow(i+1,new Dictionary<int,WorkbookCell>
            {{0,new($"Estudiante Ficticio {i}")},{1,new($"F-{i:0000}")}}))).ToList();
        var vm=new StudentImportViewModel(service,new([new("Listado",rows)]),"Listado.xlsx");
        Assert.Equal(300,vm.Rows.Count);
        var changes=0;
        vm.Rows.CollectionChanged+=(_,e)=>{Assert.Equal(NotifyCollectionChangedAction.Reset,e.Action);changes++;};
        vm.Preview();
        Assert.Equal(1,changes);
        Assert.Equal(300,vm.Rows.Count);
    }
    private static string Letters(int value)=>$"{(char)('A'+value/26)}{(char)('A'+value%26)}";
    [AvaloniaFact]
    public void Correcting_columns_rebuilds_entire_preview_without_extra_button()
    {
        var service=new WorkspaceService(new MemoryStore());
        var book=new StudentWorkbook([new("Listado",[new(1,new Dictionary<int,WorkbookCell>{{0,new("Campo A")},{1,new("Campo B")}}),new(2,new Dictionary<int,WorkbookCell>{{0,new("Ana Prueba")},{1,new("00012345")}})])]);
        var vm=new StudentImportViewModel(service,book,"Listado.xlsx");
        // Ambiguous labels can be corrected once for the whole sheet.
        vm.FirstDataRow=2;vm.DocumentColumn=null;Assert.False(vm.CanImport);
        vm.DocumentColumn=vm.Columns[1];vm.Columns[0].IsName=true;
        Assert.Single(vm.Rows);Assert.True(vm.CanImport);Assert.Equal("Ana Prueba",vm.Rows[0].Name);
        vm.Columns[0].IsName=false;Assert.Empty(vm.Rows);Assert.False(vm.CanImport);
    }
    [AvaloniaFact]
    public void Two_line_headers_fit_at_application_scale_125_percent()
    {
        var previous=Environment.GetEnvironmentVariable("GESTOR_DOCENTE_UI_SCALE");Environment.SetEnvironmentVariable("GESTOR_DOCENTE_UI_SCALE","1.25");
        try
        {
            var vm=new MainViewModel(new WorkspaceService(new MemoryStore()));var window=new MainWindow{DataContext=vm,Width=1500,Height=950};window.Show();vm.OpenCourseCommand.Execute(null);window.UpdateLayout();
            var view=window.FindControl<NotesView>("RetainedNotes")!;var table=view.FindControl<DataGrid>("GradesTable")!;
            var final=(TextBlock)table.Columns.First(c=>c.Header is TextBlock t && t.Text=="Nota del\ncorte").Header!;
            Assert.True(final.DesiredSize.Height<=table.ColumnHeaderHeight-8);Assert.Equal(Avalonia.Media.TextWrapping.NoWrap,final.TextWrapping);
            Assert.All(table.Columns.Select(c=>c.Header).OfType<TextBlock>(),h=>Assert.True(h.Text!.Split('\n').Length<=2));
            vm.Page="Asistencias";window.UpdateLayout();var attendance=window.FindControl<AttendanceView>("RetainedAttendance")!.FindControl<DataGrid>("AttendanceTable")!;
            Assert.All(attendance.Columns.Select(c=>c.Header).OfType<TextBlock>(),h=>Assert.True(h.DesiredSize.Height<=attendance.ColumnHeaderHeight-8));window.Close();
        }
        finally{Environment.SetEnvironmentVariable("GESTOR_DOCENTE_UI_SCALE",previous);}
    }
    private static StudentWorkbook Book()=>new([new("Listado",[new(3,new Dictionary<int,WorkbookCell>{{0,new("Documento")},{1,new("Nombres")},{2,new("Apellidos")},{3,new("Correo")}}),new(4,new Dictionary<int,WorkbookCell>{{0,new("000123")},{1,new("Ana")},{2,new("Ficticia")},{3,new("ignorado")}})])]);
    private sealed class MemoryStore:IWorkspaceStore{private AcademicWorkspace data=DemoWorkspaceFactory.Create();public AcademicWorkspace? Load()=>data;public void Save(AcademicWorkspace value)=>data=WorkspaceService.Clone(value);}
}
