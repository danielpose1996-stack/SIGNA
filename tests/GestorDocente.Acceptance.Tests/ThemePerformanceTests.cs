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
using GestorDocente.Desktop.Views;
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
        var sidebar=app.Resources["SidebarBackground"];var banner=app.Resources["AcademicBanner"];
        InterfaceTheme.Apply(app,new());Assert.Same(primary,app.Resources["Primary"]);
        Assert.Same(sidebar,app.Resources["SidebarBackground"]);Assert.Same(banner,app.Resources["AcademicBanner"]);
        InterfaceTheme.Apply(app,new(Accent:"#FFFACD"));Assert.Same(ink,app.Resources["Ink"]);Assert.Same(palette,theme.Palettes[ThemeVariant.Light]);
        Assert.NotSame(sidebar,app.Resources["SidebarBackground"]);Assert.NotSame(banner,app.Resources["AcademicBanner"]);
        InterfaceTheme.Apply(app,new());
    }

    [AvaloniaFact]
    public async Task Preview_updates_open_windows_and_gradients_follow_cancel_and_saved_preferences()
    {
        var app=Avalonia.Application.Current!;
        InterfaceTheme.Apply(app,new());
        var store=new Store();
        var appearance=new AppearanceViewModel(new InterfacePreferencesService(store));
        var workspace=new WorkspaceService(new DemoStore());
        var main=new MainWindow{DataContext=new MainViewModel(workspace){Page="Notas"}};
        var configuration=new ConfigurationWindow{DataContext=new ConfigurationViewModel(workspace,1)};
        var confirmation=new ConfirmWindow("¿Eliminar actividad?",destructive:true);
        main.Show();
        configuration.Show();
        confirmation.Show();
        try
        {
            var sidebar=main.GetVisualDescendants().OfType<Border>().Single(b=>b.Classes.Contains("sidebar"));
            var heading=FindHeading(main);
            var dialogHeading=FindHeading(configuration);
            var originalSidebar=Stops(sidebar.Background);
            var originalBanner=Stops(heading.Background);

            appearance.HexInput="#F54400";
            var previewSidebar=Stops(sidebar.Background);
            var previewBanner=Stops(heading.Background);
            Assert.NotEqual(originalSidebar,previewSidebar);
            Assert.NotEqual(originalBanner,previewBanner);
            Assert.Equal(previewBanner,Stops(dialogHeading.Background));
            Assert.Equal(previewBanner,Stops(confirmation.Background));
            Assert.Equal(0,store.Saves);

            appearance.CancelPreview();
            Assert.Equal(originalSidebar,Stops(sidebar.Background));
            Assert.Equal(originalBanner,Stops(heading.Background));
            Assert.Equal(originalBanner,Stops(dialogHeading.Background));

            appearance.HexInput="#F54401";
            var savedSidebar=Stops(sidebar.Background);
            var savedBanner=Stops(heading.Background);
            Assert.True(await appearance.SaveAsync());
            Assert.Equal(1,store.Saves);
            InterfaceTheme.Apply(app,new());
            InterfaceTheme.Apply(app,new InterfacePreferencesService(store).Current);
            Assert.Equal(savedSidebar,Stops(sidebar.Background));
            Assert.Equal(savedBanner,Stops(dialogHeading.Background));
        }
        finally
        {
            confirmation.Close();
            configuration.Close();
            main.Close();
            InterfaceTheme.Apply(app,new());
        }
    }

    [AvaloniaTheory]
    [InlineData("#0067F5")]
    [InlineData("#F54400")]
    [InlineData("#FFFFFF")]
    [InlineData("#000000")]
    [InlineData("#FFFF00")]
    public void Gradients_keep_distinct_stops_and_legible_foreground(string accent)
    {
        var brushes=InterfaceTheme.Brushes(new(Accent:accent));
        var (sidebarStart,sidebarEnd)=Stops(brushes["SidebarBackground"]);
        var (bannerStart,bannerEnd)=Stops(brushes["AcademicBanner"]);
        Assert.NotEqual(sidebarStart,sidebarEnd);
        Assert.NotEqual(bannerStart,bannerEnd);

        Color Solid(string key)=>Assert.IsAssignableFrom<ISolidColorBrush>(brushes[key]).Color;
        foreach(var background in new[]{sidebarStart,sidebarEnd,Solid("SidebarHover"),Solid("SidebarAction")})
        {
            Assert.True(Contrast(background,Solid("SidebarInk"))>=4.5);
            Assert.True(Contrast(background,Solid("SidebarMuted"))>=4.5);
            Assert.True(Contrast(background,Solid("SidebarWarningInk"))>=4.5);
        }
        var underWave=Blend(bannerEnd,Solid("BannerWave"),.48);
        var underBothWaves=Blend(underWave,Solid("BannerWaveEdge"),.35);
        foreach(var background in new[]{bannerStart,bannerEnd,underWave,underBothWaves})
        {
            Assert.True(Contrast(background,Solid("Ink"))>=4.5);
            Assert.True(Contrast(background,Solid("BannerMuted"))>=4.5);
        }
    }

    private static Border FindHeading(Window window)=>window.GetVisualDescendants().OfType<Border>()
        .Single(b=>b.Classes.Contains("dialog-heading") && b.IsEffectivelyVisible);

    private static (Color Start,Color End) Stops(IBrush? brush)
    {
        var stops=Assert.IsAssignableFrom<ILinearGradientBrush>(brush).GradientStops;
        Assert.Equal(2,stops.Count);
        return (stops[0].Color,stops[1].Color);
    }

    private static Color Blend(Color background,Color overlay,double opacity)
    {
        byte Mix(byte a,byte b)=>(byte)Math.Round(a+(b-a)*opacity);
        return Color.FromRgb(Mix(background.R,overlay.R),Mix(background.G,overlay.G),Mix(background.B,overlay.B));
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

    [AvaloniaTheory]
    [InlineData("#0067F5")]
    [InlineData("#FFFFFF")]
    [InlineData("#000000")]
    public void Selected_sidebar_item_preserves_text_and_icon_contrast(string accent)
    {
        var app=Avalonia.Application.Current!;
        InterfaceTheme.Apply(app,new(Accent:accent));
        var label=new TextBlock{Text="Notas y Asistencias"};
        var icon=new VectorIcon{Kind="edit",Width=18,Height=18};
        var content=new StackPanel
        {
            Orientation=Avalonia.Layout.Orientation.Horizontal,
            Children={icon,label}
        };
        var button=new Button{Content=content,Margin=new(20),Classes={"nav","active"}};
        var window=new Window{Content=new Border{Classes={"sidebar"},Child=button},Width=400,Height=180};
        window.Show();
        window.UpdateLayout();
        try
        {
            var presenter=button.GetVisualDescendants().OfType<ContentPresenter>()
                .Single(control=>control.Name=="PART_ContentPresenter");
            var point=button.TranslatePoint(new Point(28,18),window)!.Value;
            Assert.Equal(Color.Parse(accent),ColorOf(presenter.Background));
            CheckContrast();
            window.MouseMove(point);
            window.UpdateLayout();
            Assert.True(button.IsPointerOver);
            CheckContrast();
            window.MouseDown(point,MouseButton.Left);
            window.UpdateLayout();
            Assert.True(button.IsPressed);
            CheckContrast();
            window.MouseUp(point,MouseButton.Left);

            void CheckContrast()
            {
                var background=ColorOf(presenter.Background);
                Assert.True(Contrast(background,ColorOf(label.Foreground))>=4.5);
                Assert.True(Contrast(background,ColorOf(icon.Stroke))>=4.5);
            }
            static Color ColorOf(IBrush? brush)=>Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
        }
        finally
        {
            window.Close();
            InterfaceTheme.Apply(app,new());
        }
    }

    private static double Contrast(Color a,Color b)
    {
        static double L(Color c){static double C(byte b){var x=b/255d;return x<=.04045 ? x/12.92 : Math.Pow((x+.055)/1.055,2.4);}return .2126*C(c.R)+.7152*C(c.G)+.0722*C(c.B);}
        return (Math.Max(L(a),L(b))+.05)/(Math.Min(L(a),L(b))+.05);
    }
    private sealed class Store:IInterfacePreferencesStore
    {
        public int Saves;
        private InterfacePreferences current=new();
        public InterfacePreferences Load()=>current;
        public void Save(InterfacePreferences preferences){current=preferences;Saves++;}
    }

    private sealed class DemoStore:IWorkspaceStore
    {
        public AcademicWorkspace? Load()=>DemoWorkspaceFactory.Create();
        public void Save(AcademicWorkspace workspace)=>throw new InvalidOperationException("La prueba solo cambia la apariencia.");
    }
}
