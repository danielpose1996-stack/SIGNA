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

    public static IReadOnlyDictionary<string,IBrush> Brushes(InterfacePalette palette)
    {
        var option=Options.SingleOrDefault(p=>p.Id==palette) ?? throw new AcademicValidationException("Selecciona una paleta disponible.");
        var primary=Color.Parse(option.Accent);
        var (hover,dark,ink,muted,icon)=palette switch
        {
            InterfacePalette.Blue=>("#0055D0","#064392","#10205C","#526D93","#27528B"),
            InterfacePalette.Emerald=>("#006252","#005344","#133830","#526A64","#2A6458"),
            InterfacePalette.Violet=>("#593AAA","#463080","#29214F","#665A80","#574480"),
            _=>("#33495E","#273D52","#1E2A37","#5C6C7B","#405A70")
        };
        var result=new Dictionary<string,IBrush>
        {
            ["Primary"]=new ImmutableSolidColorBrush(primary),["PrimaryHover"]=Brush(hover),["PrimaryDark"]=Brush(dark),
            ["Ink"]=Brush(ink),["Muted"]=Brush(muted),["IconInk"]=Brush(icon),["Surface"]=Brush("#FFFFFF"),
            ["Overlay"]=new ImmutableSolidColorBrush(Color.FromArgb(51,Color.Parse(ink).R,Color.Parse(ink).G,Color.Parse(ink).B))
        };
        foreach(var (key,blue,amount) in new[]{("Canvas","#F5F8FC",0.025), ("Soft","#EAF3FF",0.075),
            ("Sidebar","#EBF2FA",0.06),("Header","#EDF3FA",0.05),("Line","#DFE8F4",0.12),
            ("InputLine","#CEDCEE",0.20),("Selected","#DEECFF",0.14),("StrongSelected","#D4E7FF",0.18),
            ("InputSelection","#BCD9FF",0.28),("AlternateRow","#F2F6FC",0.035)})
            result[key]=palette==InterfacePalette.Blue ? Brush(blue) : new ImmutableSolidColorBrush(Tint(primary,amount));
        result["DataGridRowSelectedBackgroundBrush"]=result["Selected"];
        return result;
    }
    public static void Apply(Avalonia.Application app,InterfacePalette palette)
    {
        foreach(var (key,brush) in Brushes(palette))app.Resources[key]=brush;
        var primary=Color.Parse(Options.Single(p=>p.Id==palette).Accent);
        if(app.Styles.OfType<FluentTheme>().FirstOrDefault() is {} fluent)
            fluent.Palettes[ThemeVariant.Light]=new ColorPaletteResources{Accent=primary};
    }
    private static IBrush Brush(string value)=>new ImmutableSolidColorBrush(Color.Parse(value));
    private static Color Tint(Color color,double amount)=>Color.FromRgb((byte)Math.Round(255+(color.R-255)*amount),(byte)Math.Round(255+(color.G-255)*amount),(byte)Math.Round(255+(color.B-255)*amount));
}
