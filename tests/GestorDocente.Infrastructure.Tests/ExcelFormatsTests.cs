using System.IO.Compression;
using System.Text;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Xunit;
namespace GestorDocente.Infrastructure.Tests;

public class ExcelFormatsTests
{
    [Theory]
    [InlineData(".xls")] [InlineData(".xlsx")] [InlineData(".xlsm")] [InlineData(".xlsb")]
    public void Supported_formats_detect_roster_and_preserve_contacts_and_document_text(string extension)
    {
        using var input=Create(extension);var book=new ExcelStudentReader().Read(input);
        var sheet=Assert.Single(book.Sheets);var mapping=StudentImportDetector.Detect(sheet);Assert.NotNull(mapping);
        var rows=StudentImportPreview.Build(sheet,mapping.HeaderRow,mapping.DocumentColumn,mapping.NameColumns,[],mapping.EmailColumn,mapping.PhoneColumn);
        var row=Assert.Single(rows);Assert.True(row.CanImport);Assert.Equal("00012345",row.Document);Assert.Equal("Ana Prueba Ficticia",row.Name);
        Assert.Equal("ana@example.test",row.Email);Assert.Equal("3000000000",row.Phone);
    }
    [Theory]
    [InlineData(new byte[]{1,2,3,4,5,6,7,8})]
    [InlineData(new byte[]{0x50,0x4b,3,4,0,0,0,0})]
    public void Corrupt_or_unsupported_content_has_a_clear_validation_error(byte[] bytes)
    {
        using var input=new MemoryStream(bytes);var error=Assert.Throws<AcademicValidationException>(()=>new ExcelStudentReader().Read(input));
        Assert.Contains("Excel",error.Message);Assert.Contains("formato",error.Message);
    }
    [Fact]
    public void Entry_size_limits_are_applied_before_reading_binary_workbooks()
    {
        using var buffer=new MemoryStream();using(var zip=new ZipArchive(buffer,ZipArchiveMode.Create,true))
        {using var entry=zip.CreateEntry("xl/workbook.bin").Open();entry.Write(new byte[21*1024*1024]);}
        buffer.Position=0;Assert.Throws<AcademicValidationException>(()=>new ExcelStudentReader().Read(buffer));
    }
    [Theory]
    [InlineData(new byte[]{0x83,0x01,0x80})] // Longitud incompleta: nunca aceptar como libro vacío.
    [InlineData(new byte[]{0x83,0x01,0xff,0xff,0xff,0x7f})] // Tamaño ficticio de 256 MiB en un archivo diminuto.
    public void Invalid_binary_record_lengths_are_rejected_before_library_allocations(byte[] record)
    {
        using var buffer=new MemoryStream();using(var zip=new ZipArchive(buffer,ZipArchiveMode.Create,true))
        {using var entry=zip.CreateEntry("xl/workbook.bin").Open();entry.Write(record);}
        buffer.Position=0;var error=Assert.Throws<AcademicValidationException>(()=>new ExcelStudentReader().Read(buffer));Assert.Contains("formato",error.Message);
    }
    private static readonly string[][] Data=[["Documento","Nombre completo","Correo","Celular"],["00012345","Ana Prueba Ficticia","ana@example.test","3000000000"]];
    internal static MemoryStream Create(string extension)
    {
        if(extension==".xls")return CompoundBiff();
        if(extension==".xlsb")return BinaryWorkbook();
        var buffer=new MemoryStream();using(var zip=new ZipArchive(buffer,ZipArchiveMode.Create,true))
        {
            Xml(zip,"xl/workbook.xml","<workbook xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships'><sheets><sheet name='Listado' sheetId='1' r:id='r1'/></sheets></workbook>");
            Xml(zip,"xl/_rels/workbook.xml.rels","<Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='r1' Target='worksheets/sheet1.xml'/></Relationships>");
            var rows=Data.Select((values,i)=>$"<row r='{i+1}'>"+string.Concat(values.Select((value,c)=>$"<c r='{(char)('A'+c)}{i+1}' t='inlineStr'><is><t>{value}</t></is></c>"))+"</row>");
            Xml(zip,"xl/worksheets/sheet1.xml","<worksheet xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'><sheetData>"+string.Concat(rows)+"</sheetData></worksheet>");
            if(extension==".xlsm")Xml(zip,"xl/vbaProject.bin","FICTICIO NO EJECUTABLE");
        }
        buffer.Position=0;return buffer;
    }
    private static MemoryStream Biff()
    {
        var buffer=new MemoryStream();using(var writer=new BinaryWriter(buffer,Encoding.Unicode,true))
        {
            Record(writer,0x0809,w=>{w.Write((ushort)0x0600);w.Write((ushort)0x0010);w.Write(new byte[12]);});
            Record(writer,0x0200,w=>{w.Write(0);w.Write(2);w.Write((ushort)0);w.Write((ushort)4);w.Write((ushort)0);});
            for(var r=0;r<Data.Length;r++)for(var c=0;c<Data[r].Length;c++)
                Record(writer,0x0204,w=>{w.Write((ushort)r);w.Write((ushort)c);w.Write((ushort)0);w.Write((ushort)Data[r][c].Length);w.Write((byte)1);w.Write(Encoding.Unicode.GetBytes(Data[r][c]));});
            Record(writer,0x000a,_=>{});
        }
        buffer.Position=0;return buffer;
    }
    private static MemoryStream CompoundBiff()
    {
        // Contenedor CFB real, con un Workbook BIFF8 ficticio de 4096 bytes.
        var buffer=new MemoryStream();using var source=Biff();
        var header=new byte[512];using(var writer=new BinaryWriter(new MemoryStream(header),Encoding.Unicode))
        {
            writer.Write(new byte[]{0xd0,0xcf,0x11,0xe0,0xa1,0xb1,0x1a,0xe1});writer.BaseStream.Position=24;
            writer.Write((ushort)0x003e);writer.Write((ushort)3);writer.Write((ushort)0xfffe);writer.Write((ushort)9);writer.Write((ushort)6);
            writer.BaseStream.Position=40;writer.Write(0);writer.Write(1);writer.Write(1);writer.Write(0);writer.Write(4096);
            writer.Write(0xfffffffeu);writer.Write(0);writer.Write(0xfffffffeu);writer.Write(0);writer.Write(0);
            for(var i=1;i<109;i++)writer.Write(0xffffffffu);
        }
        buffer.Write(header);using(var writer=new BinaryWriter(buffer,Encoding.Unicode,true))
        {
            writer.Write(0xfffffffdu);writer.Write(0xfffffffeu);for(uint i=2;i<10;i++)writer.Write(i==9 ? 0xfffffffeu : i+1);
            for(var i=10;i<128;i++)writer.Write(0xffffffffu);
            var directory=new byte[512];Entry(0,"Root Entry",5,1,0xfffffffeu,0);Entry(1,"Workbook",2,0xffffffffu,2,4096);writer.Write(directory);
            writer.Write(source.ToArray());writer.Write(new byte[4096-source.Length]);
            void Entry(int index,string name,byte type,uint child,uint start,long length)
            {
                using var entry=new BinaryWriter(new MemoryStream(directory),Encoding.Unicode);entry.BaseStream.Position=index*128;
                entry.Write(Encoding.Unicode.GetBytes(name+"\0"));entry.BaseStream.Position=index*128+64;
                entry.Write((ushort)((name.Length+1)*2));entry.Write(type);entry.Write((byte)1);entry.Write(0xffffffffu);entry.Write(0xffffffffu);entry.Write(child);
                entry.BaseStream.Position=index*128+116;entry.Write(start);entry.Write(length);
            }
        }
        buffer.Position=0;return buffer;
    }
    private static void Record(BinaryWriter writer,ushort type,Action<BinaryWriter> content)
    {
        using var buffer=new MemoryStream();using(var body=new BinaryWriter(buffer,Encoding.Unicode,true))content(body);
        writer.Write(type);writer.Write((ushort)buffer.Length);writer.Write(buffer.ToArray());
    }
    private static void Xml(ZipArchive zip,string name,string text){using var writer=new StreamWriter(zip.CreateEntry(name).Open());writer.Write(text);}
    private static MemoryStream BinaryWorkbook()
    {
        // Libro XLSB sintético: registros MS-XLSB, sin datos ajenos ni binarios descargados.
        var buffer=new MemoryStream();using(var zip=new ZipArchive(buffer,ZipArchiveMode.Create,true))
        {
            Xml(zip,"xl/_rels/workbook.bin.rels","<Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='r1' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet' Target='worksheets/sheet1.bin'/></Relationships>");
            using(var writer=new BinaryWriter(zip.CreateEntry("xl/workbook.bin").Open()))
            {
                BinaryRecord(writer,0x83,_=>{});
                BinaryRecord(writer,0x9c,w=>{w.Write(0);w.Write(1);WideString(w,"r1");WideString(w,"Listado");});
                BinaryRecord(writer,0x84,_=>{});
            }
            using(var writer=new BinaryWriter(zip.CreateEntry("xl/worksheets/sheet1.bin").Open()))
            {
                BinaryRecord(writer,0x81,_=>{});BinaryRecord(writer,0x94,w=>{w.Write(0);w.Write(1);w.Write(0);w.Write(3);});
                BinaryRecord(writer,0x91,_=>{});
                for(var r=0;r<Data.Length;r++)
                {
                    BinaryRecord(writer,0,w=>{w.Write(r);w.Write(new byte[8]);});
                    for(var c=0;c<Data[r].Length;c++)BinaryRecord(writer,6,w=>{w.Write(c);w.Write(0);WideString(w,Data[r][c]);});
                }
                BinaryRecord(writer,0x92,_=>{});BinaryRecord(writer,0x82,_=>{});
            }
        }
        buffer.Position=0;return buffer;
    }
    private static void WideString(BinaryWriter writer,string value){writer.Write(value.Length);writer.Write(Encoding.Unicode.GetBytes(value));}
    private static void BinaryRecord(BinaryWriter writer,int type,Action<BinaryWriter> content)
    {
        using var buffer=new MemoryStream();using(var body=new BinaryWriter(buffer,Encoding.Unicode,true))content(body);
        Variable(type);Variable((int)buffer.Length);writer.Write(buffer.ToArray());
        void Variable(int value){do{var next=value>>7;writer.Write((byte)((value&127)|(next>0 ? 128 : 0)));value=next;}while(value>0);}
    }
}
