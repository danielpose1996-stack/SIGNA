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
                if(Environment.GetEnvironmentVariable("GESTOR_DOCENTE_MODE")=="demo")
                {
                    var dataDirectory=Environment.GetEnvironmentVariable("GESTOR_DOCENTE_DEMO_DIR") ?? Path.Combine(AppContext.BaseDirectory,".demo-data");
                    desktop.MainWindow=new MainWindow {DataContext=new MainViewModel(new WorkspaceService(new SqliteWorkspaceStore(Path.Combine(dataDirectory,"gestor-docente-demo.db"))),workbookReader:new XlsxStudentReader(),reportExporter:new XlsxReportExporter())};
                }
                else
                {
                    var dataDirectory=Environment.GetEnvironmentVariable("GESTOR_DOCENTE_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AulaRegistro");
                    var catalog=new CatalogService(new SqliteCatalogStore(Path.Combine(dataDirectory,"aula-registro.db")));
                    desktop.MainWindow=new MainWindow {DataContext=new MainViewModel(new WorkspaceService(new EmptyWorkspaceStore()),catalog,new XlsxStudentReader(),new XlsxReportExporter())};
                }
            } catch(Exception ex) when(ex is IOException or System.Data.Common.DbException or System.Text.Json.JsonException or Domain.AcademicValidationException)
            {
                desktop.MainWindow=new Window {Title="Gestor Docente · no se pudieron abrir los datos",Width=620,Height=260,Content=new TextBlock {Margin=new(30),TextWrapping=Avalonia.Media.TextWrapping.Wrap,Text="No se pudo abrir el almacenamiento local. Los datos no se reemplazaron.\n\n"+ex.Message}};
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
}
internal sealed class EmptyWorkspaceStore:IWorkspaceStore
{
    public Domain.AcademicWorkspace? Load()=>new(){IsDemo=false,CourseName="",CourseCode="",GroupName="",Period="",Cuts=Enumerable.Range(1,3).Select(n=>new Domain.AcademicCut{Number=n}).ToList()};
    public void Save(Domain.AcademicWorkspace workspace)=>throw new Domain.AcademicValidationException("Abre primero una asignatura.");
}
