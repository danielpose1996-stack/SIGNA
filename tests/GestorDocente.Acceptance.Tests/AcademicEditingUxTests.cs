using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class AcademicEditingUxTests
{
    [AvaloniaFact]
    public async Task Activities_edit_with_visible_fields_and_new_rows_receive_focus_without_writing()
    {
        var store=new Store();var service=new WorkspaceService(store);var vm=new ConfigurationViewModel(service,1);
        var window=new ConfigurationWindow{DataContext=vm};window.Show();window.UpdateLayout();
        try
        {
            var editor=vm.GroupEditors[0];
            var name=window.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="GroupNameInput" && ReferenceEquals(t.DataContext,editor));
            name.Text="Talleres revisados";Assert.Equal("Talleres revisados",vm.Groups[0].Name);
            var percentage=window.GetVisualDescendants().OfType<TextBox>().Single(t=>ReferenceEquals(t.DataContext,editor) && AutomationProperties.GetName(t)=="Porcentaje de la actividad dentro del corte");
            percentage.Text=null;Assert.NotEmpty(vm.MainWeightError);Assert.False(await vm.SaveAsync());
            percentage.Text="texto";Assert.NotEmpty(vm.MainWeightError);Assert.False(await vm.SaveAsync());Assert.Equal(0,store.Saves);
            percentage.Text="30";Assert.Empty(vm.MainWeightError);
            window.FindControl<Button>("AddGroupButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();
            var added=vm.SelectedGroup!;
            Assert.Equal("Nueva actividad",added.Name);Assert.Equal(5,vm.Groups.Count);
            var input=window.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="GroupNameInput" && t.DataContext is EvaluationGroupEditorViewModel row && row.Group==added);
            Assert.True(input.IsFocused);input.Text="Proyecto final";
            window.FindControl<Button>("AddSubactivityButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();
            Assert.True(vm.UsesSubactivities);Assert.Single(vm.Activities);
            var sub=window.GetVisualDescendants().OfType<TextBox>().Single(t=>t.Name=="SubactivityNameInput" && ReferenceEquals(t.DataContext,vm.Activities[0]));
            Assert.True(sub.IsFocused);sub.Text="Entrega 1";Assert.Equal("Entrega 1",added.Activities[0].Name);
            Assert.Equal(0,store.Saves);Capture(window,"actividades-edicion-directa.png");
        }
        finally{window.Close();}
        Assert.Equal("Talleres",service.Cut(1).Groups[0].Name);Assert.Equal(4,service.Cut(1).Groups.Count);
    }

    [AvaloniaFact]
    public async Task Week_row_actions_target_that_week_and_cancel_confirmation_changes_nothing()
    {
        var store=new Store();var service=new WorkspaceService(store);var vm=new WeeksViewModel(service,1);
        var window=new WeeksWindow{DataContext=vm};window.Show();window.UpdateLayout();
        try
        {
            vm.SelectedWeek=vm.Weeks[0];
            RowButton(window,"week-edit",2).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));window.UpdateLayout();
            Assert.True(vm.IsEditing);Assert.Equal(2,vm.SelectedWeek!.Number);
            Assert.False(window.FindControl<CheckBox>("MarkAllPresentCheckBox")!.IsVisible);
            vm.SelectedDate=new DateTime(2026,10,20);vm.SelectedKind=vm.Kinds[1];
            Capture(window,"semana-editar.png");await vm.SaveAsync();window.UpdateLayout();
            Assert.Equal(WeekKind.Activities,service.Cut(1).Weeks[1].Kind);Assert.Equal(new DateOnly(2026,10,20),service.Cut(1).Weeks[1].Date);
            Assert.Equal(AttendanceState.Justified,service.Cut(1).Attendance.Single(a=>a.StudentId=="s2" && a.WeekNumber==2).State);
            vm.SelectedWeek=vm.Weeks[0];var writes=store.Saves;
            RowButton(window,"week-delete",2).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            var confirm=Assert.Single(window.OwnedWindows.OfType<ConfirmWindow>());confirm.Close(false);
            await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);
            Assert.Equal(writes,store.Saves);Assert.Equal(3,vm.Weeks.Count);
            RowButton(window,"week-delete",2).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            RowButton(window,"week-delete",2).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            confirm=Assert.Single(window.OwnedWindows.OfType<ConfirmWindow>());confirm.Close(true);
            for(var i=0;i<200 && (vm.IsSaving || vm.Weeks.Count==3);i++)await Task.Delay(10);
            Assert.Equal(2,vm.Weeks.Count);Assert.Equal(2,Assert.Single(service.Cut(1).RemovedWeeks).Number);
            Assert.Equal(new[]{1,2},vm.Weeks.Select(w=>w.DisplayNumber));Capture(window,"semanas-lista.png");
        }
        finally{window.Close();}
    }

    [AvaloniaFact]
    public async Task Notes_card_opens_the_clicked_activity_instead_of_the_last_one()
    {
        var vm=new MainViewModel(new WorkspaceService(new Store()));var main=new MainWindow{DataContext=vm};main.Show();vm.OpenCourseCommand.Execute(null);main.UpdateLayout();
        try
        {
            var target=vm.Groups[0];target.ConfigureCommand.Execute(target.Id);
            var dialog=Assert.Single(main.OwnedWindows.OfType<ConfigurationWindow>());
            Assert.Equal("t",Assert.IsType<ConfigurationViewModel>(dialog.DataContext).SelectedGroup!.Id);
            dialog.Close();await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);
        }
        finally{main.Close();}
    }

    [AvaloniaFact]
    public async Task Activity_and_subactivity_trash_confirm_the_clicked_row_and_only_persist_on_save()
    {
        var store=new Store();var service=new WorkspaceService(store);var vm=new ConfigurationViewModel(service,1);vm.SelectedGroup=vm.Groups[0];
        var window=new ConfigurationWindow{DataContext=vm};window.Show();window.UpdateLayout();
        try
        {
            Button GroupTrash()=>window.GetVisualDescendants().OfType<Button>().Single(b=>AutomationProperties.GetName(b)=="Eliminar actividad principal" && b.DataContext is EvaluationGroupEditorViewModel row && row.Group.Id=="q");
            GroupTrash().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));GroupTrash().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Single(window.OwnedWindows.OfType<ConfirmWindow>()).Close(false);
            await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);Assert.Equal(4,vm.Groups.Count);Assert.Equal(0,store.Saves);
            GroupTrash().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Assert.Single(window.OwnedWindows.OfType<ConfirmWindow>()).Close(true);
            await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();
            Assert.Equal(3,vm.Groups.Count);Assert.Equal("t",vm.SelectedGroup!.Id);Assert.Equal(4,service.Cut(1).Groups.Count);
            var trash=window.GetVisualDescendants().OfType<Button>().Single(b=>AutomationProperties.GetName(b)=="Eliminar subactividad" && b.DataContext is SubactivityViewModel row && row.Id=="t2");
            trash.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));Assert.Single(window.OwnedWindows.OfType<ConfirmWindow>()).Close(true);
            await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();
            Assert.Equal(2,vm.Activities.Count);Assert.Equal(3,service.Cut(1).Groups[0].Activities.Count);Assert.Equal(0,store.Saves);
            vm.GroupEditors[0].WeightInput="50";Assert.True(await vm.SaveAsync(),vm.Error);
            Assert.Single(service.Cut(1).RemovedGroups);Assert.Contains(service.Cut(1).RemovedActivities,a=>a.Activity.Id=="t2");Assert.Equal(1,store.Saves);
        }
        finally{window.Close();}
    }

    [AvaloniaTheory]
    [InlineData(1100,740,1)] [InlineData(800,600,1)] [InlineData(1100,740,1.25)]
    public void Editors_keep_primary_actions_visible_at_different_sizes(double width,double height,double scale)
    {
        var service=new WorkspaceService(new Store());var vm=new ConfigurationViewModel(service,1);vm.SelectedGroup=vm.Groups[0];
        var config=new ConfigurationWindow{DataContext=vm,Width=width,Height=height};
        var weeks=new WeeksWindow{DataContext=new WeeksViewModel(service,1),Width=width,Height=height};
        foreach(var window in new Window[]{config,weeks})
        {
            if(scale!=1 && window.Content is Control content){window.Content=null;window.Content=new LayoutTransformControl{LayoutTransform=new ScaleTransform(scale,scale),Child=content};}
            window.Show();window.UpdateLayout();
            if(window==config)Assert.True(vm.HasSelectedGroup);
            var button=window.FindControl<Button>(window==config ? "SaveButton" : "SaveWeekButton")!;
            var lowerRight=button.TranslatePoint(new Point(button.Bounds.Width,button.Bounds.Height),window);
            Assert.True(button.IsEffectivelyVisible);Assert.NotNull(lowerRight);
            Assert.InRange(lowerRight.Value.Y,0,window.ClientSize.Height+1);
            Assert.InRange(lowerRight.Value.X,0,window.ClientSize.Width+1);
            if(window==weeks)
            {
                var hint=window.FindControl<TextBlock>("WeekFooterHint")!;
                Assert.True(hint.TranslatePoint(default,window)!.Value.X>=0);
            }
            Capture(window,$"{(window==config ? "actividades" : "semanas")}-{width}-{scale}.png");window.Close();
        }
    }

    private static Button RowButton(Window window,string css,int week)=>window.GetVisualDescendants().OfType<Button>().Single(b=>b.Classes.Contains(css) && b.DataContext is AttendanceWeek w && w.Number==week);
    private static void Capture(Window window,string name)
    {
        if(Environment.GetEnvironmentVariable("SIGNA_037_PREVIEW") is not {} directory)return;
        Directory.CreateDirectory(directory);using var bitmap=window.CaptureRenderedFrame();bitmap?.Save(Path.Combine(directory,name));
    }
    private sealed class Store:IWorkspaceStore
    {
        private AcademicWorkspace data=DemoWorkspaceFactory.Create();public int Saves;
        public AcademicWorkspace? Load()=>WorkspaceService.Clone(data);
        public void Save(AcademicWorkspace value){data=WorkspaceService.Clone(value);Saves++;}
    }
}
