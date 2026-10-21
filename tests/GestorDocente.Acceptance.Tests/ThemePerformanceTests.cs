using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using GestorDocente.Application;
using GestorDocente.Desktop.Controls;
using GestorDocente.Desktop.Styles;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Domain;
using Xunit;

namespace GestorDocente.Acceptance.Tests;

public class ThemePerformanceTests
{
    [AvaloniaFact]
    public void Live_drag_coalesces_updates_and_release_flushes_the_last_selection()
    {
        var store=new Store();var preferences=new InterfacePreferencesService(store);var applied=new List<string>();
        var vm=new AppearanceViewModel(preferences,p=>applied.Add(p.Accent));
        vm.BeginColorInteraction();
        for(var i=0;i<100;i++){vm.Saturation=.01*i;vm.Brightness=1-.005*i;}
        Assert.Empty(applied);Assert.Equal(vm.SelectedHex,vm.HexInput);
        vm.EndColorInteraction();Assert.Equal(vm.SelectedHex,Assert.Single(applied));Assert.Equal(0,store.Saves);
        vm.BeginColorInteraction();vm.Hue=80;vm.CancelPreview();
        Assert.Equal(preferences.Current.Accent,applied.Last());vm.EndColorInteraction();Assert.Equal(preferences.Current.Accent,applied.Last());
    }

    [AvaloniaFact]
    public void Identical_hex_does_not_reapply_and_saving_flushes_the_final_color()
    {
        var preferences=new InterfacePreferencesService(new Store());var applied=new List<string>();var vm=new AppearanceViewModel(preferences,p=>applied.Add(p.Accent));
        vm.HexInput="#FFFFFF";Assert.Single(applied);
        vm.Hue=120;vm.Hue=121;Assert.Single(applied); // Hue changes on white, but rendered RGB does not.
        vm.BeginColorInteraction();vm.Saturation=.7;vm.EndColorInteraction();Assert.Equal(2,applied.Count);
    }

    [AvaloniaFact]
    public async Task Preview_keeps_updating_while_held_and_save_persists_the_last_color()
    {
        var preferences=new InterfacePreferencesService(new Store());var applied=new List<string>();var vm=new AppearanceViewModel(preferences,p=>applied.Add(p.Accent));
        vm.BeginColorInteraction();vm.Hue=150;vm.Saturation=.7;
        for(var i=0;i<50 && applied.Count==0;i++)await Task.Delay(10);
        Assert.Equal(vm.SelectedHex,Assert.Single(applied));
        vm.Brightness=.81;var final=vm.SelectedHex;Assert.True(await vm.SaveAsync());Assert.Equal(final,preferences.Current.Accent);Assert.Equal(final,applied.Last());
    }

    [AvaloniaFact]
    public void Theme_reuses_palette_and_skips_identical_resource_updates()
    {
        var app=Avalonia.Application.Current!;InterfaceTheme.Apply(app,new());
        var theme=app.Styles.OfType<FluentTheme>().Single();var palette=theme.Palettes[ThemeVariant.Light];
        var ink=app.Resources["Ink"];var primary=app.Resources["Primary"];
        InterfaceTheme.Apply(app,new());Assert.Same(primary,app.Resources["Primary"]);
        InterfaceTheme.Apply(app,new(Accent:"#FFFACD"));Assert.Same(ink,app.Resources["Ink"]);Assert.Same(palette,theme.Palettes[ThemeVariant.Light]);
        InterfaceTheme.Apply(app,new());
    }

    [AvaloniaTheory]
    [InlineData("#0067F5")][InlineData("#FFFFFF")][InlineData("#FFFF00")][InlineData("#000000")][InlineData("#80CCBB")]
    public void Primary_button_contrast_follows_actual_template_in_all_states(string accent)
    {
        var app=Avalonia.Application.Current!;InterfaceTheme.Apply(app,new(Accent:accent));
        var label=new TextBlock{Text="Agregar estudiante"};var icon=new VectorIcon{Kind="plus",Width=16,Height=16};
        var content=new StackPanel{Orientation=Avalonia.Layout.Orientation.Horizontal};content.Children.Add(icon);content.Children.Add(label);
        var button=new Button{Content=content,Margin=new(20)};button.Classes.Add("primary");
        var window=new Window{Content=button,Width=400,Height=160};window.Show();window.UpdateLayout();
        try
        {
            var presenter=button.GetVisualDescendants().OfType<ContentPresenter>().First(p=>p.Name=="PART_ContentPresenter");
            var point=button.TranslatePoint(new Point(button.Bounds.Width/2,button.Bounds.Height/2),window)!.Value;
            Check();Capture("normal");window.MouseMove(point);window.UpdateLayout();Assert.True(button.IsPointerOver);Check();Capture("cursor");
            window.MouseDown(point,MouseButton.Left);window.UpdateLayout();Assert.True(button.IsPressed);Check();
            window.MouseUp(point,MouseButton.Left);button.IsEnabled=false;window.UpdateLayout();Check();
            void Check()
            {
                var background=Assert.IsAssignableFrom<ISolidColorBrush>(presenter.Background).Color;
                Assert.True(Contrast(background,Assert.IsAssignableFrom<ISolidColorBrush>(label.Foreground).Color)>=4.5);
                Assert.True(Contrast(background,Assert.IsAssignableFrom<ISolidColorBrush>(icon.Stroke).Color)>=4.5);
            }
            void Capture(string state)
            {
                if(Environment.GetEnvironmentVariable("SIGNA_040_PREVIEW") is not {} folder)return;
                Directory.CreateDirectory(folder);using var image=window.CaptureRenderedFrame();image?.Save(Path.Combine(folder,$"contraste-{accent[1..]}-{state}.png"));
            }
        }
        finally{window.Close();InterfaceTheme.Apply(app,new());}
    }

    private static double Contrast(Color a,Color b)
    {
        static double L(Color c){static double C(byte b){var x=b/255d;return x<=.04045 ? x/12.92 : Math.Pow((x+.055)/1.055,2.4);}return .2126*C(c.R)+.7152*C(c.G)+.0722*C(c.B);}
        return (Math.Max(L(a),L(b))+.05)/(Math.Min(L(a),L(b))+.05);
    }
    private sealed class Store:IInterfacePreferencesStore
    {
        public int Saves;
        public InterfacePreferences Load()=>new();
        public void Save(InterfacePreferences preferences)=>Saves++;
    }
}
