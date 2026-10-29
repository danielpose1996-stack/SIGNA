using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.Controls;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Infrastructure;
using Xunit;
namespace GestorDocente.Acceptance.Tests;

public class PersonalizedCutsTests
{
    [AvaloniaTheory]
    [InlineData(1450,900,1)] [InlineData(1100,740,1)] [InlineData(1100,800,1.25)]
    public async Task Dropdown_manager_and_consolidated_risk_are_accessible_and_keep_grades(double width,double height,double scale)
    {
        var service=new WorkspaceService(new Store());var vm=new MainViewModel(service,consolidatedPdfExporter:new ConsolidatedPdfExporter()){Page="Notas"};
        var window=new MainWindow{DataContext=vm,Width=width,Height=height};
        if(scale!=1){var content=(Control)window.Content!;window.Content=null;window.Content=new LayoutTransformControl{LayoutTransform=new ScaleTransform(scale,scale),Child=content};}
        window.Show();window.UpdateLayout();
        try
        {
            var selector=window.GetVisualDescendants().OfType<ComboBox>().Single(c=>c.Name=="AcademicCutDropdown" && c.IsEffectivelyVisible);
            Assert.Equal(3,selector.ItemCount);Assert.Equal(1,Assert.IsType<CutColumn>(selector.SelectedItem).Number);
            selector.SelectedIndex=1;
            for(var i=0;i<100 && vm.FileBusy;i++)await Task.Delay(10);
            window.UpdateLayout();Assert.Equal(2,vm.SelectedCut);Assert.Equal(2,Assert.IsType<CutColumn>(selector.SelectedItem).Number);
            Capture(window,$"notas-selector-{width}.png");
            vm.ConfigureCutsCommand.Execute(null);var manager=window.OwnedWindows.OfType<CutManagementWindow>().Single();manager.UpdateLayout();
            var editor=Assert.IsType<CutManagementViewModel>(manager.DataContext);editor.Cuts[0].Name="Primer período";
            Assert.False(editor.CanModify);Assert.True(await editor.SaveAsync());Assert.Equal("Primer período",service.Cut(1).Label);
            Capture(manager,"administrar-cortes.png");manager.Close();
            vm.Page="Asistencias";window.UpdateLayout();Capture(window,$"asistencias-selector-{width}.png");
            vm.Page="Nota consolidada";window.UpdateLayout();
            var table=window.FindControl<ConsolidatedView>("RetainedConsolidated")!.FindControl<DataGrid>("ConsolidatedTable")!;
            Assert.True(table.Bounds.Height>180,$"La tabla necesita espacio para el listado: {table.Bounds.Height}");
            if(scale>1)Assert.False(window.FindControl<ConsolidatedView>("RetainedConsolidated")!.FindControl<Expander>("RiskExpander")!.IsExpanded);
            Assert.Equal(9,table.Columns.Count);Assert.True(vm.ExportConsolidatedPdfCommand.CanExecute(null));
            Assert.Equal(new[]{"s2","s1"},vm.Consolidated.RiskStudents.Select(s=>s.Student.Id));
            Assert.Contains(window.GetVisualDescendants().OfType<TextBlock>(),t=>t.Text=="Pendiente" && t.IsEffectivelyVisible);
            Capture(window,$"consolidado-riesgo-{width}-{scale}.png");
            table.ScrollIntoView(vm.Consolidated.Rows[0],table.Columns.Last());window.UpdateLayout();Capture(window,$"consolidado-final-{width}-{scale}.png");
            await service.SetGradeAsync(1,"s2","p",5);Assert.Single(vm.Consolidated.RiskStudents);
            Assert.Equal("Normal",vm.Consolidated.Rows.Single(s=>s.Student.Id=="s2").Status);
        }
        finally{foreach(var child in window.OwnedWindows.ToArray())child.Close();window.Close();}
    }
    [AvaloniaFact]
    public void Risk_cards_virtualize_large_rosters_instead_of_building_every_student_control()
    {
        var data=new AcademicWorkspace{Students=Enumerable.Range(1,1200).Select(i=>new Student($"s{i}","",$"Estudiante ficticio {i:0000}")).ToList(),Cuts=[
            new(){Number=1,Weight=30,Groups=[new(){Id="p",Name="Trabajo",Weight=100}]},new(){Number=2,Weight=30},new(){Number=3,Weight=40}]};
        data.Cuts[0].Grades=data.Students.Select(s=>new GradeEntry(s.Id,"p",2)).ToList();
        var vm=new MainViewModel(new WorkspaceService(new Store(data))){Page="Nota consolidada"};var window=new MainWindow{DataContext=vm,Width=1100,Height=740};window.Show();window.UpdateLayout();
        try
        {
            Assert.Equal(1200,vm.Consolidated.RiskStudents.Count);
            var cards=window.FindControl<ConsolidatedView>("RetainedConsolidated")!.FindControl<ItemsControl>("RiskCards")!;
            var realized=cards.GetVisualDescendants().OfType<TextBlock>().Count(t=>t.DataContext is ConsolidatedStudentRow row && t.Text==row.FullName);
            Assert.InRange(realized,1,12);
        }
        finally{window.Close();}
    }
    [AvaloniaFact]
    public async Task Unsaved_grade_drafts_are_not_discarded_by_the_new_cut_workflow()
    {
        var service=new WorkspaceService(new Store());var vm=new MainViewModel(service){Page="Notas"};
        var cell=vm.GradeRows[0].Cells["p"];cell.Input="dato inválido";
        Assert.Throws<AcademicValidationException>(()=>vm.BeginCutConfiguration());
        await vm.SelectCutAsync(2);Assert.Equal(1,vm.SelectedCut);Assert.False(vm.FileBusy);
        Assert.Contains("pendientes",vm.Message);Assert.Equal("dato inválido",cell.Input);Assert.Equal(2,service.Cut(1).Grades[0].Value);
    }
    [AvaloniaFact]
    public async Task Manager_preserves_drafts_on_failed_save_and_last_cut_cannot_be_deleted()
    {
        var store=new Store();var service=new WorkspaceService(store);var editor=new CutManagementViewModel(service);
        editor.Cuts[0].Name="Período uno";store.Fail=true;Assert.False(await editor.SaveAsync());
        Assert.NotEmpty(editor.Error);Assert.Equal("Período uno",editor.Cuts[0].Name);Assert.Equal("Corte 1",service.Cut(1).Label);
        store.Fail=false;Assert.True(await editor.SaveAsync());
        Assert.False(await editor.DeleteAsync(service.PreviewCutDeletion(1),false));Assert.Equal(3,service.Current.Cuts.Count);
        Assert.True(await editor.DeleteAsync(service.PreviewCutDeletion(1),true));Assert.Equal(new[]{1,2},editor.Cuts.Select(c=>c.Number));
        Assert.True(await editor.DeleteAsync(service.PreviewCutDeletion(1),true));Assert.False(editor.CanDelete);
        Assert.False(editor.DeleteCommand.CanExecute(1));await editor.AddAsync();Assert.True(editor.CanDelete);Assert.Equal(0,editor.Cuts[1].Weight);
    }
    private static void Capture(Window window,string name)
    {
        if(Environment.GetEnvironmentVariable("SIGNA_048_PREVIEW") is not {} directory)return;
        Directory.CreateDirectory(directory);window.UpdateLayout();using var bitmap=window.CaptureRenderedFrame();bitmap?.Save(Path.Combine(directory,name));
    }
    private sealed class Store:IWorkspaceStore
    {
        public bool Fail;
        public Store(AcademicWorkspace? workspace=null){if(workspace is not null)data=workspace;}
        private AcademicWorkspace data=new(){CourseName="Asignatura ficticia",Students=[new("s1","FIC-1","Estudiante Ficticia Uno","uno@example.invalid"),new("s2","FIC-2","Estudiante Ficticio Dos","dos@example.invalid"),new("s3","FIC-3","Estudiante Ficticio Tres")],Cuts=[
            new(){Number=1,Weight=30,Groups=[new(){Id="p",Name="Trabajo",Weight=100}],Grades=[new("s1","p",2),new("s2","p",0),new("s3","p",5)]},
            new(){Number=2,Weight=30},new(){Number=3,Weight=40}]};
        public AcademicWorkspace? Load()=>data;
        public void Save(AcademicWorkspace value){if(Fail)throw new IOException("Fallo ficticio");data=WorkspaceService.Clone(value);}
    }
}
