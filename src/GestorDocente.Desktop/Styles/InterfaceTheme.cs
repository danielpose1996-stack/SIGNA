using Avalonia.Media;
using Avalonia.Controls;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.Styles;

public sealed record AvatarOption(ProfileAvatar Id,string Name,string Icon)
{
    public override string ToString()=>Name;
}
public static class InterfaceTheme
{
    public static IReadOnlyList<AvatarOption> Avatars{get;}=[new(ProfileAvatar.Monitor,"Predeterminado","monitor"),new(ProfileAvatar.Person,"Docente","person"),new(ProfileAvatar.Book,"Libro","book"),new(ProfileAvatar.Graduation,"Académico","graduation")];
    public static IReadOnlyDictionary<string,IBrush> Brushes(InterfacePreferences preferences)
    {
        preferences.Validate();var primary=Color.Parse(preferences.Accent);var hover=Shade(primary,.8);var pressed=Shade(primary,.65);var dark=ReadableAccent(Shade(primary,.6));
        var result=new Dictionary<string,IBrush>
        {
            ["Primary"]=Brush(primary),["PrimaryHover"]=Brush(hover),["PrimaryDark"]=Brush(dark),
            ["OnPrimary"]=Brush(OnColor(primary)),["OnPrimaryHover"]=Brush(OnColor(hover)),["AccentText"]=Brush(ReadableAccent(primary)),
            ["PrimaryPressed"]=Brush(pressed),["OnPrimaryPressed"]=Brush(OnColor(pressed)),
            ["PrimaryDisabled"]=Brush("#E6E8EC"),["OnPrimaryDisabled"]=Brush("#4B5563"),
            ["Ink"]=Brush("#172B45"),["Muted"]=Brush("#52657C"),["IconInk"]=Brush(ReadableAccent(primary)),["Surface"]=Brush("#FFFFFF"),
            ["Overlay"]=Brush(Color.FromArgb(51,23,43,69))
        };
        foreach(var (key,neutral,amount) in new[]{("Canvas","#F6F7F9",.025),("Soft","#F1F3F6",.075),("Sidebar","#EFF1F4",.06),("Header","#F1F3F6",.05),("Line","#DCE1E7",.12),("InputLine","#BBC5D0",.20),("Selected","#DFE6ED",.14),("StrongSelected","#D4DFE9",.18),("InputSelection","#B3C3D3",.28),("AlternateRow","#F5F7F9",.035)})
            result[key]=Brush(Mix(Color.Parse(neutral),primary,amount));
        result["DataGridRowSelectedBackgroundBrush"]=result["Selected"];return result;
    }
    public static void Apply(Avalonia.Application app,InterfacePreferences preferences)
    {
        var changed=Brushes(preferences).Where(item=>app.Resources[item.Key] is not ISolidColorBrush old || old.Color!=((ISolidColorBrush)item.Value).Color)
            .Select(item=>new KeyValuePair<object,object?>(item.Key,item.Value)).ToArray();
        if(changed.Length>0)
        {
            if(app.Resources is ResourceDictionary resources)resources.SetItems(changed);
            else foreach(var item in changed)app.Resources[item.Key]=item.Value;
        }
        if(app.Styles.OfType<FluentTheme>().FirstOrDefault() is {} fluent)
        {
            if(!fluent.Palettes.TryGetValue(ThemeVariant.Light,out var palette))fluent.Palettes[ThemeVariant.Light]=palette=new();
            var accent=ReadableAccent(Color.Parse(preferences.Accent));
            if(palette.Accent!=accent)palette.Accent=accent;
        }
    }
    private static Color Shade(Color color,double factor)=>Color.FromRgb((byte)Math.Round(color.R*factor),(byte)Math.Round(color.G*factor),(byte)Math.Round(color.B*factor));
    private static double Luminance(Color color)
    {
        static double Channel(byte value){var x=value/255d;return x<=.04045 ? x/12.92 : Math.Pow((x+.055)/1.055,2.4);}
        return .2126*Channel(color.R)+.7152*Channel(color.G)+.0722*Channel(color.B);
    }
    private static Color OnColor(Color color)=>Luminance(color)>.179 ? Colors.Black : Colors.White;
    private static Color ReadableAccent(Color color){while(1.05/(Luminance(color)+.05)<4.5)color=Shade(color,.9);return color;}
    private static IBrush Brush(string value)=>Brush(Color.Parse(value));
    private static IBrush Brush(Color color)=>new ImmutableSolidColorBrush(color);
    private static Color Mix(Color background,Color accent,double amount)=>Color.FromRgb((byte)Math.Round(background.R+(accent.R-background.R)*amount),(byte)Math.Round(background.G+(accent.G-background.G)*amount),(byte)Math.Round(background.B+(accent.B-background.B)*amount));
}
