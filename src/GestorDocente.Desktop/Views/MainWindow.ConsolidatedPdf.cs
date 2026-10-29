using Avalonia.Platform.Storage;
using GestorDocente.Application;
using GestorDocente.Domain;
namespace GestorDocente.Desktop.Views;

public partial class MainWindow
{
    private async void ExportConsolidatedPdf()
    {
        if(observed is not {} model || model.ConsolidatedPdfExporter is not {} exporter || model.FileBusy)return;
        if(model.Service.IsSaving){model.Message="Espera a que termine de guardarse la nota y vuelve a generar el reporte.";return;}
        model.FileBusy=true;
        try
        {
            model.EnsureAcademicEditsSaved("generar el reporte consolidado");
            var code=new string(model.Service.Current.CourseCode.Select(c=>Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());
            using var file=await StorageProvider.SaveFilePickerAsync(new(){Title="Guardar reporte de nota consolidada",SuggestedFileName=$"consolidado-{code}.pdf",DefaultExtension="pdf",FileTypeChoices=[PdfType]});
            if(file is null)return;
            var path=file.TryGetLocalPath() ?? throw new AcademicValidationException("Selecciona una carpeta local para el PDF.");
            if(!path.EndsWith(".pdf",StringComparison.OrdinalIgnoreCase))throw new AcademicValidationException("El reporte debe guardarse con extensión .pdf.");
            var overwrite=File.Exists(path);
            if(overwrite && !await new ConfirmWindow("El archivo ya existe. ¿Reemplazarlo con el reporte consolidado actual?","Reemplazar reporte").ShowDialog<bool>(this))return;
            if(model.Service.IsSaving)throw new AcademicValidationException("Espera a que termine el guardado antes de generar el reporte.");
            var source=model.Service.Current;model.Message="Generando reporte de nota consolidada…";
            var bytes=await Task.Run(()=>exporter.Export(ConsolidatedPdfProjection.Build(WorkspaceService.Clone(source))));
            await Task.Run(()=>ReportFile.Save(path,bytes,overwrite));
            model.Message=$"Reporte consolidado guardado: {file.Name}. Los estudiantes se identifican únicamente por su nombre.";
        }
        catch(Exception ex){model.Message=SafeError(ex);}
        finally{model.FileBusy=false;}
    }
}
