using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using GestorDocente.Application;
using GestorDocente.Domain;
namespace GestorDocente.Infrastructure;

// Solo lectura de valores SpreadsheetML. No se ejecutan fórmulas ni vínculos externos.
public sealed class XlsxStudentReader : IStudentWorkbookReader
{
    private static readonly XNamespace S="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace R="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
    private const long MaxFile=20*1024*1024,MaxExpanded=80*1024*1024;
    public StudentWorkbook Read(Stream input)
    {
        try
        {
            using var buffer=new MemoryStream();var block=new byte[81920];int read;
            while((read=input.Read(block))>0){if(buffer.Length+read>MaxFile)throw new AcademicValidationException("El Excel supera el límite de 20 MiB.");buffer.Write(block,0,read);}
            buffer.Position=0;using var zip=new ZipArchive(buffer,ZipArchiveMode.Read);
            if(zip.Entries.Count>2048 || zip.Entries.Sum(e=>e.Length)>MaxExpanded)throw new AcademicValidationException("El contenido expandido del Excel supera el límite permitido.");
            var workbook=Xml(zip,"xl/workbook.xml");if(workbook.Root?.Name!=S+"workbook")throw new AcademicValidationException("Usa un libro .xlsx convencional sin cifrado.");
            var relationships=Xml(zip,"xl/_rels/workbook.xml.rels").Root!.Elements().Where(e=>(string?)e.Attribute("TargetMode")!="External").ToDictionary(e=>(string)e.Attribute("Id")!,e=>(string)e.Attribute("Target")!);
            var strings=zip.GetEntry("xl/sharedStrings.xml") is null ? [] : Xml(zip,"xl/sharedStrings.xml").Descendants(S+"si").Select(Text).ToArray();
            var formats=ReadFormats(zip);var sheets=new List<WorkbookSheet>();int cellCount=0;
            foreach(var sheet in workbook.Descendants(S+"sheet"))
            {
                if(!relationships.TryGetValue((string?)sheet.Attribute(R+"id") ?? "",out var target))continue;
                var path=Resolve(target);var document=Xml(zip,path);var rows=new List<WorkbookRow>();
                foreach(var row in document.Descendants(S+"sheetData").Elements(S+"row"))
                {
                    if(rows.Count>=10000)throw new AcademicValidationException("Una hoja supera 10000 filas.");
                    int number=int.TryParse((string?)row.Attribute("r"),out var n) ? n : rows.Count+1;
                    var cells=new Dictionary<int,WorkbookCell>();int next=0;
                    foreach(var cell in row.Elements(S+"c"))
                    {
                        if(++cellCount>250000)throw new AcademicValidationException("El libro supera 250000 celdas.");
                        var address=(string?)cell.Attribute("r");var column=address is null ? next : Column(address);next=column+1;
                        if(column is <0 or >=256)throw new AcademicValidationException("Solo se admiten las primeras 256 columnas.");
                        var raw=cell.Element(S+"v")?.Value ?? "";var type=(string?)cell.Attribute("t");var value=raw;
                        if(type=="s")
                        {
                            // Una celda puede conservar tipo/estilo de texto sin tener valor.
                            // Solo un índice no vacío debe resolverse en sharedStrings.
                            if(string.IsNullOrWhiteSpace(raw))value="";
                            else{if(!int.TryParse(raw,out var index) || index<0 || index>=strings.Length)throw new InvalidDataException();value=strings[index];}
                        }
                        else if(type=="inlineStr")value=Text(cell.Element(S+"is") ?? cell);
                        else if(type is null or "n")
                        {
                            if(int.TryParse((string?)cell.Attribute("s"),out var style) && formats.TryGetValue(style,out var format) && format.Length is >0 and <=80 && format.All(c=>c=='0') && decimal.TryParse(raw,NumberStyles.Float,CultureInfo.InvariantCulture,out var numeric) && numeric==decimal.Truncate(numeric))value=numeric.ToString(format,CultureInfo.InvariantCulture);
                            else if(decimal.TryParse(raw,NumberStyles.Float,CultureInfo.InvariantCulture,out var numericValue))value=numericValue.ToString("0.############################",CultureInfo.InvariantCulture);
                        }
                        cells.Add(column,new(value,cell.Element(S+"f") is not null || type=="e"));
                    }
                    if(cells.Count>0)rows.Add(new(number,cells));
                }
                sheets.Add(new((string?)sheet.Attribute("name") ?? "Hoja",rows));
            }
            if(sheets.Count==0)throw new AcademicValidationException("El libro no contiene hojas de datos legibles.");
            return new(sheets);
        }
        catch(Exception ex) when(ex is InvalidDataException or XmlException or ArgumentException or OverflowException or FormatException)
        {throw new AcademicValidationException("No se pudo leer el Excel. Puede estar dañado, cifrado o tener un formato no compatible. Guarda una copia .xlsx e intenta de nuevo.",ex);}
    }
    private static XDocument Xml(ZipArchive zip,string path)
    {
        var entry=zip.GetEntry(path) ?? throw new InvalidDataException();if(entry.Length>MaxFile)throw new AcademicValidationException("Una hoja o recurso del Excel supera 20 MiB.");
        using var stream=entry.Open();using var reader=XmlReader.Create(stream,new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=MaxFile});return XDocument.Load(reader);
    }
    private static string Text(XElement item)=>string.Concat(item.Descendants(S+"t").Where(t=>!t.Ancestors(S+"rPh").Any()).Select(t=>t.Value));
    private static string Resolve(string target)
    {
        var uri=new Uri(new Uri("https://local.invalid/xl/workbook.xml"),target.Replace('\\','/'));var path=Uri.UnescapeDataString(uri.AbsolutePath).TrimStart('/');
        if(uri.Host!="local.invalid" || !path.StartsWith("xl/",StringComparison.Ordinal) || path.Contains(".."))throw new InvalidDataException();return path;
    }
    private static int Column(string address)
    {
        int value=0;foreach(var c in address){if(c is not (>= 'A' and <= 'Z'))break;value=checked(value*26+c-'A'+1);}return value-1;
    }
    private static Dictionary<int,string> ReadFormats(ZipArchive zip)
    {
        if(zip.GetEntry("xl/styles.xml") is null)return [];
        var styles=Xml(zip,"xl/styles.xml");var custom=styles.Descendants(S+"numFmt").ToDictionary(e=>(string)e.Attribute("numFmtId")!,e=>(string?)e.Attribute("formatCode") ?? "");
        return (styles.Root?.Element(S+"cellXfs")?.Elements(S+"xf") ?? []).Select((e,i)=>(i,Format:custom.GetValueOrDefault((string?)e.Attribute("numFmtId") ?? "",""))).ToDictionary(x=>x.i,x=>x.Format);
    }
}
