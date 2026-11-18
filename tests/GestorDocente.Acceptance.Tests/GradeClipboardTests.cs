using System.Text.Json;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Headless.XUnit;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class GradeClipboardTests
{
    [AvaloniaFact]
    public async Task Copy_places_only_numbers_in_visible_order_with_pending_lines_and_filters()
    {
        var store=new Store();var service=new WorkspaceService(store);var model=new MainViewModel(service);
        var window=new MainWindow{DataContext=model};window.Show();
        try
        {
            await model.OpenCourseAsync(null);
            var before=JsonSerializer.Serialize(service.Current);var writes=store.Writes;
            model.SortStudentsCommand.Execute(null);
            await window.Clipboard!.SetTextAsync("anterior");
            model.CopyGradesCommand.Execute("direct");
            Assert.Equal(string.Join(Environment.NewLine,"4,50","","0,00"),await window.Clipboard!.TryGetTextAsync());
            Assert.Empty(window.OwnedWindows);
            model.SortStudentsCommand.Execute(null);
            model.CopyGradesCommand.Execute("direct");
            Assert.Equal(string.Join(Environment.NewLine,"0,00","","4,50"),await window.Clipboard!.TryGetTextAsync());
            model.SearchInput="Ana";model.FlushSearch();
            model.CopyGradesCommand.Execute("direct");
            Assert.Equal("4,50",await window.Clipboard!.TryGetTextAsync());
            model.GradeRows[0].Cells["direct"].Input="4";
            model.CopyGradesCommand.Execute("direct");
            Assert.Equal("4,00",await window.Clipboard!.TryGetTextAsync());
            model.GradeRows[0].Cells["direct"].Input="texto";
            model.CopyGradesCommand.Execute("direct");
            Assert.Equal("4,00",await window.Clipboard!.TryGetTextAsync());
            Assert.Contains("Revisa la nota",model.Message);
            model.SearchInput="Sin coincidencias";model.FlushSearch();
            Assert.False(model.CopyGradesCommand.CanExecute("direct"));
            Assert.Equal(writes,store.Writes);Assert.Equal(before,JsonSerializer.Serialize(service.Current));
        }
        finally
        {
            foreach(var dialog in window.OwnedWindows.ToArray())dialog.Close();
            window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Locked_cut_allows_readonly_copy_of_subactivity_and_attendance_column()
    {
        var store=new Store();var service=new WorkspaceService(store);var model=new MainViewModel(service);
        var window=new MainWindow{DataContext=model};window.Show();
        try
        {
            await model.OpenCourseAsync(null);
            await service.SetCutLockedAsync(1,true);var writes=store.Writes;
            var before=JsonSerializer.Serialize(service.Current);
            model.SortStudentsCommand.Execute(null);
            Assert.True(model.CopyGradesCommand.CanExecute("sub"));
            model.CopyGradesCommand.Execute("sub");
            Assert.Equal(string.Join(Environment.NewLine,"3,70","",""),await window.Clipboard!.TryGetTextAsync());
            model.Page="Asistencias";window.UpdateLayout();
            var attendance=window.FindControl<AttendanceView>("RetainedAttendance")!;
            var column=attendance.FindControl<DataGrid>("AttendanceTable")!.Columns.Last();
            var header=Assert.IsType<StackPanel>(column.Header);
            var action=Assert.IsType<Button>(header.Children.Last());
            var item=Assert.Single(Assert.IsType<MenuFlyout>(action.Flyout).Items.Cast<MenuItem>());
            Assert.Equal("Copiar notas",item.Header);Assert.True(item.Command!.CanExecute(item.CommandParameter));
            item.Command.Execute(item.CommandParameter);
            Assert.Equal(string.Join(Environment.NewLine,"2,00","","2,50"),await window.Clipboard!.TryGetTextAsync());
            Assert.Empty(window.OwnedWindows);
            Assert.Equal(writes,store.Writes);Assert.Equal(before,JsonSerializer.Serialize(service.Current));
        }
        finally
        {
            foreach(var dialog in window.OwnedWindows.ToArray())dialog.Close();
            window.Close();
        }
    }

    private sealed class Store:IWorkspaceStore
    {
        public int Writes;
        public AcademicWorkspace? Load()=>new()
        {
            Students=[new("c","01","Cecilia"),new("a","02","Ana"),new("b","03","Bruno")],
            Cuts=[new(){Number=1,Weight=30,
                Groups=[new(){Id="direct",Name="Actividad",Weight=60},new(){Id="group",Name="Talleres",Weight=40,Mode=EvaluationMode.SubActivities,Activities=[new(){Id="sub",Name="Taller 1"}]}],
                Grades=[new("c","direct",0),new("a","direct",4.5m),new("a","sub",3.7m)],
                Weeks=[new(1),new(2)],AttendanceGrading=new(true,5,0,4,2),
                Attendance=[new("c",1,AttendanceState.Present),new("c",2,AttendanceState.Absent),new("a",1,AttendanceState.Late)]},
                new(){Number=2,Weight=30},new(){Number=3,Weight=40}]
        };
        public void Save(AcademicWorkspace data)=>Writes++;
    }
}
