using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Domain;
using Xunit;
namespace GestorDocente.Acceptance.Tests;

public class BulkGradesAndSortingTests
{
    [AvaloniaFact]
    public async Task Assignment_includes_hidden_students_and_requires_confirmation_for_the_current_value()
    {
        var store = new Store();
        var vm = new MainViewModel(new(store)) { Search = "Ana" };
        Assert.Single(vm.GradeRows);
        var assignment = new GradePasteViewModel(vm, "t1", assignAll: true);

        assignment.Input = "5,01";
        assignment.ConfirmReplacement = true;
        Assert.False(await assignment.SaveAsync());
        assignment.Input = "0";
        Assert.False(assignment.ConfirmReplacement);
        Assert.False(await assignment.SaveAsync());
        Assert.Equal(0, store.Saves);

        assignment.ConfirmReplacement = true;
        Assert.True(await assignment.SaveAsync());
        Assert.Equal(1, store.Saves);
        var cut = vm.Service.Cut(1);
        var index = new AcademicCutReadIndex(cut);
        Assert.All(vm.Service.Current.Students, student => Assert.Equal(0m, index.Grade(student.Id, "t1")));
        Assert.Equal(GradeCalculator.Cut(cut, "s1"), vm.Consolidated.Rows.Single(r => r.FullName == vm.GradeRows[0].FullName).CutValues[1]);
    }

    [AvaloniaFact]
    public async Task Assignment_preserves_pending_edits_and_refuses_an_outdated_snapshot()
    {
        var store = new Store();
        var vm = new MainViewModel(new(store));
        var assignment = new GradePasteViewModel(vm, "t1", assignAll: true) { Input = "4.5", ConfirmReplacement = true };
        var cell = vm.GradeRows[0].Cells["t1"];
        var original = cell.Input;
        cell.Input = "incompleta";
        Assert.False(await assignment.SaveAsync());
        Assert.Equal("incompleta", cell.Input);
        Assert.Equal(0, store.Saves);
        cell.Input = original;

        await vm.Service.SetGradeAsync(1, "s1", "t1", 2.1m);
        Assert.False(await assignment.SaveAsync());
        Assert.Equal(1, store.Saves);
        Assert.Equal(2.1m, new AcademicCutReadIndex(vm.Service.Cut(1)).Grade("s1", "t1"));
    }

    [AvaloniaFact]
    public void Only_editable_columns_have_a_gear_with_both_grade_actions()
    {
        var vm = new MainViewModel(new(new Store()));
        var view = new NotesView { DataContext = vm };
        var columns = view.FindControl<DataGrid>("GradesTable")!.Columns;
        var editable = vm.GradeRows[0].Cells.Keys.Order().ToArray();
        var buttons = columns.Select(c => c.Header).OfType<StackPanel>()
            .SelectMany(header => header.Children.OfType<Button>()).ToArray();
        Assert.Equal(editable, buttons.Select(button =>
        {
            Assert.IsType<GestorDocente.Desktop.Controls.VectorIcon>(button.Content);
            var items = Assert.IsType<MenuFlyout>(button.Flyout).Items.Cast<MenuItem>().ToArray();
            Assert.Equal(new[] { "Pegar notas", "Asignar nota a todos" }, items.Select(item => item.Header as string));
            Assert.Same(vm.PasteGradesCommand, items[0].Command);
            Assert.Same(vm.AssignGradeToAllCommand, items[1].Command);
            Assert.Equal(items[0].CommandParameter, items[1].CommandParameter);
            return Assert.IsType<string>(items[1].CommandParameter);
        }).Order());
        Assert.Contains(columns, c => c.Header is TextBlock text && text.Text!.StartsWith("Promedio"));
        Assert.Contains(columns, c => c.Header is TextBlock { Text: "Nota del\ncorte" });
    }

    [AvaloniaTheory]
    [InlineData(540, 1)]
    [InlineData(400, 1.25)]
    public async Task Assignment_dialog_opens_from_the_command_and_can_be_cancelled(double width, double scale)
    {
        var store = new Store();
        var vm = new MainViewModel(new(store));
        var main = new MainWindow { DataContext = vm, Width = 1250, Height = 800 };
        main.Show();
        try
        {
            vm.AssignGradeToAllCommand.Execute("t1");
            var dialog = Assert.Single(main.OwnedWindows.OfType<GradePasteWindow>());
            dialog.Width = width;
            if (scale != 1)
            {
                var content = Assert.IsAssignableFrom<Control>(dialog.Content);
                dialog.Content = null;
                dialog.Content = new LayoutTransformControl { Child = content, LayoutTransform = new Avalonia.Media.ScaleTransform(scale, scale) };
            }
            dialog.UpdateLayout();
            var input = dialog.FindControl<TextBox>("UniformGradeInput")!;
            var save = dialog.FindControl<Button>("SavePastedGrades")!;
            Assert.True(input.IsEffectivelyVisible);
            Assert.False(dialog.FindControl<DataGrid>("GradePastePreviewTable")!.IsEffectivelyVisible);
            Assert.False(save.IsEnabled);
            input.Text = "4,5";
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            var confirmation = dialog.FindControl<CheckBox>("ConfirmUniformGrade")!;
            confirmation.IsChecked = true;
            Avalonia.Threading.Dispatcher.UIThread.RunJobs();
            Assert.True(save.IsEnabled);
            var end = save.TranslatePoint(new Avalonia.Point(save.Bounds.Width, save.Bounds.Height), dialog)!.Value;
            Assert.InRange(end.X, 0, dialog.ClientSize.Width);
            Assert.InRange(end.Y, 0, dialog.ClientSize.Height);
            if (Environment.GetEnvironmentVariable("SIGNA_ASSIGNMENT_PREVIEW") is { Length: > 0 } folder)
            {
                Directory.CreateDirectory(folder);
                using var image = dialog.CaptureRenderedFrame();
                image?.Save(Path.Combine(folder, $"asignar-nota-{width}.png"));
            }
            dialog.Close();
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { });
            Assert.Equal(0, store.Saves);
            Assert.False(vm.FileBusy);
        }
        finally
        {
            foreach (var dialog in main.OwnedWindows.ToArray()) dialog.Close();
            main.Close();
        }
    }

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
