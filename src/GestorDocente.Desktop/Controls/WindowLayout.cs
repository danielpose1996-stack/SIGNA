using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
namespace GestorDocente.Desktop.Controls;

public static class WindowLayout
{
    // Escala opcional de la aplicación para revisar accesibilidad sin cambiar la configuración de Windows.
    public static void Configure(Window window)
    {
        if(double.TryParse(Environment.GetEnvironmentVariable("GESTOR_DOCENTE_UI_SCALE"),NumberStyles.Float,CultureInfo.InvariantCulture,out var scale) && scale is >=1 and <=1.5 && scale!=1 && window.Content is Control content)
        {
            window.Content=null;
            window.Content=new LayoutTransformControl {LayoutTransform=new ScaleTransform(scale,scale),Child=content};
        }
        window.Opened+=(_,_)=> {
            var screen=window.Screens.ScreenFromWindow(window);if(screen is null)return;
            var width=screen.WorkingArea.Width/screen.Scaling-32;
            var height=screen.WorkingArea.Height/screen.Scaling-60;
            window.Width=Math.Min(window.Width,width);window.Height=Math.Min(window.Height,height);
            if(window.Owner is null)window.Position=new PixelPoint(screen.WorkingArea.X+(int)((screen.WorkingArea.Width-window.Width*screen.Scaling)/2),screen.WorkingArea.Y+(int)((screen.WorkingArea.Height-(window.Height+30)*screen.Scaling)/2));
        };
    }
}
