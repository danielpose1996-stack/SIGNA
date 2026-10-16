using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml;
using ExcelDataReader;
using ExcelDataReader.Exceptions;
using GestorDocente.Application;
using GestorDocente.Domain;
namespace GestorDocente.Infrastructure;

/// <summary>Lector local sin automatización de Excel, ejecución de macros ni evaluación de fórmulas.</summary>
public sealed class ExcelStudentReader:IStudentWorkbookReader
{
    private const long MaxFile=20*1024*1024,MaxExpanded=80*1024*1024;
    static ExcelStudentReader()=>Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    public StudentWorkbook Read(Stream input)
    {
        try
        {
            using var buffer=new MemoryStream();var block=new byte[81920];int read;
            while((read=input.Read(block))>0)
            {if(buffer.Length+read>MaxFile)throw new AcademicValidationException("El Excel supera el límite de 20 MiB.");buffer.Write(block,0,read);}
            if(buffer.Length<8)throw new InvalidDataException();
            buffer.Position=0;
            if(buffer.GetBuffer()[0]==0x50 && buffer.GetBuffer()[1]==0x4b)
            {
                bool xml;
                using(var zip=new ZipArchive(buffer,ZipArchiveMode.Read,true))
                {
                    if(zip.Entries.Count>2048 || zip.Entries.Sum(e=>e.Length)>MaxExpanded)throw new AcademicValidationException("El contenido expandido del Excel supera el límite permitido.");
                    if(zip.Entries.Any(e=>e.Length>MaxFile))throw new AcademicValidationException("Una hoja o recurso del Excel supera 20 MiB.");
                    xml=zip.GetEntry("xl/workbook.xml") is not null;
                    if(!xml && zip.GetEntry("xl/workbook.bin") is null)throw new InvalidDataException();
                    if(!xml)
                        foreach(var entry in zip.Entries.Where(IsBinaryData))ValidateBinaryRecords(entry);
                }
                buffer.Position=0;
                // Conserva el control de fórmulas, ceros iniciales y XML seguro de XLSX también para XLSM.
                if(xml)return new XlsxStudentReader().Read(buffer);
            }
            using var reader=ExcelReaderFactory.CreateReader(buffer,new(){LeaveOpen=true,SinglePassMode=true});
            if(reader.ResultsCount<=0)throw new InvalidDataException();
            var sheets=new List<WorkbookSheet>();var cellsRead=0;
            do
            {
                if(sheets.Count>=256)throw new AcademicValidationException("El Excel supera el límite de 256 hojas.");
                var rows=new List<WorkbookRow>();var number=0;
                while(reader.Read())
                {
                    if(++number>10000)throw new AcademicValidationException("Una hoja supera 10000 filas.");
                    if(reader.FieldCount>256)throw new AcademicValidationException("Solo se admiten las primeras 256 columnas.");
                    var cells=new Dictionary<int,WorkbookCell>();
                    for(var column=0;column<reader.FieldCount;column++)
                    {
                        var value=reader.GetValue(column);var error=reader.GetCellError(column) is not null;
                        if(value is null or DBNull && !error)continue;
                        if(++cellsRead>250000)throw new AcademicValidationException("El libro supera 250000 celdas.");
                        cells.Add(column,new(error ? "Error de celda" : Format(value,reader.GetNumberFormatString(column)),error));
                    }
                    if(cells.Count>0)rows.Add(new(number,cells));
                }
                sheets.Add(new(reader.Name ?? $"Hoja {sheets.Count+1}",rows));
            }while(reader.NextResult());
            if(sheets.Count==0)throw new AcademicValidationException("El libro no contiene hojas de datos legibles.");
            return new(sheets,"En XLS y XLSB se leen los valores guardados en el libro, sin recalcular fórmulas. Revisa los nombres, documentos y contactos de la vista previa antes de importar.");
        }
        catch(Exception ex)when(ex is ExcelReaderException or InvalidDataException or EndOfStreamException or XmlException or ArgumentException or OverflowException or FormatException or NotSupportedException or IndexOutOfRangeException)
        {throw new AcademicValidationException("No se pudo leer el Excel. El archivo puede estar dañado, protegido con contraseña o tener un formato no compatible. Usa un libro .xls, .xlsx, .xlsm o .xlsb sin cifrado; guarda una nueva copia desde Excel o LibreOffice e intenta de nuevo.",ex);}
    }
    private static bool IsBinaryData(ZipArchiveEntry entry)
    {
        var name=entry.FullName.Replace('\\','/');
        return name.Equals("xl/workbook.bin",StringComparison.OrdinalIgnoreCase) ||
            name.Equals("xl/styles.bin",StringComparison.OrdinalIgnoreCase) || name.Equals("xl/sharedStrings.bin",StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("xl/worksheets/",StringComparison.OrdinalIgnoreCase) && name.EndsWith(".bin",StringComparison.OrdinalIgnoreCase);
    }
    private static void ValidateBinaryRecords(ZipArchiveEntry entry)
    {
        // El lector de terceros reserva memoria según longitudes del archivo. Verificarlas
        // antes evita reservas desproporcionadas y el silencio ante encabezados truncados.
        using var stream=new BufferedStream(entry.Open());var block=new byte[81920];long consumed=0;
        while(consumed<entry.Length)
        {
            _=Variable();var length=Variable();
            if(length>entry.Length-consumed)throw new InvalidDataException();
            var remaining=(long)length;
            while(remaining>0)
            {
                var count=stream.Read(block,0,(int)Math.Min(remaining,block.Length));if(count==0)throw new InvalidDataException();
                consumed+=count;remaining-=count;
            }
        }
        uint Variable()
        {
            uint value=0;
            for(var shift=0;shift<28;shift+=7)
            {
                var next=stream.ReadByte();if(next<0)throw new InvalidDataException();consumed++;
                value|=(uint)(next&127)<<shift;if((next&128)==0)return value;
            }
            throw new InvalidDataException();
        }
    }
    private static string Format(object? value,string? format)
    {
        if(value is double or float or int or long or decimal)
        {
            var number=Convert.ToDecimal(value,CultureInfo.InvariantCulture);
            if(format is {Length:>0 and <=80} && format.All(c=>c=='0') && number==decimal.Truncate(number))return number.ToString(format,CultureInfo.InvariantCulture);
            return number.ToString("0.############################",CultureInfo.InvariantCulture);
        }
        return Convert.ToString(value,CultureInfo.InvariantCulture) ?? "";
    }
}
