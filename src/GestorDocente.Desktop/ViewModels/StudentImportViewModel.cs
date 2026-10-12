using System.Collections.ObjectModel;
using GestorDocente.Application;
using GestorDocente.Domain;
namespace GestorDocente.Desktop.ViewModels;

public sealed class ImportColumnViewModel(int index,string name):ObservableObject
{
    public int Index {get;}=index;
    public string Name {get;}=name;
    private bool isName;
    public bool IsName{get=>isName;set=>Set(ref isName,value);}
    public override string ToString()=>Name;
}
public sealed class ImportStudentRowViewModel(StudentImportRow row)
{
    public int Row=>row.Row;
    public string Document=>row.Document;
    public string Name=>row.Name;
    public string Status=>row.CanImport ? "Listo para importar" : row.Status;
    public bool CanImport=>row.CanImport;
}
public sealed class StudentImportViewModel:ObservableObject
{
    private readonly WorkspaceService service;
    private WorkbookSheet? selectedSheet;
    private ImportColumnViewModel? documentColumn;
    private int headerRow;
    private bool rebuilding,settingsExpanded;
    private string error="",summary="";
    private bool isSaving;
    public bool IsSaving {get=>isSaving;private set{if(Set(ref isSaving,value))Raise(nameof(CanImport));}}
    public IReadOnlyList<WorkbookSheet> Sheets{get;}
    public WorkbookSheet? SelectedSheet{get=>selectedSheet;set{if(Set(ref selectedSheet,value))DetectColumns();}}
    public int HeaderRow{get=>headerRow;set{if(Set(ref headerRow,value)){Raise(nameof(FirstDataRow));Preview();}}}
    public int FirstDataRow{get=>HeaderRow+1;set=>HeaderRow=value-1;}
    public ObservableCollection<ImportColumnViewModel> Columns{get;}=[];
    public ImportColumnViewModel? DocumentColumn{get=>documentColumn;set{if(Set(ref documentColumn,value))Preview();}}
    public ObservableRangeCollection<ImportStudentRowViewModel> Rows{get;}=[];
    public bool SettingsExpanded{get=>settingsExpanded;set=>Set(ref settingsExpanded,value);}
    public string SourceName{get;}
    public string Context=>$"{service.Current.CourseName} · {service.Current.Period} · {service.Current.GroupName}";
    public string Error{get=>error;private set=>Set(ref error,value);}
    public string Summary{get=>summary;private set=>Set(ref summary,value);}
    public bool HasPreview=>Rows.Count>0;
    public bool CanImport=>!IsSaving && ImportedCount==0 && Rows.Any(r=>r.CanImport);
    public string ImportButtonText=>$"Importar todos ({Rows.Count(r=>r.CanImport)})";
    public string MappingSummary=>DocumentColumn is null ? "No se identificaron las columnas. Abre Corregir columnas y elige nombre y documento una sola vez." : $"Documento: {ColumnLetter(DocumentColumn.Index)} · Nombre: {string.Join(" + ",Columns.Where(c=>c.IsName).Select(c=>ColumnLetter(c.Index)))} · Desde la fila {FirstDataRow}";
    public int ImportedCount{get;private set;}
    public StudentImportViewModel(WorkspaceService service,StudentWorkbook workbook,string sourceName)
    {
        this.service=service;Sheets=workbook.Sheets;SourceName=sourceName;
        SelectedSheet=Sheets.FirstOrDefault(s=>StudentImportDetector.Detect(s) is not null) ?? Sheets.FirstOrDefault();
    }
    public void Preview()
    {
        if(rebuilding)return;
        Error="";
        try
        {
            if(SelectedSheet is null)throw new AcademicValidationException("El archivo no contiene una hoja con estudiantes.");
            if(DocumentColumn is null || !Columns.Any(c=>c.IsName))
            {
                Rows.ReplaceAll([]);
                Summary="Elige las columnas de nombre y documento en Corregir columnas. Se aplican a toda la lista.";
                return;
            }
            var candidates=StudentImportPreview.Build(SelectedSheet,HeaderRow,DocumentColumn.Index,Columns.Where(c=>c.IsName).Select(c=>c.Index).ToList(),service.Current.Students);
            Rows.ReplaceAll(candidates.Select(row=>new ImportStudentRowViewModel(row)));
            Summary=$"{Rows.Count} estudiantes en el archivo · {Rows.Count(r=>r.CanImport)} listos para importar · {Rows.Count(r=>!r.CanImport)} omitidos";
        }
        catch(AcademicValidationException ex){Rows.ReplaceAll([]);Error=ex.Message;Summary="Corrige las columnas o la primera fila de estudiantes.";}
        finally{Raise(nameof(HasPreview));Raise(nameof(CanImport));Raise(nameof(ImportButtonText));Raise(nameof(MappingSummary));}
    }
    public bool Save()
    {
        try
        {
            var students=Rows.Where(r=>r.CanImport).Select(r=>new StudentImportCandidate(r.Document,r.Name)).ToList();
            service.ImportStudents(students);ImportedCount=students.Count;return true;
        }
        catch(Exception ex){Error=ImportError(ex);return false;}
    }
    public async Task<bool> SaveAsync()
    {
        if(IsSaving || ImportedCount>0)return false;
        IsSaving=true;var before=service.Current;
        try
        {
            var students=Rows.Where(r=>r.CanImport).Select(r=>new StudentImportCandidate(r.Document,r.Name)).ToList();
            await service.ImportStudentsAsync(students);ImportedCount=students.Count;Error="";return true;
        }
        catch(Exception ex)
        {
            var committed=service.Current!=before;
            if(committed)ImportedCount=Rows.Count(r=>r.CanImport);
            Error=ImportError(ex,committed);Raise(nameof(CanImport));return committed;
        }
        finally{IsSaving=false;}
    }
    private static string ImportError(Exception ex,bool committed=false)=>committed || ex is AcademicRefreshException ?
        "Los estudiantes se guardaron, pero falló el refresco. Cierra esta ventana y pulsa Actualizar." :
        ex is AcademicValidationException ? ex.Message : $"No se importaron estudiantes ({SafeErrorCode.From(ex)}). Revisa el acceso a los datos y reintenta.";
    private void DetectColumns()
    {
        rebuilding=true;
        try
        {
            var mapping=SelectedSheet is null ? null : StudentImportDetector.Detect(SelectedSheet);
            headerRow=mapping?.HeaderRow ?? 0;
            var labelRow=SelectedSheet?.Rows.FirstOrDefault(r=>r.Number==headerRow);
            var dataRows=SelectedSheet?.Rows.Where(r=>r.Number>headerRow).Take(30).ToList() ?? [];
            Columns.Clear();
            foreach(var index in (SelectedSheet?.Rows.SelectMany(r=>r.Cells.Keys).Distinct().Order().ToList() ?? []))
            {
                var label=labelRow?.Cells.GetValueOrDefault(index)?.Text;
                if(string.IsNullOrWhiteSpace(label))label=dataRows.Select(r=>r.Cells.GetValueOrDefault(index)?.Text).FirstOrDefault(t=>!string.IsNullOrWhiteSpace(t));
                if(label?.Length>42)label=label[..42]+"…";
                var column=new ImportColumnViewModel(index,$"{ColumnLetter(index)} · {label}"){IsName=mapping?.NameColumns.Contains(index)==true};
                column.PropertyChanged+=(_,e)=>{if(e.PropertyName==nameof(ImportColumnViewModel.IsName))Preview();};Columns.Add(column);
            }
            documentColumn=Columns.FirstOrDefault(c=>c.Index==mapping?.DocumentColumn);
            SettingsExpanded=mapping is null;
            Raise(nameof(DocumentColumn));Raise(nameof(HeaderRow));Raise(nameof(FirstDataRow));
        }
        finally{rebuilding=false;}
        Preview();
    }
    private static string ColumnLetter(int index){string result="";for(int n=index+1;n>0;n=(n-1)/26)result=(char)('A'+(n-1)%26)+result;return result;}
}
