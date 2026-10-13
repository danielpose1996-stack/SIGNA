using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using Xunit;
using Avalonia.Threading;
using Avalonia.Interactivity;
using GestorDocente.Infrastructure;

namespace GestorDocente.Acceptance.Tests;

public class EvaluationStabilityTests
{
    [AvaloniaFact]
    public async Task Mode_change_confirmation_prevents_duplicate_submit_and_preserves_direct_grade()
    {
        var service=new WorkspaceService(new MemoryStore());service.ConfigureGroups(1,[new(){Id="work",Name="Talleres",Weight=100}]);
        service.SetGrade(1,"s1","work",4m);
        var main=new MainViewModel(service);var window=new MainWindow{DataContext=main};window.Show();main.OpenCourseCommand.Execute(null);window.UpdateLayout();
        var vm=new ConfigurationViewModel(service,1);vm.UsesSubactivities=true;vm.AddActivity();vm.AddActivity();
        var dialog=new ConfigurationWindow{DataContext=vm};var modal=window.ShowAcademicDialog(dialog);dialog.UpdateLayout();
        var button=dialog.FindControl<Button>("SaveButton")!;
        button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        var confirm=Assert.Single(dialog.OwnedWindows.OfType<ConfirmWindow>());
        Assert.Equal(EvaluationMode.Direct,service.Cut(1).Groups[0].Mode);
        confirm.Close(true);await modal.WaitAsync(TimeSpan.FromSeconds(10));window.UpdateLayout();
        Assert.True(window.IsVisible);Assert.Equal(EvaluationMode.SubActivities,service.Cut(1).Groups[0].Mode);
        Assert.Equal(4m,service.Cut(1).Grades.Single(g=>g.ActivityId==vm.Activities[0].Id && g.StudentId=="s1").Value);
        Assert.Equal(4m/service.Cut(1).Groups[0].Activities.Count,GradeCalculator.Cut(service.Cut(1),"s1"));
        Assert.Equal("Pendiente",main.GradeRows[0].Completion);window.Close();
    }

    [AvaloniaFact]
    public async Task Refresh_error_after_commit_is_reported_as_saved_and_not_as_a_failed_write()
    {
        var store=new MemoryStore();var service=new WorkspaceService(store);var vm=new ConfigurationViewModel(service,1);
        vm.SelectedGroup=vm.Groups.First();vm.AddActivity();service.Changed+=()=>throw new InvalidOperationException("Suscriptor de prueba");
        Assert.False(await vm.SaveAsync());Assert.StartsWith("Los datos se guardaron",vm.Error);
        Assert.Contains("AcademicRefreshException",vm.ErrorDetails);
        Assert.DoesNotContain("Suscriptor de prueba",vm.ErrorDetails);
        Assert.Equal(4,service.Cut(1).Groups[0].Activities.Count);
        Assert.Equal(4,new WorkspaceService(store).Cut(1).Groups[0].Activities.Count);
    }

