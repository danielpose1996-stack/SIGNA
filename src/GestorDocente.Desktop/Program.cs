using System.Globalization;
using Avalonia;
namespace GestorDocente.Desktop;
internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture=CultureInfo.GetCultureInfo("es-CO");
        CultureInfo.DefaultThreadCurrentUICulture=CultureInfo.GetCultureInfo("es");
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    public static AppBuilder BuildAvaloniaApp()=>AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace();
}
