using System.Globalization;
using Avalonia;
using GestorDocente.Application;
namespace GestorDocente.Desktop;
internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture=CultureInfo.GetCultureInfo("es-CO");
        CultureInfo.DefaultThreadCurrentUICulture=CultureInfo.GetCultureInfo("es");
        if(args.Length==1 && args[0]=="--version")
        {
            Console.WriteLine($"{ProductIdentity.Name} {typeof(Program).Assembly.GetName().Version}");
            return;
        }
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }
    public static AppBuilder BuildAvaloniaApp()
    {
        var builder=AppBuilder.Configure<App>().UsePlatformDetect();
        if(OperatingSystem.IsLinux())builder.With(new X11PlatformOptions{WmClass=ProductIdentity.Name});
        return builder.LogToTrace();
    }
}
