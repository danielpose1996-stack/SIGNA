using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using GestorDocente.Application;
using GestorDocente.Infrastructure;
using GestorDocente.Desktop.ViewModels;
using GestorDocente.Desktop.Views;
using GestorDocente.Desktop.Styles;
namespace GestorDocente.Desktop;

public partial class App : Avalonia.Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        var iconFile=OperatingSystem.IsWindows() ? "SIGNA_icono_windows.ico" : "SIGNA_icono_512.png";
        using var iconStream=AssetLoader.Open(new Uri($"avares://GestorDocente.Desktop/Assets/Branding/{iconFile}"));
        Resources["SignaWindowIcon"]=new WindowIcon(iconStream);
        InterfaceTheme.Apply(this,new());
    }
    public override void OnFrameworkInitializationCompleted()
    {
        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            try {
                if(Environment.GetEnvironmentVariable("GESTOR_DOCENTE_MODE")=="demo")
                {
                    var dataDirectory=Environment.GetEnvironmentVariable("GESTOR_DOCENTE_DEMO_DIR") ?? Path.Combine(AppContext.BaseDirectory,".demo-data");
                    desktop.MainWindow=new MainWindow {DataContext=new MainViewModel(new WorkspaceService(new SqliteWorkspaceStore(Path.Combine(dataDirectory,"gestor-docente-demo.db"))),workbookReader:new XlsxStudentReader(),reportExporter:new XlsxReportExporter(),attendancePdfExporter:new AttendancePdfExporter(),notesPdfExporter:new NotesPdfExporter())};
                }
                else
                {
                    desktop.MainWindow=new MainWindow {DataContext=CreateProductionViewModel()};
                }
            } catch(Exception ex)
            {
                var message=ex is Domain.AcademicValidationException ? ex.Message : $"No se pudo abrir el almacenamiento local ({SafeErrorCode.From(ex)}). Revisa permisos y espacio disponible. Los datos no se reemplazaron.";
                desktop.MainWindow=Environment.GetEnvironmentVariable("GESTOR_DOCENTE_MODE")=="demo" ?
                    new Window {Title=ProductIdentity.Name+" · no se pudieron abrir los datos",Width=620,Height=260,Content=new TextBlock {Margin=new(30),TextWrapping=Avalonia.Media.TextWrapping.Wrap,Text=message}} :
                    new RecoveryStartupWindow(ProductionDatabasePath(),message);
            }
        }
        base.OnFrameworkInitializationCompleted();
    }
    internal static MainViewModel CreateProductionViewModel()
    {
        var path=ProductionDatabasePath();
        var catalog=new CatalogService(new SqliteCatalogStore(path));
        var backup=new EncryptedLocalBackupService(path);
        var preferences=new InterfacePreferencesService(new SqliteInterfacePreferencesStore(path));
        if(Current is {} app)InterfaceTheme.Apply(app,preferences.Current);
        return new MainViewModel(new WorkspaceService(new EmptyWorkspaceStore()),catalog,new XlsxStudentReader(),new XlsxReportExporter(),backup,new AttendancePdfExporter(),preferences,new NotesPdfExporter());
    }
    internal static string ProductionDatabasePath()
    {
        var dataDirectory=Environment.GetEnvironmentVariable("GESTOR_DOCENTE_DATA_DIR") ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AulaRegistro");
        return Path.Combine(dataDirectory,"aula-registro.db");
    }
}
internal sealed class EmptyWorkspaceStore:IWorkspaceStore
{
    public Domain.AcademicWorkspace? Load()=>new(){IsDemo=false,CourseName="",CourseCode="",GroupName="",Period="",Cuts=Domain.AcademicRules.DefaultCuts()};
    public void Save(Domain.AcademicWorkspace workspace)=>throw new Domain.AcademicValidationException("Abre primero una asignatura.");
}
