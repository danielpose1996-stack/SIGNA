using Avalonia.Controls;
using Avalonia.Headless.XUnit;
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
public class WeekEditingAndProfileTests
{
    [AvaloniaFact]
    public async Task Week_editor_can_cancel_edit_and_retry_without_changing_attendance()
    {
        var store=new Store();var service=new WorkspaceService(store);await service.SetAttendanceAsync(1,"s1",2,AttendanceState.Absent);var main=new MainViewModel(service);
        var vm=new WeeksViewModel(service,1);var window=new WeeksWindow{DataContext=vm};window.Show();
        try
        {
            vm.SelectedWeek=vm.Weeks[1];vm.BeginEdit();Assert.True(vm.IsEditing);Assert.False(window.FindControl<CheckBox>("MarkAllPresentCheckBox")!.IsVisible);
            vm.SelectedDate=new DateTime(2026,10,14);vm.SelectedKind=vm.Kinds[1];vm.CancelEdit();Assert.False(vm.IsEditing);Assert.Null(service.Cut(1).Weeks[1].Date);
            vm.BeginEdit();vm.SelectedDate=new DateTime(2026,10,14);vm.SelectedKind=vm.Kinds[1];window.UpdateLayout();Capture(window,"editar-semana.png");store.Fail=true;
            await vm.SaveAsync();Assert.True(vm.IsEditing);Assert.NotEmpty(vm.Error);Assert.NotNull(vm.SelectedDate);
            store.Fail=false;await vm.SaveAsync();Assert.False(vm.IsEditing);Assert.Equal(WeekKind.Activities,service.Cut(1).Weeks[1].Kind);
            Assert.Equal(AttendanceState.Absent,service.Cut(1).Attendance.Single(a=>a.WeekNumber==2 && a.StudentId=="s1").State);
            await service.RemoveWeekAsync(1,1);
            Assert.StartsWith("Semana 1",main.Weeks[0].Label);Assert.StartsWith("Semana 1",main.AttendanceRows[0].WeeklyDetails[0].Title);
            var attendanceWindow=new MainWindow{DataContext=main,Width=1100,Height=700};attendanceWindow.Show();main.Page="Asistencias";attendanceWindow.UpdateLayout();Capture(attendanceWindow,"semanas-renumeradas.png");attendanceWindow.Close();
            while(service.Cut(1).Weeks.Count<9)await service.AddWeekAsync(1);
            var full=new WeeksViewModel(service,1);Assert.False(full.CanAdd);full.BeginEdit();Assert.True(full.CanSave);
            full.SelectedDate=null;await full.SaveAsync();Assert.Empty(full.Error);Assert.Equal(9,full.Weeks.Count);
        }
        finally{window.Close();}
    }

    [AvaloniaFact]
    public async Task Profile_rgb_and_result_switch_apply_to_retained_controls_only_after_save()
    {
        var preferences=new InterfacePreferencesService(new MemoryInterfacePreferencesStore());var main=new MainViewModel(new WorkspaceService(new Store()),appearance:preferences);
        var window=new MainWindow{DataContext=main};window.Show();main.NavigateCommand.Execute("Notas");window.UpdateLayout();
        try
        {
            var row=main.GradeRows[0];var before=System.Text.Json.JsonSerializer.Serialize(main.Service.Current);
            var vm=main.Appearance;var dialog=new AppearanceWindow{DataContext=vm};dialog.Show(window);dialog.UpdateLayout();
            try
            {
                Assert.NotNull(dialog.FindControl<TextBox>("TeacherNameTextBox"));Assert.NotNull(dialog.FindControl<CheckBox>("ColorGradesCheckBox"));
                dialog.FindControl<TextBox>("TeacherNameTextBox")!.Text="  Docente de prueba  ";
                dialog.FindControl<CheckBox>("CustomColorCheckBox")!.IsChecked=true;
                var inputs=dialog.GetVisualDescendants().OfType<NumericUpDown>().ToArray();Assert.Equal(3,inputs.Length);
                inputs[0].Value=255;inputs[1].Value=240;inputs[2].Value=20;
                Assert.Equal(255,vm.Red);Assert.Equal(240,vm.Green);Assert.Equal(20,vm.Blue);vm.ColorGrades=false;
                Assert.NotEqual("Docente de prueba",vm.CurrentTeacherName);
                Assert.True(await vm.SaveAsync());window.UpdateLayout();
                Assert.Equal("Docente de prueba",window.FindControl<TextBlock>("TeacherProfileName")!.Text);
                Assert.Equal("#FFF014",preferences.Current.CustomAccent);Assert.False(vm.CurrentColorGrades);
                Assert.Equal(Color.Parse("#FFF014"),((ISolidColorBrush)ThemePalettes.Brushes(preferences.Current)["Primary"]).Color);
                Assert.Same(row,main.GradeRows[0]);Assert.Equal(before,System.Text.Json.JsonSerializer.Serialize(main.Service.Current));
                var cells=window.GetVisualDescendants().OfType<ResultGrade>().ToArray();Assert.NotEmpty(cells);
                Assert.All(cells,c=>Assert.False(c.Colorize));
                vm.ColorGrades=true;Assert.True(await vm.SaveAsync());window.UpdateLayout();Assert.All(cells,c=>Assert.True(c.Colorize));
                vm.TeacherName="Sin guardar";vm.Red=0;vm.ResetDraft();Assert.Equal("Docente de prueba",vm.TeacherName);Assert.Equal(255,vm.Red);
                Capture(dialog,"apariencia-rgb.png");
                dialog.Width=500;dialog.Height=500;dialog.UpdateLayout();Capture(dialog,"apariencia-rgb-pequena.png");
            }
            finally{dialog.Close();}
            var grades=window.FindControl<NotesView>("RetainedNotes")!.FindControl<DataGrid>("GradesTable")!;
            grades.ScrollIntoView(main.GradeRows[0],grades.Columns[^2]);window.UpdateLayout();
            Capture(window,"notas-colores.png");main.Page="Nota consolidada";window.UpdateLayout();Capture(window,"consolidada-colores.png");
        }
        finally{window.Close();ThemePalettes.Apply(Avalonia.Application.Current!,InterfacePalette.Blue);}
    }

