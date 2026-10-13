using Avalonia;
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

public class AttendancePresentationTests
{
    [AvaloniaFact]
    public void Persisted_attendance_refresh_preserves_selected_student_with_the_table_attached()
    {
        var service=new WorkspaceService(new MemoryStore(DemoWorkspaceFactory.Create()));
        var vm=new MainViewModel(service){Page="Asistencias",DetailVisible=true};
        var window=new MainWindow{DataContext=vm};window.Show();window.UpdateLayout();
        try
        {
            var selected=vm.SelectedStudent!.Student.Id;
            service.SetAttendance(1,selected,1,AttendanceState.Absent);window.UpdateLayout();
            Assert.Equal(selected,vm.SelectedStudent!.Student.Id);
            Assert.Equal("66,67 %",vm.SelectedStudent.Percentage);Assert.Equal("Regular",vm.SelectedStudent.Status);
            var table=window.FindControl<AttendanceView>("RetainedAttendance")!.FindControl<DataGrid>("AttendanceTable")!;
            Assert.Same(vm.SelectedStudent,table.SelectedItem);
        }
        finally{window.Close();}
    }
    [AvaloniaFact]
    public void Percentages_use_global_records_and_update_without_filtering_the_group()
    {
        var data=DemoWorkspaceFactory.Create();
        data.Students=[new("one","F-01","Estudiante ficticio uno"),new("two","F-02","Estudiante ficticio dos"),new("three","F-03","Estudiante ficticio tres")];
        foreach(var cut in data.Cuts){cut.Grades.Clear();cut.Attendance.Clear();cut.Weeks.Clear();}
        data.Cuts[0].Weeks=[new(1),new(2,WeekKind.Activities),new(3),new(4)];
        data.Cuts[0].Attendance=[new("one",1,AttendanceState.Present),new("one",2,AttendanceState.Absent),
            new("two",1,AttendanceState.Late),new("two",2,AttendanceState.Absent),new("two",3,AttendanceState.Absent),new("two",4,AttendanceState.Absent),
            new("three",1,AttendanceState.Justified)];
        var service=new WorkspaceService(new MemoryStore(data));var vm=new MainViewModel(service);
        Assert.Equal(200m/6,vm.GroupAttendancePercentage);
        Assert.Equal("Regular",vm.AttendanceRows[0].Status);
        Assert.Equal("Sin registros",vm.AttendanceRows[2].Status);
        vm.Search="uno";
        Assert.Single(vm.AttendanceRows);Assert.Equal(200m/6,vm.GroupAttendancePercentage);
        service.SetAttendance(1,"one",2,AttendanceState.Present);
        Assert.Equal(50m,vm.GroupAttendancePercentage);
        Assert.Equal("100 %",vm.SelectedStudent!.Percentage);Assert.Equal("Excelente",vm.SelectedStudent.Status);
        vm.SelectedCut=2;Assert.Null(vm.GroupAttendancePercentage);
        Assert.Equal("Sin registros",vm.SelectedStudent!.Status);
    }

    [AvaloniaFact]
    public void Long_names_and_week_headers_are_readable_with_horizontal_scrolling()
    {
        var data=DemoWorkspaceFactory.Create();
        data.Students[0]=data.Students[0] with{FullName="Estudiante ficticia con nombres y apellidos extensos para comprobar la visualización completa"};
        data.Cuts[0].Weeks=Enumerable.Range(1,9).Select(n=>new AttendanceWeek(n,n%2==0 ? WeekKind.Activities : WeekKind.Class)).ToList();
        var vm=new MainViewModel(new WorkspaceService(new MemoryStore(data))){Page="Asistencias",DetailVisible=true};
        var window=new MainWindow{DataContext=vm,Width=1100,Height=800};window.Show();window.UpdateLayout();
        try
        {
            var attendance=window.FindControl<AttendanceView>("RetainedAttendance")!.FindControl<DataGrid>("AttendanceTable")!;
            Assert.Equal(Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,attendance.HorizontalScrollBarVisibility);
            Assert.True(double.IsNaN(attendance.RowHeight));
            Assert.Contains(attendance.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text==data.Students[0].FullName && t.TextWrapping==TextWrapping.Wrap);
            var weekHeader=Assert.IsType<TextBlock>(attendance.Columns[4].Header);
            Assert.Contains("Actividades",weekHeader.Text);Assert.Equal(TextWrapping.Wrap,weekHeader.TextWrapping);
            Assert.True(attendance.Columns[4].Width.Value>=105);
            if(Environment.GetEnvironmentVariable("AULA_ATTENDANCE_PREVIEW") is {Length:>0} preview)window.CaptureRenderedFrame()?.Save(preview);
            attendance.ScrollIntoView(vm.AttendanceRows[0],attendance.Columns.Last());window.UpdateLayout();
            if(Environment.GetEnvironmentVariable("AULA_ATTENDANCE_SCROLL_PREVIEW") is {Length:>0} scrollPreview)window.CaptureRenderedFrame()?.Save(scrollPreview);
            vm.Page="Notas";window.UpdateLayout();
            var notes=window.FindControl<NotesView>("RetainedNotes")!.FindControl<DataGrid>("GradesTable")!;
            Assert.Equal(Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,notes.HorizontalScrollBarVisibility);
            Assert.True(double.IsNaN(notes.RowHeight));
            if(Environment.GetEnvironmentVariable("AULA_NOTES_PREVIEW") is {Length:>0} notesPreview)window.CaptureRenderedFrame()?.Save(notesPreview);
        }
        finally{window.Close();}
    }

    private sealed class MemoryStore(AcademicWorkspace value):IWorkspaceStore
    {
        private AcademicWorkspace data=value;
        public AcademicWorkspace? Load()=>WorkspaceService.Clone(data);
        public void Save(AcademicWorkspace workspace)=>data=WorkspaceService.Clone(workspace);
    }
}
