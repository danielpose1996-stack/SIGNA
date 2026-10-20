using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class StudentBatchRegistrationTests
{
    [AvaloniaFact]
    public async Task Individual_and_bulk_drafts_are_preserved_and_review_is_required_after_each_change()
    {
        var store=new Store();var service=new WorkspaceService(store);var vm=new StudentEditorViewModel(service);
        var window=new StudentEditorWindow{DataContext=vm};window.Show();window.UpdateLayout();
        try
        {
            Assert.False(vm.IsBulk);vm.FullName="Borrador individual";
            vm.ShowBulkCommand.Execute(null);await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();Assert.True(vm.IsBulk);
            Assert.Equal(1060,window.Width);Assert.InRange(window.ClientSize.Width,1000,1100);
            var view=window.FindControl<StudentBatchView>("BulkStudentEditor")!;
            Assert.True(view.IsEffectivelyVisible);
            var names=view.FindControl<TextBox>("BulkNamesInput")!;names.Text="Ana Ficticia\nBeto Ficticio";
            Assert.Equal(names.Text,vm.Bulk!.Names);Assert.False(vm.CanSave);Assert.False(await vm.SaveAsync());
            vm.Bulk.Documents="001\n002";vm.Bulk.Phones="3000000001\n3000000002";vm.Bulk.PreviewCommand.Execute(null);
            Assert.True(vm.CanSave);Assert.Equal(2,vm.Bulk.Rows.Count);Assert.Equal("••••••••••",vm.Bulk.Rows[0].PhoneDisplay);
            vm.Bulk.ShowPhones=true;Assert.Equal("3000000001",vm.Bulk.Rows[0].PhoneDisplay);
            vm.Bulk.Documents="001\n001";Assert.False(vm.CanSave);vm.Bulk.Preview();Assert.All(vm.Bulk.Rows,r=>Assert.NotEmpty(r.Error));
            vm.ShowIndividualCommand.Execute(null);Assert.Equal("Borrador individual",vm.FullName);
            await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();Assert.False(view.IsEffectivelyVisible);
            vm.ShowBulkCommand.Execute(null);Assert.Equal("Ana Ficticia\nBeto Ficticio",vm.Bulk.Names);
            vm.Bulk.Documents="001\n002";vm.Bulk.Preview();Assert.True(vm.CanSave);
            await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);window.UpdateLayout();
            var table=view.FindControl<DataGrid>("BulkStudentsPreviewTable")!;
            Assert.True(table.Bounds.Height>=100,$"Window {window.Width}x{window.Height}, client {window.ClientSize}, view {view.Bounds}, table {table.Bounds}, visible {view.IsEffectivelyVisible}");
            Capture(window,"registro-masivo-revision.png");
            var count=service.Current.Students.Count;Assert.True(await vm.SaveAsync());Assert.Equal(count+2,service.Current.Students.Count);Assert.Equal(1,store.Saves);
            Assert.False(await vm.SaveAsync());Assert.Equal(1,store.Saves);
        }
        finally{window.Close();}
        var edit=new StudentEditorViewModel(service,service.Current.Students[0]);
        Assert.False(edit.CanUseBulk);Assert.False(edit.ShowBulkCommand.CanExecute(null));Assert.Null(edit.Bulk);
    }

    [AvaloniaFact]
    public async Task Errors_preserve_the_bulk_draft_and_cancelling_never_writes()
    {
        var store=new Store{Fail=true};var service=new WorkspaceService(store);var vm=new StudentEditorViewModel(service);
        vm.ShowBulkCommand.Execute(null);vm.Bulk!.Names="Ana Ficticia\nBeto Ficticio";vm.Bulk.Preview();
        Assert.False(await vm.SaveAsync());Assert.NotEmpty(vm.Bulk.Error);Assert.Equal("Ana Ficticia\nBeto Ficticio",vm.Bulk.Names);
        Assert.True(vm.CanEdit);Assert.True(vm.CanSave);Assert.Equal(0,store.Saves);
        var window=new StudentEditorWindow{DataContext=vm};window.Show();window.Close();Assert.Equal(0,store.Saves);
        store.Fail=false;Assert.True(await vm.SaveAsync());Assert.Equal(1,store.Saves);
    }

    private static void Capture(Window window,string name)
    {
        if(Environment.GetEnvironmentVariable("SIGNA_039_PREVIEW") is not {} folder)return;
        Directory.CreateDirectory(folder);using var image=window.CaptureRenderedFrame();image?.Save(Path.Combine(folder,name));
    }
    private sealed class Store:IWorkspaceStore
    {
        public int Saves;public bool Fail;
        public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();
        public void Save(AcademicWorkspace data){if(Fail)throw new IOException("Fallo ficticio");Saves++;}
    }
}