    [AvaloniaFact]
    public async Task Invalid_percentages_leave_configuration_open_and_do_not_write()
    {
        var store=new MemoryStore();var service=new WorkspaceService(store);var vm=new ConfigurationViewModel(service,1);
        vm.Groups[0].Weight=1;var before=service.Current;
        var dialog=new ConfigurationWindow{DataContext=vm};dialog.Show();dialog.UpdateLayout();
        dialog.FindControl<Button>("SaveButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        for(var i=0;vm.IsSaving && i<500;i++)await Task.Delay(10);
        Assert.False(vm.IsSaving);Assert.True(dialog.IsVisible);Assert.Contains("100",vm.Error);
        Assert.Equal(0,store.Saves);Assert.Same(before,service.Current);Assert.Equal(1m,vm.Groups[0].Weight);dialog.Close();
    }

    [AvaloniaFact]
    public async Task Save_button_with_sqlite_and_visible_roster_keeps_main_open_and_reopens_all_subactivities()
    {
        var path=Path.Combine(Path.GetTempPath(),$"aula-estabilidad-{Guid.NewGuid():N}.db");
        try
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));var semester=catalog.CreateSemester(2026,1);
            var course=catalog.CreateCourse(semester.Id,"TEST","Materia ficticia","Escuela","");var service=catalog.Open(course.Id);
            service.ImportStudents(Enumerable.Range(1,100).Select(i=>new StudentImportCandidate($"0099{i:000}",$"Estudiante ficticio {i}")).ToList());
            var main=new MainViewModel(service,catalog);var window=new MainWindow{DataContext=main};window.Show();main.OpenCourseCommand.Execute(null);window.UpdateLayout();
            var vm=new ConfigurationViewModel(service,1);vm.AddGroup();vm.SelectedGroup!.Name="Talleres";vm.SelectedGroup.Weight=100;
            vm.UsesSubactivities=true;vm.AddActivity();vm.AddActivity();
            var dialog=new ConfigurationWindow{DataContext=vm};var modal=window.ShowAcademicDialog(dialog);dialog.UpdateLayout();
            dialog.FindControl<Button>("SaveButton")!.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await modal.WaitAsync(TimeSpan.FromSeconds(10));window.UpdateLayout();
            Assert.True(window.IsVisible);Assert.Empty(vm.Error);Assert.Equal(100,main.GradeRows.Count);
            var reopened=catalog.Open(course.Id);var group=Assert.Single(reopened.Cut(1).Groups);
            Assert.Equal("Talleres",group.Name);Assert.Equal(100m,group.Weight);
            Assert.Equal(new[]{"Talleres 1","Talleres 2","Talleres 3"},group.Activities.Select(a=>a.Name));
            Assert.Equal(100,reopened.Current.Students.Count);Assert.Empty(reopened.Cut(2).Groups);
            window.Close();
        }
        finally{File.Delete(path);}
    }

    [AvaloniaFact]
    public async Task Stale_sqlite_revision_shows_error_without_closing_or_overwriting_and_can_retry_after_reload()
    {
        var path=Path.Combine(Path.GetTempPath(),$"aula-conflicto-{Guid.NewGuid():N}.db");
        try
        {
            var catalog=new CatalogService(new SqliteCatalogStore(path));var semester=catalog.CreateSemester(2026,1);
            var course=catalog.CreateCourse(semester.Id,"TEST","Materia ficticia","Escuela","");
            var old=catalog.Open(course.Id);catalog.Open(course.Id).AddWeek(1);
            var vm=new ConfigurationViewModel(old,1);vm.AddGroup();vm.SelectedGroup!.Weight=100;
            var dialog=new ConfigurationWindow{DataContext=vm};dialog.Show();
            Assert.False(await vm.SaveAsync());Assert.True(dialog.IsVisible);Assert.Contains("Otra instancia",vm.Error);
            Assert.Empty(old.Cut(1).Groups);Assert.Empty(catalog.Open(course.Id).Cut(1).Groups);Assert.Single(catalog.Open(course.Id).Cut(1).Weeks);
            var retry=new ConfigurationViewModel(catalog.Open(course.Id),1,vm.Groups.Single());
            Assert.True(await retry.SaveAsync());Assert.Single(catalog.Open(course.Id).Cut(1).Groups);Assert.Single(catalog.Open(course.Id).Cut(1).Weeks);
            dialog.Close();
        }
        finally{File.Delete(path);}
    }

    [AvaloniaFact]
    public async Task Slow_save_yields_ui_rejects_duplicate_write_and_publishes_on_ui_thread()
    {
        var store=new DelayedStore();var service=new WorkspaceService(store);var before=service.Current;
        var vm=new ConfigurationViewModel(service,1);vm.SelectedGroup=vm.Groups.First();vm.AddActivity();
        var main=new MainViewModel(service);var window=new MainWindow{DataContext=main};window.Show();main.OpenCourseCommand.Execute(null);window.UpdateLayout();
        var dialog=new ConfigurationWindow{DataContext=vm};var modal=window.ShowAcademicDialog(dialog);
        var uiThread=Environment.CurrentManagedThreadId;var changedThread=0;service.Changed+=()=>changedThread=Environment.CurrentManagedThreadId;
        var save=vm.SaveAsync();
        try
        {
            await store.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.True(vm.IsSaving);Assert.False(dialog.FindControl<Button>("SaveButton")!.IsEnabled);
            Assert.False(save.IsCompleted);Assert.Same(before,service.Current);
            var responsive=false;await Dispatcher.UIThread.InvokeAsync(()=>responsive=true);Assert.True(responsive);
            Assert.False(await vm.SaveAsync());Assert.Equal(1,store.Saves);
            Assert.Throws<AcademicValidationException>(()=>service.AddWeek(1));
            dialog.Close();Assert.True(dialog.IsVisible);
        }
        finally{store.Release.Set();}
        Assert.True(await save);Assert.False(vm.IsSaving);Assert.NotEqual(uiThread,store.WriterThread);Assert.Equal(uiThread,changedThread);
        Assert.Equal(4,service.Cut(1).Groups[0].Activities.Count);window.UpdateLayout();Assert.True(window.IsVisible);
        dialog.Close();await modal;window.Close();
    }

    [AvaloniaTheory]
    [InlineData("permission")]
    [InlineData("unexpected")]
    public async Task Save_failure_keeps_draft_reports_details_and_can_retry(string error)
    {
        var store=new MemoryStore();var service=new WorkspaceService(store);var before=service.Current;
        var vm=new ConfigurationViewModel(service,1);vm.SelectedGroup=vm.Groups.First();vm.AddActivity();
        store.Failure=error=="permission" ? new UnauthorizedAccessException("Permiso de prueba") : new InvalidOperationException("Fallo de prueba");
        Assert.False(await vm.SaveAsync());Assert.Same(before,service.Current);Assert.Equal(4,vm.Activities.Count);
        Assert.False(vm.IsSaving);Assert.NotEmpty(vm.Error);Assert.Contains(store.Failure.GetType().Name,vm.ErrorDetails);
        store.Failure=null;Assert.True(await vm.SaveAsync());Assert.Empty(vm.Error);Assert.Empty(vm.ErrorDetails);
    }
    [AvaloniaFact]
    public void Subactivities_notify_incremental_additions_and_do_not_write_until_save()
    {
        var store=new MemoryStore();var service=new WorkspaceService(store);
        var vm=new ConfigurationViewModel(service,1);vm.SelectedGroup=vm.Groups.First();
        var dialog=new ConfigurationWindow{DataContext=vm};dialog.Show();dialog.UpdateLayout();
        var table=dialog.FindControl<DataGrid>("ActivitiesTable")!;
        var source=Assert.IsAssignableFrom<INotifyCollectionChanged>(table.ItemsSource);
        var changes=new List<NotifyCollectionChangedAction>();source.CollectionChanged+=(_,e)=>changes.Add(e.Action);
        var first=vm.Activities.First();table.SelectedItem=first;var writes=store.Saves;
        for(var i=0;i<30;i++){vm.AddActivity();dialog.UpdateLayout();}
        Assert.Equal(33,vm.Activities.Count());Assert.Equal(30,changes.Count);
        Assert.All(changes,action=>Assert.Equal(NotifyCollectionChangedAction.Add,action));
        Assert.Same(first,table.SelectedItem);Assert.Same(source,table.ItemsSource);Assert.Equal(writes,store.Saves);
        dialog.Close();
    }

    [AvaloniaFact]
    public void Saving_new_column_schema_with_visible_students_does_not_use_removed_cell_ids()
    {
        var service=new WorkspaceService(new MemoryStore());var main=new MainViewModel(service);
        var window=new MainWindow{DataContext=main};window.Show();main.OpenCourseCommand.Execute(null);window.UpdateLayout();
        // The old table contains t1/t2/t3; saving replaces them with different grade targets.
        service.ConfigureGroups(1,[new(){Id="new-work",Name="Talleres",Weight=100,Mode=EvaluationMode.SubActivities,
            Activities=[new(){Id="new-1",Name="Taller 1"},new(){Id="new-2",Name="Taller 2"},new(){Id="new-3",Name="Taller 3"}]}]);
        window.UpdateLayout();
        Assert.Equal(10,main.GradeRows.Count);Assert.Equal(3,main.GradeRows[0].Cells.Count);
        Assert.True(window.IsVisible);window.Close();
    }

    private sealed class MemoryStore:IWorkspaceStore
    {
        private AcademicWorkspace data=DemoWorkspaceFactory.Create();
        public int Saves{get;private set;}
        public Exception? Failure{get;set;}
        public AcademicWorkspace? Load()=>data;
        public void Save(AcademicWorkspace value){if(Failure is not null)throw Failure;data=WorkspaceService.Clone(value);Saves++;}
    }
    private sealed class DelayedStore:IWorkspaceStore
    {
        public TaskCompletionSource Entered{get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ManualResetEventSlim Release{get;}=new(false);
        public int Saves{get;private set;}
        public int WriterThread{get;private set;}
        public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();
        public void Save(AcademicWorkspace value)
        {
            Saves++;WriterThread=Environment.CurrentManagedThreadId;Entered.SetResult();
            if(!Release.Wait(TimeSpan.FromSeconds(10)))throw new TimeoutException("La prueba no liberó el guardado.");
        }
    }
}
