using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
namespace GestorDocente.Desktop.Controls;

public static class TablePresentation
{
    public static TextBlock Header(string text)=>new() {Text=text,FontSize=11,FontWeight=FontWeight.SemiBold,TextAlignment=TextAlignment.Center,TextWrapping=TextWrapping.Wrap,VerticalAlignment=VerticalAlignment.Center};
    public static DataGridTemplateColumn Text(string title,string path,double width)=>new() {
        Header=Header(title),Width=new(width),
        CellTemplate=new FuncDataTemplate<object>((_,_)=> {
            var text=new TextBlock {FontSize=12,Margin=new(4,0),VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=path=="FullName" ? HorizontalAlignment.Left : HorizontalAlignment.Center};
            text.Bind(TextBlock.TextProperty,new Binding(path));return text;
        })
    };
}
