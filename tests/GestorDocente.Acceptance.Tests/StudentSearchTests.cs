using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class StudentSearchTests
{
    [AvaloniaFact]
    public void Typing_filters_existing_rows_without_rebuilding_academic_columns()
    {
        var store=new CountingStore();
        var service=new WorkspaceService(store);
        var vm=new MainViewModel(service);
        var notes=new NotesView { DataContext=vm };
        var attendance=new AttendanceView { DataContext=vm };
        var notesTable=Assert.IsType<DataGrid>(notes.FindControl<DataGrid>("GradesTable"));
        var attendanceTable=Assert.IsType<DataGrid>(attendance.FindControl<DataGrid>("AttendanceTable"));
        var notesColumn=notesTable.Columns[3];
        var attendanceColumn=attendanceTable.Columns[3];
        var anaGrade=vm.GradeRows[0];
        var anaAttendance=vm.AttendanceRows[0];
        vm.SelectedStudent=anaAttendance;
        var draft=anaGrade.Cells["t1"];
        draft.Input="4,99";
        var loads=store.Loads;var saves=store.Saves;
        var groupChanges=0;var weekChanges=0;
        vm.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(vm.Groups))groupChanges++;if(e.PropertyName==nameof(vm.Weeks))weekChanges++;};

        vm.Search="Ana";
        Assert.Single(vm.GradeRows);Assert.Single(vm.AttendanceRows);
        Assert.Same(anaGrade,vm.GradeRows[0]);Assert.Same(anaAttendance,vm.AttendanceRows[0]);
        Assert.Same(anaAttendance,vm.SelectedStudent);
        var resets=0;
        vm.GradeRows.CollectionChanged+=(_,e)=>{if(e.Action==NotifyCollectionChangedAction.Reset)resets++;};
        vm.Search="Ana T";
        Assert.Equal(0,resets);
        vm.Search="Carlos";
        Assert.Equal("DEMO-002",Assert.Single(vm.GradeRows).Document);
        Assert.Equal("DEMO-002",vm.SelectedStudent?.Document);
        vm.Search="DEMO-001";
        Assert.Same(anaGrade,Assert.Single(vm.GradeRows));
        Assert.Same(draft,vm.GradeRows[0].Cells["t1"]);
        Assert.Equal("4,99",draft.Input);
        Assert.Same(notesColumn,notesTable.Columns[3]);
        Assert.Same(attendanceColumn,attendanceTable.Columns[3]);
        Assert.Equal(0,groupChanges);Assert.Equal(0,weekChanges);
        Assert.Equal(loads,store.Loads);Assert.Equal(saves,store.Saves);
    }

    [AvaloniaFact]
    public void Hidden_student_updates_are_visible_after_clearing_search()
    {
        var service=new WorkspaceService(new CountingStore());
        var vm=new MainViewModel(service);
        var previous=vm.GradeRows[1];
        vm.Search="Ana";
        service.SetGrade(1,"s2","t1",4.5m);
        service.SetAttendance(1,"s2",1,AttendanceState.Absent);
        vm.Search="";
        var updated=vm.GradeRows[1];
        Assert.NotSame(previous,updated);
        Assert.Equal("4,50",updated.Cells["t1"].Input);
        Assert.Equal(AttendanceState.Absent,vm.AttendanceRows[1].Cells[1].Selected.State);
        vm.Search="sin coincidencias";
        Assert.Empty(vm.GradeRows);Assert.Empty(vm.AttendanceRows);
        vm.Search=null!;
        Assert.Equal(string.Empty,vm.Search);
        Assert.Equal(10,vm.GradeRows.Count);Assert.Equal(10,vm.AttendanceRows.Count);
    }

    private sealed class CountingStore:IWorkspaceStore
    {
        private AcademicWorkspace data=DemoWorkspaceFactory.Create();
        public int Loads {get;private set;}
        public int Saves {get;private set;}
        public AcademicWorkspace? Load(){Loads++;return WorkspaceService.Clone(data);}
        public void Save(AcademicWorkspace value){Saves++;data=WorkspaceService.Clone(value);}
    }
}
