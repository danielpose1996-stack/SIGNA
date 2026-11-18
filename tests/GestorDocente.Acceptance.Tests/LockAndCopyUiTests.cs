using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Infrastructure;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class LockAndCopyUiTests
{
    [AvaloniaTheory]
    [InlineData(1580,1)] [InlineData(1100,1)] [InlineData(1100,1.25)]
    public async Task Locked_cut_disables_editing_and_preserves_reports_and_other_cuts(double width,double scale)
    {
        var service=new WorkspaceService(new Store());var main=new MainViewModel(service,notesPdfExporter:new NotesPdfExporter(),attendancePdfExporter:new AttendancePdfExporter());
        var window=new MainWindow{DataContext=main,Width=width,Height=960};
        if(scale!=1 && window.Content is Control root){window.Content=null;window.Content=new LayoutTransformControl{LayoutTransform=new ScaleTransform(scale,scale),Child=root};}
        window.Show();
        try
        {
            await main.OpenCourseAsync(null);window.UpdateLayout();
            var cut=service.Cut(1);
            var target=cut.Groups[0].Activities[0].Id;
            var before=main.GradeRows[0].Cells[target].Input;
            await service.SetCutLockedAsync(1,true);window.UpdateLayout();
            Assert.True(main.SelectedCutLocked);Assert.False(main.CanEditSelectedCut);
            Assert.False(main.ConfigureCommand.CanExecute(null));Assert.False(main.ManageWeeksCommand.CanExecute(null));
            Assert.False(main.PasteGradesCommand.CanExecute(target));Assert.True(main.CopyGradesCommand.CanExecute(target));
            Assert.False(main.GradeRows[0].Cells[target].CanEdit);
            main.GradeRows[0].Cells[target].Input="1";Assert.Equal(before,main.GradeRows[0].Cells[target].Input);
            Assert.All(main.AttendanceRows[0].Cells.Values,c=>Assert.False(c.CanEdit));
            Assert.True(main.ExportNotesPdfCommand.CanExecute(NotesPdfFormat.Detailed));
            Assert.True(main.ExportAttendancePdfCommand.CanExecute(null));
            var selector=window.FindControl<UserControl>("CourseCutSelector")!;
            var lockButton=selector.FindControl<Button>("CutLockButton")!;
            var edge=lockButton.TranslatePoint(new Point(lockButton.Bounds.Width,lockButton.Bounds.Height),selector)!.Value;
            Assert.InRange(edge.X,1,selector.Bounds.Width+1);
            var actions=selector.GetVisualDescendants().OfType<Button>().Where(b=>b.Command is not null).Cast<Control>()
                .Append(selector.FindControl<ComboBox>("AcademicCutDropdown")!).ToArray();
            var bounds=actions.Select(action=>new Rect(action.TranslatePoint(default,selector)!.Value,action.Bounds.Size)).ToArray();
            for(var i=0;i<bounds.Length;i++)
            {
                Assert.InRange(bounds[i].Left,0,selector.Bounds.Width);
                Assert.InRange(bounds[i].Right,0,selector.Bounds.Width+1);
                for(var j=i+1;j<bounds.Length;j++)Assert.False(bounds[i].Intersects(bounds[j]),$"Controles superpuestos a {width} / {scale}: {bounds[i]} y {bounds[j]}");
            }
            Capture(window,$"notas-bloqueadas-{width}-{scale}.png");
            await main.SelectCutAsync(2);Assert.True(main.CanEditSelectedCut);
            await main.SelectCutAsync(1);await service.SetCutLockedAsync(1,false);
            Assert.True(main.CanEditSelectedCut);Assert.True(main.ConfigureCommand.CanExecute(null));
        }
        finally{window.Close();}
    }

    [AvaloniaFact]
    public async Task Lock_button_requires_confirmation_and_preserves_cancelled_state()
    {
        var service=new WorkspaceService(new Store());var main=new MainViewModel(service);
        var window=new MainWindow{DataContext=main};window.Show();
        try
        {
            await main.OpenCourseAsync(null);
            main.ToggleCutLockCommand.Execute(null);
            var dialog=Assert.Single(window.OwnedWindows.OfType<ConfirmWindow>());
            Assert.False(service.Cut(1).IsLocked);dialog.Close(false);
            await Idle(main);
            Assert.False(service.Cut(1).IsLocked);
            main.ToggleCutLockCommand.Execute(null);
            Assert.Single(window.OwnedWindows.OfType<ConfirmWindow>()).Close(true);
            await Idle(main);Assert.True(service.Cut(1).IsLocked);
        }
        finally{window.Close();}
    }

    [AvaloniaFact]
    public async Task Course_form_normalizes_all_text_inputs_while_typing_and_retains_digits()
    {
        var directory=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try
        {
            var catalog=new CatalogService(new SqliteCatalogStore(Path.Combine(directory,"test.db")));
            var term=catalog.CreateSemester(2026,2);var model=new CatalogEditorViewModel(catalog,term,false);
            var window=new CatalogEditorWindow{DataContext=model};window.Show();window.UpdateLayout();
            try
            {
                var inputs=new[]{("Código de asignatura","inf-01","INF-01"),("Nombre de asignatura","álgebra","ÁLGEBRA"),
                    ("Escuela de la asignatura","ingeniería","INGENIERÍA"),("Grupo opcional","nocturno 02","NOCTURNO 02")};
                foreach(var (label,value,expected) in inputs)
                {
                    var box=window.GetVisualDescendants().OfType<TextBox>().Single(b=>AutomationProperties.GetName(b)==label);
                    box.Text=value;
                    await Dispatcher.UIThread.InvokeAsync(()=>{},DispatcherPriority.Background);
                    Assert.Equal(expected,box.Text);
                }
                Capture(window,"materia-mayusculas.png");
                Assert.True(await model.SaveAsync());Assert.Equal("ÁLGEBRA",catalog.Open(model.SavedCourseId).Current.CourseName);
            }
            finally{window.Close();}
        }
        finally{Directory.Delete(directory,true);}
    }

    private static async Task Idle(MainViewModel model)
    {
        for(var i=0;i<150 && model.FileBusy;i++)await Task.Delay(10);
        Assert.False(model.FileBusy);
    }

    private static void Capture(Window window,string name)
    {
        if(Environment.GetEnvironmentVariable("SIGNA_055_PREVIEW") is not {} folder)return;
        Directory.CreateDirectory(folder);window.UpdateLayout();using var image=window.CaptureRenderedFrame();image!.Save(Path.Combine(folder,name));
    }
    private sealed class Store:IWorkspaceStore
    {
        public AcademicWorkspace? Load()
        {
            var data=DemoWorkspaceFactory.Create();
            data.Students=data.Students.Select((s,i)=>s with{FullName=$"Estudiante ficticio {i+1:00}"}).ToList();
            return data;
        }
        public void Save(AcademicWorkspace workspace){}
    }
}
