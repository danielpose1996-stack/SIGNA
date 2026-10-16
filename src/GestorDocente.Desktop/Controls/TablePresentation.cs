using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
namespace GestorDocente.Desktop.Controls;

public static class TablePresentation
{
    public static TextBlock Header(string text)
    {
        var header=new TextBlock {Text=text,FontSize=11,LineHeight=15,FontWeight=FontWeight.SemiBold,TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.Wrap,VerticalAlignment=VerticalAlignment.Center};
        ToolTip.SetTip(header,text.Replace('\n',' '));return header;
    }
    public static Button NameHeader(ViewModels.MainViewModel model)
    {
        var text=Header("");text.Bind(TextBlock.TextProperty,new Binding(nameof(model.NameHeaderLabel)){Source=model});
        var button=new Button{Content=text,Command=model.SortStudentsCommand,Padding=new(3),HorizontalAlignment=HorizontalAlignment.Stretch,HorizontalContentAlignment=HorizontalAlignment.Center};
        button.Classes.Add("quiet");ToolTip.SetTip(button,"Ordenar por nombre: alternar A–Z / Z–A. Solo cambia el orden visual.");
        Avalonia.Automation.AutomationProperties.SetName(button,"Ordenar estudiantes por nombre completo");return button;
    }
    public static Control GradeHeader(string title,string id,ViewModels.MainViewModel model)
    {
        var stack=new StackPanel{Spacing=2};stack.Children.Add(Header(title));
        var button=new Button{Content="Pegar notas",Command=model.PasteGradesCommand,CommandParameter=id,FontSize=10,MinHeight=22,Padding=new(4,1),HorizontalAlignment=HorizontalAlignment.Center};
        button.Classes.Add("quiet");ToolTip.SetTip(button,$"Pegar una lista de calificaciones en {title.Replace('\n',' ')}");
        Avalonia.Automation.AutomationProperties.SetName(button,$"Pegar notas: {title.Replace('\n',' ')}");stack.Children.Add(button);return stack;
    }
    public static DataGridTemplateColumn Text(string title,string path,double width)=>new() {
        Header=Header(title),Width=new(width),
        CellTemplate=new FuncDataTemplate<object>((_,_)=> {
            var text=new TextBlock {FontSize=12,Margin=new(4,2),MinHeight=28,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Stretch,
                TextAlignment=path=="FullName" ? TextAlignment.Left : TextAlignment.Center,TextWrapping=TextWrapping.Wrap};
            text.Bind(TextBlock.TextProperty,new Binding(path));text.Bind(ToolTip.TipProperty,new Binding(path));
            if(path=="Status")text.Bind(TextBlock.ForegroundProperty,new Binding("StatusTone"));
            return text;
        })
    };
    public static DataGridTemplateColumn Grade(string title,string path,double width,ViewModels.AppearanceViewModel appearance)=>new()
    {
        Header=Header(title),Width=new(width),
        CellTemplate=new FuncDataTemplate<object>((_,_)=>
        {
            var result=new ResultGrade();result.Bind(ResultGrade.ValueProperty,new Binding(path));
            result.Bind(ResultGrade.ColorizeProperty,new Binding(nameof(ViewModels.AppearanceViewModel.CurrentColorGrades)){Source=appearance});
            return result;
        })
    };
    public static double HeaderWidth(string text,double minimum=110,double maximum=220)=>
        Math.Clamp(MeasureWidth(text.Split('\n'),11)+24,minimum,maximum);
    internal static double MeasureWidth(IEnumerable<string> values,double fontSize)
    {
        // Se mide al cambiar de materia/esquema, nunca en cada pulsación del buscador.
        var measure=new TextBlock{FontSize=fontSize};var width=0d;
        foreach(var value in values.OrderByDescending(v=>v.Length).Take(20))
        {
            measure.Text=value;measure.Measure(Size.Infinity);width=Math.Max(width,measure.DesiredSize.Width);
        }
        return width;
    }
}
