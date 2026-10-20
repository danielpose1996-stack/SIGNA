using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class WeightedSubactivitiesTests
{
    [AvaloniaFact]
    public async Task Optional_weight_editor_validates_decimals_and_saves_without_changing_parent_percentage()
    {
        var store=new Store();var service=new WorkspaceService(store);var vm=new ConfigurationViewModel(service,1);
        vm.SelectedGroup=vm.Groups[0];vm.Groups[2].Weight=10;vm.SelectedGroup.Weight=50;
        var dialog=new ConfigurationWindow{DataContext=vm};dialog.Show();dialog.UpdateLayout();
        var toggle=dialog.FindControl<CheckBox>("WeightedAverageToggle")!;
        Assert.False(toggle.IsChecked);Assert.False(vm.UsesWeightedAverage);
        toggle.IsChecked=true;dialog.UpdateLayout();Assert.True(vm.UsesWeightedAverage);
        vm.Activities[0].WeightInput="33,33";vm.Activities[1].WeightInput="33.33";vm.Activities[2].WeightInput="33,34";
        Assert.Contains("100",vm.InternalWeightSummary);Assert.Empty(vm.InternalWeightError);Assert.Equal(0,store.Saves);
        Assert.True(await vm.SaveAsync(),vm.Error);Assert.Equal(50m,service.Cut(1).Groups[0].Weight);
        Assert.Equal(new decimal?[]{33.33m,33.33m,33.34m},service.Cut(1).Groups[0].Activities.Select(a=>a.Weight));
        Capture(dialog,"porcentajes-internos.png");
        vm.Activities[0].WeightInput="incorrecto";Assert.NotEmpty(vm.InternalWeightError);Assert.False(await vm.SaveAsync());
        Assert.Equal(1,store.Saves);Assert.Contains("porcentaje",vm.Error,StringComparison.OrdinalIgnoreCase);
        Capture(dialog,"porcentaje-invalido.png");
        vm.Activities[0].WeightInput="30";Assert.NotEmpty(vm.InternalWeightError);Assert.False(await vm.SaveAsync());
        vm.UsesWeightedAverage=false;Assert.True(await vm.SaveAsync());
        Assert.False(service.Cut(1).Groups[0].UseWeightedAverage);Assert.Equal(30m,service.Cut(1).Groups[0].Activities[0].Weight);
        vm.UsesWeightedAverage=true;Assert.Equal("30",vm.Activities[0].WeightInput);
        dialog.Close();
    }

    [AvaloniaFact]
    public void Cancel_and_group_selection_keep_independent_drafts_without_writing()
    {
        var store=new Store();var service=new WorkspaceService(store);var vm=new ConfigurationViewModel(service,1);
        var first=vm.Groups[0];vm.SelectedGroup=first;var rows=vm.Activities;
        vm.UsesWeightedAverage=true;vm.Activities[0].WeightInput="40";vm.Activities[1].WeightInput="60";
        vm.SelectedGroup=vm.Groups[1];Assert.False(vm.UsesWeightedAverage);
        vm.SelectedGroup=first;Assert.True(vm.UsesWeightedAverage);Assert.Same(rows,vm.Activities);
        vm.UsesSubactivities=false;vm.UsesSubactivities=true;
        Assert.True(vm.UsesWeightedAverage);Assert.Equal("40",vm.Activities[0].WeightInput);
        var dialog=new ConfigurationWindow{DataContext=vm};dialog.Show();dialog.Close();
        Assert.Equal(0,store.Saves);Assert.False(service.Cut(1).Groups[0].UseWeightedAverage);
    }

    [AvaloniaFact]
    public void Saving_weighted_configuration_refreshes_visible_and_consolidated_grades()
    {
        var service=new WorkspaceService(new Store());var main=new MainViewModel(service);
        var window=new MainWindow{DataContext=main};window.Show();main.OpenCourseCommand.Execute(null);window.UpdateLayout();
        var vm=new ConfigurationViewModel(service,1);vm.SelectedGroup=vm.Groups[0];vm.UsesWeightedAverage=true;
        vm.Activities[0].WeightInput="40";vm.Activities[1].WeightInput="60";vm.Activities[2].WeightInput="0";
        Assert.True(vm.Save());window.UpdateLayout();
        Assert.Equal(GradeCalculator.Cut(service.Cut(1),"s1"),main.GradeRows[0].TotalValue);
        Assert.Contains(main.Groups,g=>g.TypeLabel.Contains("ponderado"));
        Capture(window,"notas-ponderadas.png");window.Close();
    }

    [AvaloniaFact]
    public async Task Weighted_recovery_opens_a_draft_to_adjust_percentages_before_saving()
    {
        var store=new Store();var service=new WorkspaceService(store);
        await service.ConfigureGroupsAsync(1,[new(){Id="g",Name="Parcial",Weight=100,Mode=EvaluationMode.SubActivities,UseWeightedAverage=true,
            Activities=[new(){Id="a",Name="Teoría",Weight=40},new(){Id="b",Name="Práctica",Weight=60}]}]);
        await service.SetGradeAsync(1,"s1","b",5);
        var remaining=WorkspaceService.Clone(service.Cut(1).Groups);remaining[0].Activities.RemoveAt(1);remaining[0].Activities[0].Weight=100;
        await service.ConfigureGroupsAsync(1,remaining);var saves=store.Saves;
        var recovery=new RecoveryViewModel(service,1);recovery.Selected=recovery.Items.Single(i=>i.Kind=="Subactividad" && i.Id=="b");ConfigurationViewModel? draft=null;
        recovery.SubactivityRequested+=item=>draft=new ConfigurationViewModel(service,1,restoreActivity:item);
        await recovery.RestoreAsync();Assert.NotNull(draft);Assert.Equal(2,draft.Activities.Count);
        Assert.Equal(saves,store.Saves);Assert.NotEmpty(draft.InternalWeightError);Assert.False(await draft.SaveAsync());
        draft.Activities[0].WeightInput="40";Assert.True(await draft.SaveAsync(),draft.Error);
        Assert.Empty(service.Cut(1).RemovedActivities);Assert.Equal(5m,service.Cut(1).Grades.Single(g=>g.ActivityId=="b").Value);
        Assert.Equal(3m,GradeCalculator.Cut(service.Cut(1),"s1"));
    }

    private static void Capture(Window window,string name)
    {
        if(Environment.GetEnvironmentVariable("SIGNA_036_PREVIEW") is not {} folder)return;
        Directory.CreateDirectory(folder);using var bitmap=window.CaptureRenderedFrame();bitmap?.Save(Path.Combine(folder,name));
    }
    private sealed class Store:IWorkspaceStore
    {
        private AcademicWorkspace data=DemoWorkspaceFactory.Create();public int Saves;
        public AcademicWorkspace? Load()=>WorkspaceService.Clone(data);
        public void Save(AcademicWorkspace value){data=WorkspaceService.Clone(value);Saves++;}
    }
}
