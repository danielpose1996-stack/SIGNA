using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Domain;
using Xunit;
namespace GestorDocente.Acceptance.Tests;

public class BulkGradesAndSortingTests
{
    [AvaloniaFact]
    public void Name_header_toggles_both_tables_visually_without_queries_or_losing_drafts()
    {
        var store=new Store();var vm=new MainViewModel(new(store));var notes=new NotesView{DataContext=vm};var attendance=new AttendanceView{DataContext=vm};
        var column=notes.FindControl<DataGrid>("GradesTable")!.Columns[4];
        var selected=vm.SelectedStudent!;var draft=vm.GradeRows[0].Cells["t1"];draft.Input="4,99";
        var before=JsonSerializer.Serialize(vm.Service.Current);var loads=store.Loads;
        vm.SortStudentsCommand.Execute(null);var ascending=vm.GradeRows.Select(r=>r.FullName).ToArray();
        Assert.Equal(ascending.OrderBy(n=>n,StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("es-CO"),true)),ascending);
        Assert.Equal(vm.GradeRows.Select(r=>r.Student.Id),vm.AttendanceRows.Select(r=>r.Student.Id));
        vm.SortStudentsCommand.Execute(null);Assert.Equal(ascending.Reverse(),vm.GradeRows.Select(r=>r.FullName));
        Assert.Same(selected,vm.SelectedStudent);Assert.Same(draft,vm.GradeRows.Single(r=>r.Student.Id=="s1").Cells["t1"]);Assert.Equal("4,99",draft.Input);
        Assert.Same(column,notes.FindControl<DataGrid>("GradesTable")!.Columns[4]);Assert.Equal(loads,store.Loads);Assert.Equal(0,store.Saves);Assert.Equal(before,JsonSerializer.Serialize(vm.Service.Current));
        vm.Search="Ana";Assert.Equal("s1",Assert.Single(vm.GradeRows).Student.Id);vm.Search="";Assert.Equal(ascending.Reverse(),vm.GradeRows.Select(r=>r.FullName));
    }
    [AvaloniaFact]
    public async Task Bulk_modal_previews_filtered_sorted_order_requires_replacement_and_updates_consolidated()
    {
        var store=new Store();var vm=new MainViewModel(new(store));vm.SortStudentsCommand.Execute(null);vm.SortStudentsCommand.Execute(null);
        var ids=vm.GradeRows.Select(r=>r.Student.Id).ToArray();var bulk=new GradePasteViewModel(vm,"t1");
        bulk.Input=string.Join('\n',Enumerable.Repeat("3,75",ids.Length));Assert.False(bulk.CanSave);bulk.Preview();
        Assert.Equal(ids,bulk.Rows.Select(r=>r.StudentId));Assert.True(bulk.RequiresReplacement);Assert.False(bulk.CanSave);
        bulk.ConfirmReplacement=true;Assert.True(bulk.CanSave);Assert.True(await bulk.SaveAsync());Assert.True(bulk.HasSaved);Assert.Equal(1,store.Saves);
        Assert.All(vm.GradeRows,r=>Assert.Equal("3,75",r.Cells["t1"].Input));Assert.Equal(ids,vm.GradeRows.Select(r=>r.Student.Id));
        Assert.Equal(GradeCalculator.Cut(vm.Service.Cut(1),"s1"),vm.Consolidated.Rows.Single(r=>r.FullName==vm.GradeRows.Single(g=>g.Student.Id=="s1").FullName).CutValues[1]);
        vm.Search="Ana";var filtered=new GradePasteViewModel(vm,"t1"){Input="4"};filtered.Preview();Assert.Single(filtered.Rows);
        filtered.ConfirmReplacement=true;filtered.Input="5";Assert.False(filtered.CanSave);Assert.Empty(filtered.Rows);
        filtered.Preview();filtered.ConfirmReplacement=true;vm.Search="";Assert.False(await filtered.SaveAsync());Assert.Contains("orden",filtered.Error,StringComparison.OrdinalIgnoreCase);Assert.Equal(1,store.Saves);
    }
    [AvaloniaFact]
    public async Task Optional_attendance_grade_updates_and_does_not_change_existing_statistics()
    {
        var store=new Store();var service=new WorkspaceService(store);var vm=new MainViewModel(service);
        var attendance=new AttendanceView{DataContext=vm};var table=attendance.FindControl<DataGrid>("AttendanceTable")!;
        var original=vm.GroupAttendancePercentage;var notes=JsonSerializer.Serialize(service.Current.Cuts.Select(c=>c.Grades));
        var draft=vm.GradeRows[0].Cells["t1"];draft.Input="4,99";
        Assert.False(vm.AttendanceGradingEnabled);var editor=new AttendanceGradingViewModel(service,1);Assert.False(editor.CanSave);
        var values=new[]{"5","0","4","1"};for(var i=0;i<4;i++)editor.StateGrades[i].Input=values[i];Assert.True(editor.CanSave);Assert.True(await editor.SaveAsync());
        Assert.True(vm.AttendanceGradingEnabled);Assert.Contains(table.Columns,c=>c.Header is TextBlock{Text:"Nota de asistencia"});
        Assert.Same(draft,vm.GradeRows[0].Cells["t1"]);Assert.Equal("4,99",draft.Input);
        Assert.Equal(original,vm.GroupAttendancePercentage);Assert.Equal(notes,JsonSerializer.Serialize(service.Current.Cuts.Select(c=>c.Grades)));
        await service.SetAttendanceAsync(1,"s1",1,AttendanceState.Absent);Assert.Equal(10m/3,vm.AttendanceRows.Single(r=>r.Student.Id=="s1").AttendanceGradeValue);
        await service.ConfigureAttendanceGradingAsync(1,new(false,5,0,4,1));Assert.False(vm.AttendanceGradingEnabled);Assert.DoesNotContain(table.Columns,c=>c.Header is TextBlock{Text:"Nota de asistencia"});
        Assert.Equal(4m,service.Cut(1).AttendanceGrading!.Justified);vm.SelectedCut=2;Assert.False(vm.AttendanceGradingEnabled);
    }
    private sealed class Store:IWorkspaceStore
    {
        public int Loads,Saves;public AcademicWorkspace? Load(){Loads++;return DemoWorkspaceFactory.Create();}public void Save(AcademicWorkspace data)=>Saves++;
    }
}
