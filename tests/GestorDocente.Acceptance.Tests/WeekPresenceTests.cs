using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class WeekPresenceTests
{
    [AvaloniaFact]
    public async Task Checkbox_marks_all_students_and_updates_group_detail_and_report()
    {
        var store=new Store();var service=new WorkspaceService(store);var mainVm=new MainViewModel(service){Page="Asistencias",Search="uno"};
        var model=new WeeksViewModel(service,1){SelectedDate=new DateTime(2026,10,13)};var window=new WeeksWindow{DataContext=model};window.Show();
        try
        {
            var checkbox=window.FindControl<CheckBox>("MarkAllPresentCheckBox")!;
            Assert.False(model.MarkAllPresent);Assert.False(checkbox.IsChecked);Assert.Equal("Marcar presente a todos",checkbox.Content);
            checkbox.IsChecked=true;Assert.True(model.MarkAllPresent);model.SelectedKind=model.Kinds[1];
            await model.AddAsync();window.UpdateLayout();
            Assert.Equal(1,store.Saves);Assert.Equal(2,service.Cut(1).Attendance.Count);Assert.False(model.MarkAllPresent);Assert.False(checkbox.IsChecked);Assert.Null(model.SelectedDate);
            Assert.Equal(100m,mainVm.GroupAttendancePercentage);Assert.Equal("100 %",mainVm.SelectedStudent!.Percentage);Assert.Equal("Excelente",mainVm.SelectedStudent.Status);
            var report=AttendancePdfProjection.Build(service.Current,1,mainVm.SelectedStudent.Student.Id);Assert.Equal("P - Presente",Assert.Single(report.Lines).State);
            await service.SetAttendanceAsync(1,"s1",1,AttendanceState.Absent);Assert.Equal(50m,mainVm.GroupAttendancePercentage);
            Assert.Equal("0 %",mainVm.SelectedStudent!.Percentage);Assert.Equal("Bajo",mainVm.SelectedStudent.Status);
            await model.AddAsync();Assert.Equal(2,service.Cut(1).Weeks.Count);Assert.Equal(2,service.Cut(1).Attendance.Count);
        }
        finally{window.Close();}
    }

    [AvaloniaFact]
    public async Task Failed_save_keeps_optional_choice_and_date_for_retry()
    {
        var store=new Store{Fail=true};var service=new WorkspaceService(store);var model=new WeeksViewModel(service,1){MarkAllPresent=true,SelectedDate=new DateTime(2026,10,13)};
        await model.AddAsync();Assert.NotEmpty(model.Error);Assert.True(model.MarkAllPresent);Assert.NotNull(model.SelectedDate);
        Assert.Empty(model.Weeks);Assert.Empty(service.Cut(1).Attendance);Assert.False(model.IsSaving);
        store.Fail=false;await model.AddAsync();Assert.Empty(model.Error);Assert.Single(model.Weeks);Assert.Equal(2,service.Cut(1).Attendance.Count);Assert.False(model.MarkAllPresent);
    }

    private sealed class Store:IWorkspaceStore
    {
        private AcademicWorkspace data=new(){Students=[new("s1","F01","Estudiante ficticio uno"),new("s2","F02","Estudiante ficticio dos")],Cuts=AcademicRules.DefaultCuts()};
        public int Saves;public bool Fail;
        public AcademicWorkspace? Load()=>WorkspaceService.Clone(data);
        public void Save(AcademicWorkspace value){if(Fail)throw new IOException("Fallo simulado");data=WorkspaceService.Clone(value);Saves++;}
    }
}
