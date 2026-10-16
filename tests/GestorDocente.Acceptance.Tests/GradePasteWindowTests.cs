using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Domain;
using Xunit;
namespace GestorDocente.Acceptance.Tests;

public class GradePasteWindowTests
{
    [AvaloniaTheory]
    [InlineData(1300,740)] [InlineData(1100,700)]
    public async Task Headers_modals_and_optional_attendance_column_are_accessible_when_resized(int width,int height)
    {
        var store=new Store();var service=new WorkspaceService(store);var vm=new MainViewModel(service){Page="Notas"};
        var window=new MainWindow{DataContext=vm,Width=width,Height=height};window.Show();window.UpdateLayout();
        try
        {
            var table=window.FindControl<NotesView>("RetainedNotes")!.FindControl<DataGrid>("GradesTable")!;
            var editableCount=service.Cut(1).Groups.Sum(g=>g.Mode==EvaluationMode.Direct ? 1 : g.Activities.Count);
            var pasteButtons=table.Columns.Select(c=>c.Header).OfType<StackPanel>().SelectMany(s=>s.Children.OfType<Button>()).ToArray();
            Assert.Equal(editableCount,pasteButtons.Length);Assert.All(pasteButtons,b=>Assert.True(b.IsEnabled));
            var header=Assert.IsType<Button>(table.Columns[2].Header);
            var point=header.TranslatePoint(new Avalonia.Point(header.Bounds.Width/2,header.Bounds.Height/2),window)!.Value;
            window.MouseDown(point,Avalonia.Input.MouseButton.Left);window.MouseUp(point,Avalonia.Input.MouseButton.Left);window.UpdateLayout();Assert.Contains("A–Z",vm.NameHeaderLabel);
            Capture(window,$"notas-{width}.png");
            var model=new GradePasteViewModel(vm,"t1"){Input=string.Join('\n',Enumerable.Repeat("4,5",vm.GradeRows.Count))};
            var dialog=new GradePasteWindow{DataContext=model,Width=width==1100 ? 600 : 840,Height=height==700 ? 550 : 700};
            var task=window.ShowAcademicDialog(dialog);dialog.UpdateLayout();
            try
            {
                Assert.True(window.FindControl<Border>("ModalOverlay")!.IsVisible);
                model.Preview();dialog.UpdateLayout();Assert.Equal(10,dialog.FindControl<DataGrid>("GradePastePreviewTable")!.ItemsSource!.Cast<object>().Count());
                Assert.False(dialog.FindControl<Button>("SavePastedGrades")!.IsEnabled);model.ConfirmReplacement=true;dialog.UpdateLayout();Assert.True(dialog.FindControl<Button>("SavePastedGrades")!.IsEnabled);
                Capture(dialog,$"pegado-{width}.png");
            }
            finally{dialog.Close();await task;}
            Assert.False(window.FindControl<Border>("ModalOverlay")!.IsVisible);Assert.Equal(0,store.Saves);
            await service.ConfigureAttendanceGradingAsync(1,new(true,5,0,4,1));vm.Page="Asistencias";window.UpdateLayout();Capture(window,$"asistencias-{width}.png");
            var grading=new AttendanceGradingWindow{DataContext=new AttendanceGradingViewModel(service,1),Width=530,Height=height==700 ? 500 : 590};
            var gradingTask=window.ShowAcademicDialog(grading);grading.UpdateLayout();
            try{Capture(grading,$"configuracion-asistencia-{width}.png");}
            finally{grading.Close();await gradingTask;}
        }
        finally{window.Close();}
    }
    [AvaloniaFact]
    public async Task Failed_bulk_save_keeps_preview_and_allows_retry_and_cut_changes_invalidate_mapping()
    {
        var store=new Store();var vm=new MainViewModel(new(store));var bulk=new GradePasteViewModel(vm,"t1"){Input=string.Join('\n',Enumerable.Repeat("4",vm.GradeRows.Count))};
        bulk.Preview();bulk.ConfirmReplacement=true;store.Fail=true;Assert.False(await bulk.SaveAsync());
        Assert.NotEmpty(bulk.Error);Assert.True(bulk.CanSave);Assert.Equal(10,bulk.Rows.Count);Assert.False(bulk.HasSaved);
        store.Fail=false;Assert.True(await bulk.SaveAsync());Assert.Equal(1,store.Saves);Assert.False(bulk.CanSave);
        var next=new GradePasteViewModel(vm,"t1"){Input=string.Join('\n',Enumerable.Repeat("5",vm.GradeRows.Count))};next.Preview();next.ConfirmReplacement=true;vm.SelectedCut=2;
        Assert.False(await next.SaveAsync());Assert.Equal(1,store.Saves);
    }
    private static void Capture(Window window,string name)
    {
        if(Environment.GetEnvironmentVariable("SIGNA_034_PREVIEW") is not {} folder)return;
        Directory.CreateDirectory(folder);using var image=window.CaptureRenderedFrame();image?.Save(Path.Combine(folder,name));
    }
    private sealed class Store:IWorkspaceStore
    {
        public int Saves;public bool Fail;
        public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();public void Save(AcademicWorkspace data){if(Fail)throw new IOException("Fallo ficticio");Saves++;}
    }
}
