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
        var lines=text.Split('\n',2);var label=lines.Length==1 ? text : lines[0]+"\n"+lines[1].Replace('\n',' ');
        var header=new TextBlock {Text=label,FontSize=11,LineHeight=15,FontWeight=FontWeight.SemiBold,TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.NoWrap,TextTrimming=TextTrimming.CharacterEllipsis,VerticalAlignment=VerticalAlignment.Center};
        ToolTip.SetTip(header,text.Replace('\n',' '));return header;
    }
    public static DataGridTemplateColumn Text(string title,string path,double width)=>new() {
        Header=Header(title),Width=new(width),
        CellTemplate=new FuncDataTemplate<object>((_,_)=> {
            var text=new TextBlock {FontSize=12,Margin=new(4,0),VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=path=="FullName" ? HorizontalAlignment.Left : HorizontalAlignment.Center};
            text.Bind(TextBlock.TextProperty,new Binding(path));return text;
        })
    };
}
