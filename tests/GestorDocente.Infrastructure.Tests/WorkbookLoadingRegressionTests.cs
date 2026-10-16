using System.IO.Compression;
using GestorDocente.Application;
using GestorDocente.Domain;
using GestorDocente.Infrastructure;
using Xunit;
namespace GestorDocente.Infrastructure.Tests;

public class WorkbookLoadingRegressionTests
{
    [Theory]
    [InlineData("")] [InlineData("<v/>")] [InlineData("<v> </v>")]
    public void Shared_string_cells_without_a_value_are_empty_instead_of_corrupt(string value)
    {
        using var input=Book($"<c r='C2' t='s'>{value}</c><c r='D2' t='s'><f>1+1</f>{value}</c>");
        var workbook=new ExcelStudentReader().Read(input);var sheet=Assert.Single(workbook.Sheets);
        var student=sheet.Rows.Single(r=>r.Number==2);Assert.Empty(student.Cells[2].Text);Assert.Empty(student.Cells[3].Text);Assert.True(student.Cells[3].IsFormula);
        var mapping=StudentImportDetector.Detect(sheet)!;var row=Assert.Single(StudentImportPreview.Build(sheet,mapping.HeaderRow,mapping.DocumentColumn,mapping.NameColumns,[]));
        Assert.True(row.CanImport);Assert.Equal("00012345",row.Document);Assert.Equal("Ana Ficticia",row.Name);
    }
    [Theory]
    [InlineData("not-an-index")] [InlineData("-1")] [InlineData("99999")]
    public void Nonempty_invalid_shared_string_indices_still_reject_the_file(string value)
    {
        using var input=Book($"<c r='C2' t='s'><v>{value}</v></c>");
        Assert.Throws<AcademicValidationException>(()=>new ExcelStudentReader().Read(input));
    }
    [Fact]
    public void Workbook_can_be_read_while_another_program_has_write_access_without_modifying_it()
    {
        var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")+".xlsx");using var book=Book("");var bytes=book.ToArray();
        try
        {
            File.WriteAllBytes(path,bytes);
            using(var editor=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.ReadWrite))
            {
                if(OperatingSystem.IsWindows())Assert.Throws<IOException>(()=>File.OpenRead(path));
                using var input=StudentWorkbookFile.OpenRead(path);Assert.True(input.CanRead);Assert.False(input.CanWrite);
                Assert.Single(new ExcelStudentReader().Read(input).Sheets);var observed=new byte[bytes.Length];editor.Position=0;editor.ReadExactly(observed);Assert.Equal(bytes,observed);
            }
            using var exclusive=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None); // No quedó ningún handle pendiente.
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }
    [Fact]
    public void Exclusive_lock_has_a_clear_error_and_keeps_the_original_exception()
    {
        var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")+".xlsx");
        try
        {
            File.WriteAllBytes(path,[1,2,3]);
            using var editor=new FileStream(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None);
            var error=Assert.Throws<AcademicValidationException>(()=>StudentWorkbookFile.OpenRead(path));Assert.Contains("bloqueado",error.Message);Assert.IsType<IOException>(error.InnerException);
        }
        finally{if(File.Exists(path))File.Delete(path);}
    }
    private static MemoryStream Book(string extraCells)
    {
        var buffer=new MemoryStream();using(var zip=new ZipArchive(buffer,ZipArchiveMode.Create,true))
        {
            Xml("xl/workbook.xml","<workbook xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships'><sheets><sheet name='Listado ficticio' sheetId='1' r:id='r1'/></sheets></workbook>");
            Xml("xl/_rels/workbook.xml.rels","<Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='r1' Target='worksheets/sheet1.xml'/></Relationships>");
            Xml("xl/sharedStrings.xml","<sst xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'><si><t>Ana Ficticia</t></si></sst>");
            Xml("xl/worksheets/sheet1.xml","<worksheet xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'><sheetData><row r='1'><c r='A1' t='inlineStr'><is><t>Documento</t></is></c><c r='B1' t='inlineStr'><is><t>Nombre completo</t></is></c></row><row r='2'><c r='A2' t='inlineStr'><is><t>00012345</t></is></c><c r='B2' t='s'><v>0</v></c>"+extraCells+"</row></sheetData></worksheet>");
            void Xml(string name,string text){using var writer=new StreamWriter(zip.CreateEntry(name).Open());writer.Write(text);}
        }
        buffer.Position=0;return buffer;
    }
}
