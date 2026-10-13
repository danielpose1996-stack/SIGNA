using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class CutDeletionTests
{
    [AvaloniaTheory]
    [InlineData(1,3,2)]
    [InlineData(2,2,2)]
    [InlineData(3,3,2)]
    [InlineData(3,1,1)]
    public async Task Deletion_refreshes_selection_tables_and_pending_consolidated_without_redistributing(int removed,int selected,int expected)
    {
        var service=new WorkspaceService(new Store());var vm=new MainViewModel(service){Page="Nota consolidada",SelectedCut=selected};
        var window=new MainWindow{DataContext=vm,Width=1100,Height=780};window.Show();
        try
        {
            var table=window.FindControl<ConsolidatedView>("RetainedConsolidated")!.FindControl<DataGrid>("ConsolidatedTable")!;
            await vm.DeleteCutAsync(service.PreviewCutDeletion(removed),true);
            Assert.Equal(expected,vm.SelectedCut);Assert.Equal(2,vm.Cuts.Count);Assert.Equal(7,table.Columns.Count);
            Assert.True(vm.Consolidated.WeightsPending);Assert.Null(vm.Consolidated.Rows[0].FinalValue);
            Assert.Equal("Configurar porcentajes",vm.Consolidated.Rows[0].Status);
            Assert.Contains("100",vm.Consolidated.CalculationNote);
            var weights=Assert.IsType<CutWeightsWindow>(window.OwnedWindows.Single(w=>w.IsVisible));
            var editor=Assert.IsType<CutWeightsViewModel>(weights.DataContext);Assert.False(editor.CanSave);
            Assert.Equal(service.Current.Cuts.Select(c=>c.Weight),editor.Cuts.Select(c=>c.Value!.Value));
            weights.Close();
            editor.Cuts[0].Weight="50";editor.Cuts[1].Weight="50";Assert.True(await editor.SaveAsync());
            Assert.False(vm.Consolidated.WeightsPending);Assert.NotNull(vm.Consolidated.Rows[0].FinalValue);
            vm.Page="Notas";Assert.Equal(AcademicFormatting.Grade(GradeCalculator.Cut(service.Cut(expected),"s1")),vm.GradeRows[0].Total);
            vm.Page="Asistencias";Assert.Equal(service.Cut(expected).Weeks.Count,vm.Weeks.Count);
        }
        finally{foreach(var child in window.OwnedWindows.ToArray())child.Close();window.Close();}
    }

    [AvaloniaFact]
    public async Task Confirmation_warns_about_all_associated_data_and_cancel_never_writes()
    {
        var store=new Store();var service=new WorkspaceService(store);var vm=new MainViewModel(service);
        var main=new MainWindow{DataContext=vm};main.Show();
        try
        {
            var preview=service.PreviewCutDeletion(1);var task=main.ConfirmCutDeletionAsync(preview);
            var dialog=Assert.IsType<ConfirmWindow>(main.OwnedWindows.Single(w=>w.IsVisible));dialog.UpdateLayout();
            Assert.Equal(main,dialog.Owner);Assert.True(main.FindControl<Border>("ModalOverlay")!.IsVisible);
            var text=dialog.GetVisualDescendants().OfType<TextBlock>().Single(t=>t.Text?.Contains("definitivamente")==true).Text!;
            foreach(var word in new[]{"notas","subactividades","asistencia","porcentajes","último corte"})Assert.Contains(word,text);
            var cancel=dialog.GetVisualDescendants().OfType<Button>().Single(b=>b.Content as string=="Cancelar");
            Assert.True(cancel.Bounds.Height>0);cancel.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.False(await task);await vm.DeleteCutAsync(preview,false);
            Assert.Equal(0,store.Saves);Assert.Equal(3,service.Current.Cuts.Count);Assert.False(main.FindControl<Border>("ModalOverlay")!.IsVisible);
        }
        finally{main.Close();}
    }

    [AvaloniaFact]
    public async Task Only_remaining_cut_has_disabled_delete_buttons_and_can_use_one_hundred_percent()
    {
        var service=new WorkspaceService(new Store());await service.DeleteCutAsync(service.PreviewCutDeletion(1),true);await service.DeleteCutAsync(service.PreviewCutDeletion(1),true);
        var vm=new MainViewModel(service){Page="Notas"};var main=new MainWindow{DataContext=vm};main.Show();main.UpdateLayout();
        try
        {
            var trash=main.GetVisualDescendants().OfType<Button>().Single(b=>ReferenceEquals(b.Command,vm.DeleteCutCommand));
            Assert.False(trash.IsEffectivelyEnabled);Assert.False(vm.DeleteCutCommand.CanExecute(1));Assert.False(vm.CanDeleteCut);
            var weights=new CutWeightsViewModel(service);Assert.Single(weights.Cuts);weights.Cuts[0].Weight="100";
            Assert.True(await weights.SaveAsync());Assert.NotNull(vm.Consolidated.Rows[0].FinalValue);
            await vm.AddCutAsync();Assert.True(vm.CanDeleteCut);Assert.True(vm.DeleteCutCommand.CanExecute(1));
        }
        finally{main.Close();}
    }

    private sealed class Store:IWorkspaceStore
    {
        private AcademicWorkspace data=DemoWorkspaceFactory.Create();public int Saves;
        public AcademicWorkspace? Load()=>data;
        public void Save(AcademicWorkspace value){data=WorkspaceService.Clone(value);Saves++;}
    }
}
