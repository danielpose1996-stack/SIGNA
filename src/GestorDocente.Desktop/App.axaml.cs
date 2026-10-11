using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GestorDocente.Application;
using GestorDocente.Infrastructure;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
namespace GestorDocente.Desktop;

public partial class App : Avalonia.Application
{
    public override void Initialize()=>AvaloniaXamlLoader.Load(this);
    public override void OnFrameworkInitializationCompleted()
    {
        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try {
                var dataDirectory=Environment.GetEnvironmentVariable("GESTOR_DOCENTE_DEMO_DIR") ?? Path.Combine(AppContext.BaseDirectory,".demo-data");
                var service=new WorkspaceService(new SqliteWorkspaceStore(Path.Combine(dataDirectory,"gestor-docente-demo.db")));
                desktop.MainWindow=new MainWindow {DataContext=new MainViewModel(service)};
            } catch(Exception ex) when(ex is IOException or System.Data.Common.DbException or System.Text.Json.JsonException or Domain.AcademicValidationException)
            {
                desktop.MainWindow=new Window {Title="Gestor Docente · no se pudo abrir la demostración",Width=620,Height=260,Content=new TextBlock {Margin=new(30),TextWrapping=Avalonia.Media.TextWrapping.Wrap,Text="No se pudo abrir la base de demostración. Los datos no se reemplazaron.\n\n"+ex.Message}};
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
}
