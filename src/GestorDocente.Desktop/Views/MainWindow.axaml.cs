using Avalonia.Controls;
using Avalonia.Platform.Storage;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Desktop.ViewModels;
namespace GestorDocente.Desktop.Views;

public partial class MainWindow : Window
{
    private MainViewModel? observed;
    public MainWindow() {InitializeComponent();Controls.WindowLayout.Configure(this);DataContextChanged+=(_,_)=>Attach();}
    private void Attach()
    {
        if(observed is not null) {observed.ConfigurationRequested-=OpenConfiguration;observed.CatalogEditorRequested-=OpenCatalog;observed.WeekEditorRequested-=OpenWeeks;observed.RecoveryRequested-=OpenRecovery;observed.ImportRequested-=OpenImport;observed.ExportRequested-=ExportReport;observed.BackupRequested-=CreateBackup;observed.RestoreRequested-=RestoreBackup;}
        observed=DataContext as MainViewModel;
        if(observed is not null) {observed.ConfigurationRequested+=OpenConfiguration;observed.CatalogEditorRequested+=OpenCatalog;observed.WeekEditorRequested+=OpenWeeks;observed.RecoveryRequested+=OpenRecovery;observed.ImportRequested+=OpenImport;observed.ExportRequested+=ExportReport;observed.BackupRequested+=CreateBackup;observed.RestoreRequested+=RestoreBackup;}
    }
    private async void OpenConfiguration()
    {
        if(observed is null)return;
        try{await ShowAcademicDialog(new ConfigurationWindow {DataContext=new ConfigurationViewModel(observed.Service,observed.SelectedCut)});}
        catch(Exception ex){observed.Message=SafeError(ex);}
    }
    private async void OpenCatalog(bool semester)
    {
        if(observed?.Catalog is not {} catalog)return;
        var vm=new CatalogEditorViewModel(catalog,observed.Home.SelectedSemester,semester);
        try
        {
            await ShowAcademicDialog(new CatalogEditorWindow{DataContext=vm});
            if(!string.IsNullOrWhiteSpace(vm.SavedSemesterId))
            {
                await observed.Home.RefreshAsync();
                observed.Home.SelectedSemester=observed.Home.Semesters.FirstOrDefault(s=>s.Id==vm.SavedSemesterId);
                if(!string.IsNullOrWhiteSpace(vm.Error))observed.Message=vm.Error;
            }
        }
        catch(Exception ex){observed.Message=!string.IsNullOrWhiteSpace(vm.SavedSemesterId) ?
            $"El registro se guardó, pero falló el refresco ({SafeErrorCode.From(ex)}). Pulsa Actualizar." : SafeError(ex);}
    }
    private async void OpenWeeks()
    {
        if(observed is null)return;
        try{await ShowAcademicDialog(new WeeksWindow{DataContext=new WeeksViewModel(observed.Service,observed.SelectedCut)});}
        catch(Exception ex){observed.Message=SafeError(ex);}
    }
    private async void OpenRecovery()
    {
        if(observed is null)return;
        try
        {
            var model=new RecoveryViewModel(observed.Service,observed.SelectedCut);
            var dialog=new RecoveryWindow{DataContext=model};
            model.GroupRequested+=async group=>{
                try{await new ConfigurationWindow{DataContext=new ConfigurationViewModel(observed.Service,observed.SelectedCut,group)}.ShowDialog(dialog);model.Refresh();}
                catch(Exception ex){model.ReportError(SafeError(ex));}
            };
            await ShowAcademicDialog(dialog);
        }
        catch(Exception ex){observed.Message=SafeError(ex);}
    }
    public async Task ShowAcademicDialog(Window dialog)
    {
        dialog.MaxHeight=Math.Max(420,ClientSize.Height-30);
        dialog.MaxWidth=Math.Max(560,ClientSize.Width-40);
        dialog.WindowStartupLocation=WindowStartupLocation.CenterOwner;
        ModalOverlay.IsVisible=true;
        try {await dialog.ShowDialog(this);} finally {ModalOverlay.IsVisible=false;}
    }
    private static FilePickerFileType ExcelType=>new("Libro de Excel (.xlsx)"){Patterns=["*.xlsx"],MimeTypes=["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"]};
    private static FilePickerFileType BackupType=>new("Respaldo cifrado de AulaRegistro (.aulabk)"){Patterns=["*.aulabk"]};
    private async Task<string?> RequestBackupPassword(string title,bool confirmation)
    {
        ModalOverlay.IsVisible=true;
        try{return await new BackupPasswordWindow(title,confirmation).ShowDialog<string?>(this);}
        finally{ModalOverlay.IsVisible=false;}
    }
    private async void CreateBackup()
    {
        if(observed is not {} model || model.LocalBackup is not {} backup || model.FileBusy)return;
        model.FileBusy=true;
        try
        {
            var password=await RequestBackupPassword("Contraseña del respaldo",true);
            if(password is null)return;
            var suggested=$"aula-registro-{DateTime.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.aulabk";
            using var file=await StorageProvider.SaveFilePickerAsync(new(){Title="Crear respaldo cifrado",SuggestedFileName=suggested,DefaultExtension="aulabk",FileTypeChoices=[BackupType]});
            if(file is null)return;
            var path=file.TryGetLocalPath() ?? throw new AcademicValidationException("Elige una carpeta local para el respaldo.");
            if(!path.EndsWith(".aulabk",StringComparison.OrdinalIgnoreCase))throw new AcademicValidationException("El respaldo debe usar la extensión .aulabk.");
            model.BackupStatus="Creando y verificando el respaldo cifrado…";
            var result=await Task.Run(()=>backup.Create(path,password));
            model.BackupStatus=$"Respaldo verificado: {result.Path} · {result.CourseCount} materias, {result.StudentCount} estudiantes.";
        }
        catch(Exception ex){model.BackupStatus=SafeError(ex);}
        finally{model.FileBusy=false;}
    }
    private async void RestoreBackup()
    {
        if(observed is not {} model || model.LocalBackup is not {} backup || model.FileBusy)return;
        model.FileBusy=true;
        var replaced=false;
        try
        {
            if(model.Service.IsSaving)throw new AcademicValidationException("Espera a que termine el guardado de la materia antes de restaurar.");
            var files=await StorageProvider.OpenFilePickerAsync(new(){Title="Elegir respaldo cifrado",AllowMultiple=false,FileTypeFilter=[BackupType]});
            if(files.Count==0)return;
            using var file=files[0];
            var path=file.TryGetLocalPath() ?? throw new AcademicValidationException("Elige un archivo local de respaldo.");
            var password=await RequestBackupPassword("Contraseña para restaurar",false);
            if(password is null)return;
            model.BackupStatus="Verificando contraseña e integridad…";
            var inspection=await Task.Run(()=>backup.Inspect(path,password));
            var question=$"Se reemplazará el catálogo local con {inspection.CourseCount} materias y {inspection.StudentCount} estudiantes del respaldo. Antes se conservará una copia preventiva cifrada de los datos actuales. Cierra otras instancias del programa. ¿Restaurar ahora?";
            if(!await new ConfirmWindow(question,"Restaurar respaldo").ShowDialog<bool>(this))
            {model.BackupStatus="Restauración cancelada. Los datos actuales se conservan.";return;}
            model.BackupStatus="Guardando copia preventiva y restaurando…";
            var result=await Task.Run(()=>backup.Restore(path,password));
            replaced=true;
            var next=App.CreateProductionViewModel();
            DataContext=next;
            next.NavigateCommand.Execute("Respaldos");
            next.BackupStatus=result.PreviousCopyWasRaw ?
                $"Restauración completa. El archivo anterior dañado se conservó cifrado en {result.PreviousBackupPath}; puede no ser una base válida." :
                $"Restauración completa: {result.CourseCount} materias y {result.StudentCount} estudiantes. Copia anterior cifrada: {result.PreviousBackupPath}.";
            next.Message="Respaldo restaurado. Selecciona una materia para continuar.";
        }
        catch(Exception ex)
        {
            if(replaced)
            {
                DataContext=null;
                Content=new TextBlock{Margin=new(30),TextWrapping=Avalonia.Media.TextWrapping.Wrap,Text="El respaldo se restauró, pero la aplicación no pudo volver a abrir el catálogo. Cierra y abre la aplicación antes de escribir nuevos datos. Error: "+SafeError(ex)};
            }
            else model.BackupStatus=SafeError(ex);
        }
        finally{if(!replaced)model.FileBusy=false;}
    }
    private async void OpenImport()
    {
        if(observed is not {} model || model.WorkbookReader is null || model.FileBusy)return;
        model.FileBusy=true;var service=model.Service;
        try
        {
            var files=await StorageProvider.OpenFilePickerAsync(new(){Title="Importar listado de estudiantes",AllowMultiple=false,FileTypeFilter=[ExcelType]});
            if(files.Count==0)return;using var file=files[0];
            if(!file.Name.EndsWith(".xlsx",StringComparison.OrdinalIgnoreCase))throw new AcademicValidationException("Selecciona un archivo .xlsx.");
            model.Message="Leyendo Excel…";await using var input=await file.OpenReadAsync();
            var workbook=await Task.Run(()=>model.WorkbookReader.Read(input));
            var import=new StudentImportViewModel(service,workbook,file.Name);model.Message="";
            await ShowAcademicDialog(new StudentImportWindow{DataContext=import});
            if(import.ImportedCount>0)model.Message=string.IsNullOrWhiteSpace(import.Error) ?
                $"Se importaron {import.ImportedCount} estudiantes. Los datos anteriores se conservaron." : import.Error;
        }
        catch(Exception ex){model.Message=SafeError(ex);}
        finally{model.FileBusy=false;}
    }
    private async void ExportReport()
    {
        if(observed is not {} model || model.ReportExporter is null || model.FileBusy)return;
        model.FileBusy=true;
        try
        {
            var cut=model.SelectedCut;var kind=model.IsAttendance ? AcademicReportKind.Attendance : AcademicReportKind.Notes;
            var course=model.Service.Current;
            var code=new string(course.CourseCode.Select(c=>Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
            using var file=await StorageProvider.SaveFilePickerAsync(new(){Title="Exportar reporte Excel del corte",SuggestedFileName=$"{code}-{course.Period}-corte-{cut}-{kind}.xlsx",DefaultExtension="xlsx",FileTypeChoices=[ExcelType]});
            if(file is null)return;
            var path=file.TryGetLocalPath() ?? throw new AcademicValidationException("Selecciona una carpeta local para el reporte.");
            if(!path.EndsWith(".xlsx",StringComparison.OrdinalIgnoreCase))throw new AcademicValidationException("El reporte debe guardarse con extensión .xlsx.");
            var overwrite=File.Exists(path);if(overwrite && !await new ConfirmWindow("El archivo ya existe. ¿Reemplazar este reporte con los datos actuales?","Reemplazar reporte").ShowDialog<bool>(this))return;
            model.Message="Generando reporte Excel…";
            var source=model.Service.Current;
            var bytes=await Task.Run(()=>model.ReportExporter.Export(WorkspaceService.Clone(source),cut,kind));
            await Task.Run(()=>ReportFile.Save(path,bytes,overwrite));model.Message=$"Reporte Excel guardado: {file.Name}. Incluye toda la lista del corte {cut}.";
        }
        catch(Exception ex){model.Message=SafeError(ex);}
        finally{model.FileBusy=false;}
    }
    private static string SafeError(Exception ex)=>ex is AcademicValidationException or AcademicRefreshException ? ex.Message :
        $"La operación no se completó ({SafeErrorCode.From(ex)}). Revisa el archivo local e intenta de nuevo.";
}
