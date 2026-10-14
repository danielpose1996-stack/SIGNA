using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.Styles;

public sealed record PaletteOption(InterfacePalette Id,string Name,string Accent)
{
    public override string ToString()=>Name;
}
public sealed record AvatarOption(ProfileAvatar Id,string Name,string Icon)
{
    public override string ToString()=>Name;
}
public static class ThemePalettes
{
    public static IReadOnlyList<PaletteOption> Options{get;}=[new(InterfacePalette.Blue,"Azul institucional","#0067F5"),new(InterfacePalette.Emerald,"Verde","#007A66"),new(InterfacePalette.Violet,"Violeta","#6D4ACB"),new(InterfacePalette.Graphite,"Grafito","#405A70")];
    public static IReadOnlyList<AvatarOption> Avatars{get;}=[new(ProfileAvatar.Monitor,"Predeterminado","monitor"),new(ProfileAvatar.Person,"Docente","person"),new(ProfileAvatar.Book,"Libro","book"),new(ProfileAvatar.Graduation,"Académico","graduation")];

    public static IReadOnlyDictionary<string,IBrush> Brushes(InterfacePalette palette)=>Brushes(new InterfacePreferences(palette));
    public static IReadOnlyDictionary<string,IBrush> Brushes(InterfacePreferences preferences)
    {
        preferences.Validate();var palette=preferences.Palette;
        var option=Options.SingleOrDefault(p=>p.Id==palette) ?? throw new AcademicValidationException("Selecciona una paleta disponible.");
        var primary=Color.Parse(preferences.CustomAccent ?? option.Accent);
        var (hover,dark,ink,muted,icon)=palette switch
        {
            InterfacePalette.Blue=>("#0055D0","#064392","#10205C","#526D93","#27528B"),
            InterfacePalette.Emerald=>("#006252","#005344","#133830","#526A64","#2A6458"),
            InterfacePalette.Violet=>("#593AAA","#463080","#29214F","#665A80","#574480"),
            _=>("#33495E","#273D52","#1E2A37","#5C6C7B","#405A70")
        };
        if(preferences.CustomAccent is not null)
        {
            hover=Shade(primary,.80).ToString();dark=ReadableAccent(Shade(primary,.60)).ToString();
            ink="#172B45";muted="#52657C";icon=ReadableAccent(primary).ToString();
        }
        var result=new Dictionary<string,IBrush>
        {
            ["Primary"]=new ImmutableSolidColorBrush(primary),["PrimaryHover"]=Brush(hover),["PrimaryDark"]=Brush(dark),
            ["OnPrimary"]=new ImmutableSolidColorBrush(OnColor(primary)),["OnPrimaryHover"]=new ImmutableSolidColorBrush(OnColor(Color.Parse(hover))),
            ["AccentText"]=new ImmutableSolidColorBrush(ReadableAccent(primary)),
            ["Ink"]=Brush(ink),["Muted"]=Brush(muted),["IconInk"]=Brush(icon),["Surface"]=Brush("#FFFFFF"),
            ["Overlay"]=new ImmutableSolidColorBrush(Color.FromArgb(51,Color.Parse(ink).R,Color.Parse(ink).G,Color.Parse(ink).B))
        };
        foreach(var (key,blue,neutral,amount) in new[]{("Canvas","#F5F8FC","#F6F7F9",0.025), ("Soft","#EAF3FF","#F1F3F6",0.075),
            ("Sidebar","#EBF2FA","#EFF1F4",0.06),("Header","#EDF3FA","#F1F3F6",0.05),("Line","#DFE8F4","#DCE1E7",0.12),
            ("InputLine","#CEDCEE","#BBC5D0",0.20),("Selected","#DEECFF","#DFE6ED",0.14),("StrongSelected","#D4E7FF","#D4DFE9",0.18),
            ("InputSelection","#BCD9FF","#B3C3D3",0.28),("AlternateRow","#F2F6FC","#F5F7F9",0.035)})
            result[key]=preferences.CustomAccent is not null ? new ImmutableSolidColorBrush(Mix(Color.Parse(neutral),primary,amount)) :
                palette==InterfacePalette.Blue ? Brush(blue) : new ImmutableSolidColorBrush(Tint(primary,amount));
        result["DataGridRowSelectedBackgroundBrush"]=result["Selected"];
        return result;
    }
    public static void Apply(Avalonia.Application app,InterfacePalette palette)=>Apply(app,new InterfacePreferences(palette));
    public static void Apply(Avalonia.Application app,InterfacePreferences preferences)
    {
        foreach(var (key,brush) in Brushes(preferences))app.Resources[key]=brush;
        var primary=Color.Parse(preferences.CustomAccent ?? Options.Single(p=>p.Id==preferences.Palette).Accent);
        if(app.Styles.OfType<FluentTheme>().FirstOrDefault() is {} fluent)
            fluent.Palettes[ThemeVariant.Light]=new ColorPaletteResources{Accent=preferences.CustomAccent is null ? primary : ReadableAccent(primary)};
    }
    private static Color Shade(Color color,double factor)=>Color.FromRgb((byte)Math.Round(color.R*factor),(byte)Math.Round(color.G*factor),(byte)Math.Round(color.B*factor));
    private static double Luminance(Color color)
    {
        static double Channel(byte value){var x=value/255d;return x<=.04045 ? x/12.92 : Math.Pow((x+.055)/1.055,2.4);}
        return .2126*Channel(color.R)+.7152*Channel(color.G)+.0722*Channel(color.B);
    }
    private static Color OnColor(Color color)=>Luminance(color)>.179 ? Colors.Black : Colors.White;
    private static Color ReadableAccent(Color color)
    {
        while(1.05/(Luminance(color)+.05)<4.5)color=Shade(color,.9);
        return color;
    }
    private static IBrush Brush(string value)=>new ImmutableSolidColorBrush(Color.Parse(value));
    private static Color Mix(Color background,Color accent,double amount)=>Color.FromRgb((byte)Math.Round(background.R+(accent.R-background.R)*amount),(byte)Math.Round(background.G+(accent.G-background.G)*amount),(byte)Math.Round(background.B+(accent.B-background.B)*amount));
    private static Color Tint(Color color,double amount)=>Color.FromRgb((byte)Math.Round(255+(color.R-255)*amount),(byte)Math.Round(255+(color.G-255)*amount),(byte)Math.Round(255+(color.B-255)*amount));
}