    [AvaloniaFact]
    public async Task Result_cell_updates_after_a_grade_change_without_coloring_activity_cells()
    {
        var data=new AcademicWorkspace{Students=[new("s1","F01","Estudiante ficticio")],Cuts=AcademicRules.DefaultCuts()};
        foreach(var cut in data.Cuts)cut.Groups=[new(){Id="p",Name="Evaluación",Weight=100}];
        var service=new WorkspaceService(new FixtureStore(data));var main=new MainViewModel(service);var window=new MainWindow{DataContext=main};window.Show();main.Page="Notas";window.UpdateLayout();
        try
        {
            await service.SetGradeAsync(1,"s1","p",4.6m);window.UpdateLayout();
            var table=window.FindControl<NotesView>("RetainedNotes")!.FindControl<DataGrid>("GradesTable")!;
            var cell=Assert.Single(table.GetVisualDescendants().OfType<ResultGrade>());
            Assert.Equal(4.6m,cell.Value);Assert.Equal(Color.Parse("#E5F0FF"),((ISolidColorBrush)cell.Background!).Color);
            Assert.NotEmpty(table.GetVisualDescendants().OfType<GradeEditor>());
            Assert.Equal(4.6m,main.Consolidated.Rows[0].CutValues[1]);
            await service.SetGradeAsync(1,"s1","p",2.99m);window.UpdateLayout();
            cell=Assert.Single(table.GetVisualDescendants().OfType<ResultGrade>());Assert.Equal(Color.Parse("#FDEBEC"),((ISolidColorBrush)cell.Background!).Color);
            Assert.Equal(2.99m,main.Consolidated.Rows[0].CutValues[1]);
        }
        finally{window.Close();}
    }

    [Theory]
    [InlineData(0,"#FDEBEC")][InlineData(2.9,"#FDEBEC")][InlineData(2.99,"#FDEBEC")]
    [InlineData(3,"#FFF3D8")][InlineData(3.99,"#FFF3D8")]
    [InlineData(4,"#E5F5EB")][InlineData(4.5,"#E5F5EB")][InlineData(4.59,"#E5F5EB")]
    [InlineData(4.6,"#E5F0FF")][InlineData(5,"#E5F0FF")]
    public void Result_colors_follow_exact_boundaries_without_rounding(double grade,string expected)
    {
        Assert.Equal(Color.Parse(expected),ResultGrade.ColorFor((decimal)grade,true));
        Assert.Equal(Colors.Transparent,ResultGrade.ColorFor((decimal)grade,false));Assert.Equal(Colors.Transparent,ResultGrade.ColorFor(null,true));
    }

    [Fact]
    public void All_rgb_extremes_keep_legible_primary_button_and_accent_text()
    {
        foreach(var accent in new[]{"#FFFFFF","#FFFF00","#000000","#00FF00","#FF0000","#0000FF"})
        {
            var brushes=ThemePalettes.Brushes(new InterfacePreferences(CustomAccent:accent));
            Assert.True(Contrast(((ISolidColorBrush)brushes["Primary"]).Color,((ISolidColorBrush)brushes["OnPrimary"]).Color)>=4.5);
            Assert.True(Contrast(((ISolidColorBrush)brushes["AccentText"]).Color,Colors.White)>=4.5);
        }
    }
    private static double Contrast(Color a,Color b)
    {
        static double L(Color c){static double C(byte b){var x=b/255d;return x<=0.04045 ? x/12.92 : Math.Pow((x+0.055)/1.055,2.4);}return .2126*C(c.R)+.7152*C(c.G)+.0722*C(c.B);}
        return (Math.Max(L(a),L(b))+.05)/(Math.Min(L(a),L(b))+.05);
    }
    private static void Capture(Window window,string name)
    {
        if(Environment.GetEnvironmentVariable("SIGNA_FEATURE_PREVIEW") is not {} directory)return;
        Directory.CreateDirectory(directory);using var bitmap=Avalonia.Headless.HeadlessWindowExtensions.CaptureRenderedFrame(window);bitmap?.Save(Path.Combine(directory,name));
    }
    private sealed class Store:IWorkspaceStore
    {
        public bool Fail;
        private AcademicWorkspace data=DemoWorkspaceFactory.Create();
        public AcademicWorkspace? Load()=>WorkspaceService.Clone(data);
        public void Save(AcademicWorkspace value){if(Fail)throw new IOException("Fallo ficticio");data=WorkspaceService.Clone(value);}
    }
    private sealed class FixtureStore(AcademicWorkspace data):IWorkspaceStore
    {
        public AcademicWorkspace? Load()=>WorkspaceService.Clone(data);
        public void Save(AcademicWorkspace value)=>data=WorkspaceService.Clone(value);
    }
}
