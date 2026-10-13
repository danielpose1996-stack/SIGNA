using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.Controls;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class ConfigurableCutsTests
{
    [AvaloniaFact]
    public async Task New_cut_is_selectable_and_consolidated_updates_from_any_cut_without_storage_reads()
    {
        var store=new Store();var service=new WorkspaceService(store);var vm=new MainViewModel(service){Page="Nota consolidada"};
        var window=new MainWindow{DataContext=vm,Width=1100,Height=780};window.Show();
        try
        {
            var view=window.FindControl<ConsolidatedView>("RetainedConsolidated")!;
            var table=view.FindControl<DataGrid>("ConsolidatedTable")!;
            Assert.Equal(8,table.Columns.Count);
            await vm.AddCutAsync();Assert.Equal(4,vm.SelectedCut);Assert.Equal(4,vm.Cuts.Count);
            await service.ConfigureGroupsAsync(4,[new(){Id="direct",Name="Proyecto",Weight=100}]);
            await service.ConfigureCutWeightsAsync(new Dictionary<int,decimal>{{1,20},{2,20},{3,30},{4,30}});
            Assert.Equal(9,table.Columns.Count);
            await service.SetGradeAsync(4,"s1","direct",5);
            var expected=GradeCalculator.Final(service.Current,"s1");
            Assert.Equal(expected,vm.Consolidated.Rows[0].FinalValue);
            vm.SelectedCut=1;
            await service.SetGradeAsync(4,"s1","direct",0);
            Assert.Equal(expected-1.5m,vm.Consolidated.Rows[0].FinalValue);
            var reads=store.Loads;vm.Consolidated.Search="Torres";vm.Consolidated.Search="DEMO-001";Assert.Equal(reads,store.Loads);
            Assert.Single(vm.Consolidated.Rows);Assert.Equal("s1",vm.Consolidated.Rows[0].Student.Id);
            vm.Consolidated.Search="";window.UpdateLayout();
            Assert.True(view.IsVisible);Assert.True(vm.IsConsolidated);
            if(Environment.GetEnvironmentVariable("AULA_CONSOLIDATED_PREVIEW") is {Length:>0} preview)window.CaptureRenderedFrame()?.Save(preview);
            table.ScrollIntoView(vm.Consolidated.Rows[0],table.Columns[^2]);window.UpdateLayout();
            Assert.Contains(table.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text==vm.Consolidated.Rows[0].Final);
            if(Environment.GetEnvironmentVariable("AULA_FINAL_PREVIEW") is {Length:>0} finalPreview)window.CaptureRenderedFrame()?.Save(finalPreview);
        }
        finally{window.Close();}
    }

    [AvaloniaFact]
    public async Task Many_cuts_reflow_at_desktop_scale_and_remain_available_in_statistics()
    {
        var service=new WorkspaceService(new Store());for(var i=0;i<6;i++)await service.AddCutAsync();
        var vm=new MainViewModel(service){Page="Nota consolidada"};
        var window=new MainWindow{DataContext=vm,Width=1100,Height=780};
        var content=(Control)window.Content!;window.Content=null;
        window.Content=new LayoutTransformControl{LayoutTransform=new ScaleTransform(1.25,1.25),Child=content};
        window.Show();
        try
        {
            window.UpdateLayout();Assert.Equal(9,vm.Cuts.Count);
            var table=window.FindControl<ConsolidatedView>("RetainedConsolidated")!.FindControl<DataGrid>("ConsolidatedTable")!;
            Assert.Equal(14,table.Columns.Count);Assert.True(table.Bounds.Height>200,$"Altura disponible: {table.Bounds.Height}; ventana: {window.ClientSize}");
            table.ScrollIntoView(vm.Consolidated.Rows[0],table.Columns.Last());window.UpdateLayout();
            Assert.Equal(Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,table.HorizontalScrollBarVisibility);
            await vm.Dashboard.RefreshAsync();Assert.Equal(9,vm.Dashboard.Courses.Single().Cuts.Count);
            if(Environment.GetEnvironmentVariable("AULA_SCALED_CUTS_PREVIEW") is {Length:>0} preview)window.CaptureRenderedFrame()?.Save(preview);
        }
        finally{window.Close();}
    }

    [AvaloniaFact]
    public async Task Weights_modal_validates_draft_and_cancel_preserves_persisted_weights()
    {
        var service=new WorkspaceService(new Store());await service.AddCutAsync();
        var vm=new CutWeightsViewModel(service);var main=new MainWindow{DataContext=new MainViewModel(service)};main.Show();
        var dialog=new CutWeightsWindow{DataContext=vm};var task=main.ShowAcademicDialog(dialog);
        Assert.Equal(main,dialog.Owner);Assert.True(main.FindControl<Border>("ModalOverlay")!.IsVisible);
        vm.Cuts[3].Weight="10";Assert.False(vm.CanSave);Assert.False(await vm.SaveAsync());Assert.Equal(0,service.Cut(4).Weight);
        dialog.Close();await task;Assert.False(main.FindControl<Border>("ModalOverlay")!.IsVisible);
        var valid=new CutWeightsViewModel(service);valid.Cuts[0].Weight="20";valid.Cuts[3].Weight="10";
        Assert.True(await valid.SaveAsync());Assert.Equal(10,service.Cut(4).Weight);main.Close();
    }

    [AvaloniaFact]
    public async Task Optional_calendar_date_is_visible_in_week_header_and_pdf_projection()
    {
        var service=new WorkspaceService(new Store());var vm=new WeeksViewModel(service,1){SelectedDate=new DateTime(2026,10,12)};
        await vm.AddAsync();Assert.Null(vm.SelectedDate);
        await vm.AddAsync();Assert.Null(service.Cut(1).Weeks.Last().Date);
        var mainVm=new MainViewModel(service){Page="Asistencias"};var window=new MainWindow{DataContext=mainVm,Width=1100,Height=780};window.Show();
        try
        {
            window.UpdateLayout();var table=window.FindControl<AttendanceView>("RetainedAttendance")!.FindControl<DataGrid>("AttendanceTable")!;
            Assert.Contains("12/10/2026",Assert.IsType<TextBlock>(table.Columns[6].Header).Text);
            Assert.Equal(new DateOnly(2026,10,12),AttendancePdfProjection.Build(service.Current,1,"s1").Lines[3].Date);
            var dialog=new WeeksWindow{DataContext=vm};var task=window.ShowAcademicDialog(dialog);
            Assert.True(dialog.IsVisible);
            if(Environment.GetEnvironmentVariable("AULA_CALENDAR_PREVIEW") is {Length:>0} preview)dialog.CaptureRenderedFrame()?.Save(preview);
            dialog.Close();await task;
            if(Environment.GetEnvironmentVariable("AULA_DATED_ATTENDANCE_PREVIEW") is {Length:>0} datedPreview)window.CaptureRenderedFrame()?.Save(datedPreview);
        }
        finally{window.Close();}
    }

    private sealed class Store:IWorkspaceStore
    {
        private AcademicWorkspace snapshot=DemoWorkspaceFactory.Create();public int Loads;
        public AcademicWorkspace? Load(){Loads++;return snapshot;}
        public void Save(AcademicWorkspace value)=>snapshot=WorkspaceService.Clone(value);
    }
}
