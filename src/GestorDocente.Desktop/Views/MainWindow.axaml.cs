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
        if(observed is not null) {observed.ConfigurationRequested-=OpenConfiguration;observed.CatalogEditorRequested-=OpenCatalog;observed.WeekEditorRequested-=OpenWeeks;observed.RecoveryRequested-=OpenRecovery;observed.ImportRequested-=OpenImport;observed.ExportRequested-=ExportReport;}
        observed=DataContext as MainViewModel;
        if(observed is not null) {observed.ConfigurationRequested+=OpenConfiguration;observed.CatalogEditorRequested+=OpenCatalog;observed.WeekEditorRequested+=OpenWeeks;observed.RecoveryRequested+=OpenRecovery;observed.ImportRequested+=OpenImport;observed.ExportRequested+=ExportReport;}
    }
    private async void OpenConfiguration()
    {
        if(observed is not null)await ShowAcademicDialog(new ConfigurationWindow {DataContext=new ConfigurationViewModel(observed.Service,observed.SelectedCut)});
    }
    private async void OpenCatalog(bool semester)
    {
        if(observed?.Catalog is not {} catalog)return;
        var vm=new CatalogEditorViewModel(catalog,observed.Home.SelectedSemester,semester);
        await ShowAcademicDialog(new CatalogEditorWindow{DataContext=vm});
        if(vm.SavedSemesterId is {} id)observed.Home.SelectedSemester=observed.Home.Semesters.FirstOrDefault(s=>s.Id==id);
    }
    private async void OpenWeeks(){if(observed is not null)await ShowAcademicDialog(new WeeksWindow{DataContext=new WeeksViewModel(observed.Service,observed.SelectedCut)});}
    private async void OpenRecovery()
    {
        if(observed is null)return;
        var model=new RecoveryViewModel(observed.Service,observed.SelectedCut);
        var dialog=new RecoveryWindow{DataContext=model};
        model.GroupRequested+=async group=>{await new ConfigurationWindow{DataContext=new ConfigurationViewModel(observed.Service,observed.SelectedCut,group)}.ShowDialog(dialog);model.Refresh();};
        await ShowAcademicDialog(dialog);
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
            if(import.ImportedCount>0)model.Message=$"Se importaron {import.ImportedCount} estudiantes. Los datos anteriores se conservaron.";
        }
        catch(Exception ex)when(IsFileError(ex)){model.Message=ex is AcademicValidationException ? ex.Message : "No se importó el archivo. Revisa su formato y permisos e intenta de nuevo.";}
        finally{model.FileBusy=false;}
    }
    private async void ExportReport()
    {
        if(observed is not {} model || model.ReportExporter is null || model.FileBusy)return;
        model.FileBusy=true;
        var snapshot=WorkspaceService.Clone(model.Service.Current);var cut=model.SelectedCut;var kind=model.IsAttendance ? AcademicReportKind.Attendance : AcademicReportKind.Notes;
        try
        {
            var code=new string(snapshot.CourseCode.Select(c=>Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
            using var file=await StorageProvider.SaveFilePickerAsync(new(){Title="Exportar reporte Excel del corte",SuggestedFileName=$"{code}-{snapshot.Period}-corte-{cut}-{kind}.xlsx",DefaultExtension="xlsx",FileTypeChoices=[ExcelType]});
            if(file is null)return;
            var path=file.TryGetLocalPath() ?? throw new AcademicValidationException("Selecciona una carpeta local para el reporte.");
            if(!path.EndsWith(".xlsx",StringComparison.OrdinalIgnoreCase))throw new AcademicValidationException("El reporte debe guardarse con extensión .xlsx.");
            var overwrite=File.Exists(path);if(overwrite && !await new ConfirmWindow("El archivo ya existe. ¿Reemplazar este reporte con los datos actuales?","Reemplazar reporte").ShowDialog<bool>(this))return;
            model.Message="Generando reporte Excel…";
            var bytes=await Task.Run(()=>model.ReportExporter.Export(snapshot,cut,kind));
            await Task.Run(()=>ReportFile.Save(path,bytes,overwrite));model.Message=$"Reporte Excel guardado: {file.Name}. Incluye toda la lista del corte {cut}.";
        }
        catch(Exception ex)when(IsFileError(ex)){model.Message=ex is AcademicValidationException ? ex.Message : "No se exportó el reporte. Revisa el destino y los permisos e intenta de nuevo.";}
        finally{model.FileBusy=false;}
    }
    private static bool IsFileError(Exception ex)=>ex is AcademicValidationException or IOException or UnauthorizedAccessException or System.Data.Common.DbException or System.Text.Json.JsonException;
}
