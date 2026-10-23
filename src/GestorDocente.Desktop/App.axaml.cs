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
                    desktop.MainWindow=new MainWindow {DataContext=new MainViewModel(new WorkspaceService(new SqliteWorkspaceStore(Path.Combine(dataDirectory,"gestor-docente-demo.db"))),workbookReader:new ExcelStudentReader(),reportExporter:new XlsxReportExporter(),attendancePdfExporter:new AttendancePdfExporter(),notesPdfExporter:new NotesPdfExporter())};
                }
                else
                {
                    var startup=new StartupWindow();desktop.MainWindow=startup;
                    startup.Opened+=async (_,_)=>await startup.LoadAsync(async ()=>{var model=await CreateProductionViewModelAsync();return new MainWindow{DataContext=model};},
                        window=>desktop.MainWindow=window,ex=>new RecoveryStartupWindow(ProductionDatabasePath(),StartupError(ex)));
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
    internal static async Task<MainViewModel> CreateProductionViewModelAsync()=>CreateProductionViewModel(await Task.Run(LoadProductionServices));
    private sealed record ProductionServices(CatalogService Catalog,LocalBackupService Backup,InterfacePreferencesService Preferences,IApplicationUpdater Updater,IUpdateSafetyCheck UpdateSafety,BackupPreferencesService BackupPreferences,ILocalDataResetService DataReset);
    private static ProductionServices LoadProductionServices()
    {
        var path=ProductionDatabasePath();
        var catalog=new CatalogService(new SqliteCatalogStore(path));
        var backup=new LocalBackupService(path);
        var preferences=new InterfacePreferencesService(new SqliteInterfacePreferencesStore(path));
        var updater=new VelopackApplicationUpdater(typeof(App).Assembly.GetName().Version!.ToString(3));
        var safety=new SqliteUpdateSafetyCheck(path,()=>Velopack.Locators.VelopackLocator.Current.RootAppDir);
        return new(catalog,backup,preferences,updater,safety,new(new SqliteBackupPreferencesStore(path)),new SqliteLocalDataResetService(path));
    }
    private static MainViewModel CreateProductionViewModel(ProductionServices services)
    {
        if(Current is {} app)InterfaceTheme.Apply(app,services.Preferences.Current);
        return new MainViewModel(new WorkspaceService(new EmptyWorkspaceStore()),services.Catalog,new ExcelStudentReader(),new XlsxReportExporter(),services.Backup,new AttendancePdfExporter(),services.Preferences,new NotesPdfExporter(),services.Updater,services.UpdateSafety,services.BackupPreferences,services.DataReset);
    }
    private static string StartupError(Exception ex)=>ex is Domain.AcademicValidationException ? ex.Message : $"No se pudo abrir el almacenamiento local ({SafeErrorCode.From(ex)}). Revisa permisos y espacio disponible. Los datos no se reemplazaron.";
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
