using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
namespace GestorDocente.Desktop.Views;

public sealed class InformationWindow : Window
{
    public InformationWindow(string title,string text)
    {
        Title=title;Width=590;Height=580;MinWidth=430;MinHeight=320;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var close=new Button {Content="Cerrar",HorizontalAlignment=HorizontalAlignment.Right,Margin=new(0,14,0,0)};
        close.Click+=(_,_)=>Close();
        var grid=new Grid {Margin=new(24),RowDefinitions=new("Auto,*,Auto")};
        grid.Children.Add(new TextBlock {Text=title,FontSize=21,FontWeight=FontWeight.SemiBold,Margin=new(0,0,0,18)});
        var scroll=new ScrollViewer {Content=new SelectableTextBlock {Text=text,TextWrapping=TextWrapping.Wrap,FontSize=13}};Grid.SetRow(scroll,1);grid.Children.Add(scroll);
        Grid.SetRow(close,2);grid.Children.Add(close);Content=grid;
    }
}
public sealed class ConfirmWindow : Window
{
    public ConfirmWindow(string text,string action="Eliminar del borrador",bool scrollable=false)
    {
        Title="Confirmar cambio";Width=460;Height=220;CanResize=false;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var cancel=new Button {Content="Cancelar"};cancel.Click+=(_,_)=>Close(false);
        var confirm=new Button {Content=action};confirm.Classes.Add("primary");confirm.Click+=(_,_)=>Close(true);
        var message=new TextBlock {Text=text,TextWrapping=TextWrapping.Wrap};
        var buttons=new StackPanel {Orientation=Orientation.Horizontal,Spacing=10,HorizontalAlignment=HorizontalAlignment.Right,Children={cancel,confirm}};
        if(scrollable)
        {
            var grid=new Grid{Margin=new(24),RowDefinitions=new("*,Auto"),RowSpacing=18};
            grid.Children.Add(new ScrollViewer{Content=message,VerticalScrollBarVisibility=Avalonia.Controls.Primitives.ScrollBarVisibility.Auto});
            Grid.SetRow(buttons,1);grid.Children.Add(buttons);Content=grid;
        }
        else Content=new StackPanel {Margin=new(24),Spacing=25,Children={message,buttons}};
    }
}
