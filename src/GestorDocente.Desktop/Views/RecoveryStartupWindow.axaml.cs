using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GestorDocente.Application;
using GestorDocente.Infrastructure;
using GestorDocente.Domain;
using GestorDocente.Desktop.ViewModels;

namespace GestorDocente.Desktop.Views;

public partial class RecoveryStartupWindow:Window
{
    private readonly string databasePath;
    private bool busy;
    public RecoveryStartupWindow():this("","El catálogo local no se pudo abrir."){}
    public RecoveryStartupWindow(string databasePath,string message)
    {
        InitializeComponent();Controls.WindowLayout.Configure(this);
        this.databasePath=databasePath;Explanation.Text=message;
        RestoreButton.IsEnabled=!string.IsNullOrWhiteSpace(databasePath);
    }
    private void CloseWindow(object? sender,RoutedEventArgs e)=>Close();
    private async void Restore(object? sender,RoutedEventArgs e)
    {
        if(busy || string.IsNullOrWhiteSpace(databasePath))return;
        busy=true;RestoreButton.IsEnabled=false;
        var replaced=false;
        try
        {
            var kind=new FilePickerFileType("Respaldo cifrado (.aulabk)"){Patterns=["*.aulabk"]};
            var files=await StorageProvider.OpenFilePickerAsync(new(){Title="Elegir respaldo de SIGNA",AllowMultiple=false,FileTypeFilter=[kind]});
            if(files.Count==0)return;
            using var file=files[0];
            var path=file.TryGetLocalPath() ?? throw new AcademicValidationException("Elige un archivo local de respaldo.");
            var password=await new BackupPasswordWindow("Contraseña para restaurar",false).ShowDialog<string?>(this);
            if(password is null)return;
            var backup=new EncryptedLocalBackupService(databasePath);
            Status.Text="Verificando el respaldo…";
            var inspection=await Task.Run(()=>backup.Inspect(path,password));
            if(!await new ConfirmWindow($"Se restaurarán {inspection.SemesterCount} semestres, {inspection.CourseCount} materias y {inspection.StudentCount} estudiantes, con todos sus contactos, cortes, porcentajes, actividades, notas y asistencias. Toda la información local actual será reemplazada tras conservar una copia cifrada. ¿Continuar?","Restaurar respaldo",scrollable:true){Width=620,Height=300}.ShowDialog<bool>(this))return;
            Status.Text="Conservando el archivo actual y restaurando…";
            var result=await Task.Run(()=>backup.Restore(path,password));
            replaced=true;
            var next=await App.CreateProductionViewModelAsync();
            next.BackupStatus=result.PreviousCopyWasRaw ?
                $"Restauración completa. El archivo anterior dañado se conservó cifrado en {result.PreviousBackupPath}; puede no ser una base válida." :
                $"Restauración completa: {result.SemesterCount} semestres, {result.CourseCount} materias y {result.StudentCount} estudiantes. Copia anterior: {result.PreviousBackupPath}.";
            next.NavigateCommand.Execute("Respaldos");
            if(Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window=new MainWindow{DataContext=next};desktop.MainWindow=window;window.Show();Close();
            }
            else Status.Text="Restauración completada. Vuelve a abrir la aplicación para usar los datos.";
        }
        catch(Exception ex){Status.Text=replaced ? $"El respaldo se restauró, pero no se pudo abrir el catálogo ({SafeErrorCode.From(ex)}). Cierra y vuelve a abrir la aplicación." :
            ex is AcademicValidationException ? ex.Message : $"No se completó la restauración ({SafeErrorCode.From(ex)}). El archivo actual se conservó.";}
        finally{busy=false;RestoreButton.IsEnabled=true;}
    }
}
