using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using GestorDocente.Application;
using GestorDocente.Desktop.Controls;
using GestorDocente.Desktop.Styles;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Acceptance.Tests;
public class ColorSelectionTests
{
    [Theory]
    [InlineData(0,1,1,"#FF0000")][InlineData(120,1,1,"#00FF00")][InlineData(240,1,1,"#0000FF")]
    [InlineData(360,1,1,"#FF0000")][InlineData(45,0,1,"#FFFFFF")][InlineData(60,1,0,"#000000")]
    public void Hsv_to_hex_and_hex_to_hsv_are_synchronized(double hue,double saturation,double value,string expected)
    {
        Assert.Equal(expected,new ColorSelection(hue,saturation,value).ToHex());
        Assert.Equal(expected,ColorSelection.FromHex(expected,hue).ToHex());
    }
    [AvaloniaFact]
    public void Keyboard_and_achromatic_values_preserve_hue_and_closing_discards_preview()
    {
        var preferences=new InterfacePreferencesService(new MemoryInterfacePreferencesStore());var vm=new AppearanceViewModel(preferences);
        var window=new AppearanceWindow{DataContext=vm};window.Show();
        try
        {
            vm.Hue=210;vm.Saturation=0;vm.Brightness=0;Assert.Equal(210,vm.Hue);Assert.Equal("#000000",vm.SelectedHex);
            vm.Brightness=1;vm.Saturation=1;Assert.Equal(210,vm.Hue);
            vm.Saturation=.5;
            window.FindControl<HsvColorCanvas>("ColorCanvas")!.RaiseEvent(new Avalonia.Input.KeyEventArgs{RoutedEvent=Avalonia.Input.InputElement.KeyDownEvent,Key=Avalonia.Input.Key.Right});
            Assert.InRange(vm.Saturation,.509,.511);
            window.FindControl<HueColorSlider>("HueSlider")!.RaiseEvent(new Avalonia.Input.KeyEventArgs{RoutedEvent=Avalonia.Input.InputElement.KeyDownEvent,Key=Avalonia.Input.Key.Left});
            Assert.Equal(209,vm.Hue);Assert.Equal(vm.SelectedHex,vm.HexInput);
        }
        finally{window.Close();}
        Assert.Equal(Color.Parse(preferences.Current.Accent),((ISolidColorBrush)Avalonia.Application.Current!.Resources["Primary"]!).Color);
    }
    [AvaloniaFact]
    public async Task Live_preview_hex_and_canvas_are_linked_and_cancel_restores_saved_color()
    {
        var store=new MemoryInterfacePreferencesStore();var preferences=new InterfacePreferencesService(store);
        var vm=new AppearanceViewModel(preferences);var window=new AppearanceWindow{DataContext=vm};window.Show();
        try
        {
            Assert.Null(window.FindControl<ComboBox>("PaletteSelector"));
            var canvas=window.FindControl<HsvColorCanvas>("ColorCanvas")!;Assert.NotNull(canvas);
            window.FindControl<TextBox>("HexColorInput")!.Text="#2563eb";window.UpdateLayout();
            Assert.Equal("#2563EB",vm.SelectedHex);Assert.Equal(Color.Parse("#2563EB"),((ISolidColorBrush)Avalonia.Application.Current!.Resources["Primary"]!).Color);
            Capture(window,"selector-hsv.png");
            Assert.NotEqual(vm.SelectedHex,preferences.Current.Accent);
            var point=canvas.TranslatePoint(new Avalonia.Point(canvas.Bounds.Width*.75,canvas.Bounds.Height*.25),window)!.Value;
            window.MouseDown(point,Avalonia.Input.MouseButton.Left);window.MouseUp(point,Avalonia.Input.MouseButton.Left);window.UpdateLayout();
            Assert.InRange(vm.Saturation,.74,.76);Assert.InRange(vm.Brightness,.74,.76);Assert.Equal(vm.SelectedHex,vm.HexInput);
            vm.Hue=120;Assert.Equal(vm.SelectedHex,vm.HexInput);
            var saved=preferences.Current.Accent;vm.CancelPreview();Assert.Equal(Color.Parse(saved),((ISolidColorBrush)Avalonia.Application.Current.Resources["Primary"]!).Color);
            vm.ResetDraft();vm.HexInput="#FFAA22";Assert.True(await vm.SaveAsync());Assert.Equal("#FFAA22",new InterfacePreferencesService(store).Current.Accent);
            window.Width=470;window.Height=520;window.UpdateLayout();Capture(window,"selector-hsv-pequeno.png");
            vm.HexInput="#12";Assert.False(await vm.SaveAsync());Assert.NotEmpty(vm.HexError);Assert.Equal("#FFAA22",preferences.Current.Accent);
        }
        finally{vm.CancelPreview();window.Close();InterfaceTheme.Apply(Avalonia.Application.Current!,new());}
    }
    [AvaloniaFact]
    public void Notes_pdf_action_uses_entire_roster_and_current_cut_despite_a_search_filter()
    {
        var service=new WorkspaceService(new Store());var vm=new MainViewModel(service,notesPdfExporter:new GestorDocente.Infrastructure.NotesPdfExporter());
        vm.Search="Ana";vm.SelectedCut=2;NotesPdfReport? requested=null;vm.NotesPdfRequested+=()=>requested=NotesPdfProjection.Build(service.Current,vm.SelectedCut);
        var notes=new NotesView{DataContext=vm};var window=new Window{Content=notes,Width=1300,Height=700};window.Show();window.UpdateLayout();
        try
        {
            var button=notes.FindControl<Button>("NotesPdfButton")!;Assert.True(button.IsEnabled);button.Command!.Execute(null);
            Assert.NotNull(requested);Assert.Equal(2,requested.Cut);Assert.Equal(10,requested.Students.Count);Assert.Single(vm.GradeRows);
            vm.FileBusy=true;Assert.False(button.Command.CanExecute(null));vm.FileBusy=false;
            Capture(window,"notas-boton-pdf.png");
        }
        finally{window.Close();}
    }
    private static void Capture(Window window,string name)
    {
        if(Environment.GetEnvironmentVariable("SIGNA_HSV_PREVIEW") is not {} folder)return;
        Directory.CreateDirectory(folder);using var bitmap=window.CaptureRenderedFrame();bitmap?.Save(Path.Combine(folder,name));
    }
    private sealed class Store:IWorkspaceStore{public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();public void Save(AcademicWorkspace data){} }
}
