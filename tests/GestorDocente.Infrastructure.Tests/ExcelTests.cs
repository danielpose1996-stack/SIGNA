using System.IO.Compression;
using System.Xml.Linq;
using GestorDocente.Application;
using GestorDocente.Infrastructure;
using Xunit;
namespace GestorDocente.Infrastructure.Tests;
public class ExcelTests
{
    [Fact]
    public void Shared_strings_sparse_rows_and_numeric_zero_format_are_read_without_formulas()
    {
        using var buffer=new MemoryStream();
        using(var zip=new ZipArchive(buffer,ZipArchiveMode.Create,true))
        {
            Entry(zip,"xl/workbook.xml","<workbook xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships'><sheets><sheet name='Listado' sheetId='1' r:id='r1'/><sheet name='Otra hoja' sheetId='2' r:id='r2'/></sheets></workbook>");
            Entry(zip,"xl/_rels/workbook.xml.rels","<Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='r1' Target='worksheets/sheet1.xml'/><Relationship Id='r2' Target='/xl/worksheets/sheet2.xml'/></Relationships>");
            Entry(zip,"xl/sharedStrings.xml","<sst xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'><si><t>Nombre completo</t></si><si><r><t>Ana </t></r><r><t>Ficticia</t></r></si></sst>");
            Entry(zip,"xl/styles.xml","<styleSheet xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'><numFmts><numFmt numFmtId='164' formatCode='000000'/></numFmts><cellXfs><xf numFmtId='0'/><xf numFmtId='164'/></cellXfs></styleSheet>");
            Entry(zip,"xl/worksheets/sheet1.xml","<worksheet xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'><sheetData><row r='3'><c r='A3' t='inlineStr'><is><t>Documento</t></is></c><c r='C3' t='s'><v>0</v></c></row><row r='4'><c r='A4' s='1'><v>123</v></c><c r='C4' t='s'><v>1</v></c><c r='F4'><f>1+1</f><v>2</v></c></row></sheetData></worksheet>");
            Entry(zip,"xl/worksheets/sheet2.xml","<worksheet xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'><sheetData/></worksheet>");
        }
        buffer.Position=0;var book=new XlsxStudentReader().Read(buffer);Assert.Equal(2,book.Sheets.Count);Assert.Equal("000123",book.Sheets[0].Rows[1].Cells[0].Text);Assert.Equal("Ana Ficticia",book.Sheets[0].Rows[1].Cells[2].Text);Assert.True(book.Sheets[0].Rows[1].Cells[5].IsFormula);
    }
    [Fact]
    public void Unsafe_text_is_exported_as_literal_and_pending_grade_is_blank()
    {
        var data=DemoWorkspaceFactory.Create();data.Students=[new("safe","00123","=SUM(1,2)")];data.Cuts[0].Grades=[];
        using var zip=new ZipArchive(new MemoryStream(new XlsxReportExporter().Export(data,1,AcademicReportKind.Notes)));
        using var stream=zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();var doc=XDocument.Load(stream);XNamespace ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        Assert.Empty(doc.Descendants(ns+"f"));Assert.Contains(doc.Descendants(ns+"t"),e=>e.Value=="00123");Assert.Contains(doc.Descendants(ns+"t"),e=>e.Value=="=SUM(1,2)");Assert.Contains(doc.Descendants(ns+"c"),c=>(string?)c.Attribute("r")=="D6" && !c.Elements().Any());
        var report=AcademicReportProjection.Build(data,1,AcademicReportKind.Notes);
        var directColumn=Enumerable.Range(0,report.Columns.Count).Single(i=>report.Columns[i].Title.StartsWith("Nota Parcial",StringComparison.Ordinal));
        var directCell=XlsxReportExporter.Column(directColumn+1)+"6";
        Assert.Contains(doc.Descendants(ns+"c"),c=>(string?)c.Attribute("r")==directCell && !c.Elements().Any());
        var totalCell=XlsxReportExporter.Column(report.Columns.Count-2)+"6";
        Assert.Contains(doc.Descendants(ns+"c"),c=>(string?)c.Attribute("r")==totalCell && (string?)c.Element(ns+"v")=="0");
    }
    [Fact]
    public void File_writer_does_not_overwrite_existing_report_without_explicit_permission()
    {
        var path=Path.Combine(Path.GetTempPath(),Guid.NewGuid().ToString("N")+".xlsx");
        try{File.WriteAllBytes(path,[8,9]);Assert.Throws<IOException>(()=>ReportFile.Save(path,[1,2]));Assert.Equal(new byte[]{8,9},File.ReadAllBytes(path));ReportFile.Save(path,[1,2],true);Assert.Equal(new byte[]{1,2},File.ReadAllBytes(path));}
        finally{if(File.Exists(path))File.Delete(path);}
    }
    private static void Entry(ZipArchive zip,string path,string xml){using var stream=zip.CreateEntry(path).Open();using var writer=new StreamWriter(stream);writer.Write(xml);}
    [Fact]
    public void Notes_export_is_valid_xlsx_with_numeric_grades_text_ids_and_roundtrip()
    {
        var workspace=DemoWorkspaceFactory.Create();var before=System.Text.Json.JsonSerializer.Serialize(workspace);
        var bytes=new XlsxReportExporter().Export(workspace,1,AcademicReportKind.Notes);
        using var input=new MemoryStream(bytes);var book=new XlsxStudentReader().Read(input);
        Assert.Single(book.Sheets);var sheet=book.Sheets[0];Assert.Contains(sheet.Rows,r=>r.Cells.Values.Any(c=>c.Text==workspace.Students[0].Document));
        using var zip=new ZipArchive(new MemoryStream(bytes));using var xml=zip.GetEntry("xl/worksheets/sheet1.xml")!.Open();var doc=XDocument.Load(xml);XNamespace ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        Assert.Contains(doc.Descendants(ns+"c"),c=>(string?)c.Attribute("t")=="n");Assert.NotNull(doc.Descendants(ns+"pane").SingleOrDefault());Assert.Equal(before,System.Text.Json.JsonSerializer.Serialize(workspace));
    }
    [Fact]
    public void Attendance_export_excludes_removed_weeks_and_does_not_invent_percentage()
    {
        var data=DemoWorkspaceFactory.Create();data.Cuts[0].Weeks.RemoveAt(0);
        using var input=new MemoryStream(new XlsxReportExporter().Export(data,1,AcademicReportKind.Attendance));
        var texts=new XlsxStudentReader().Read(input).Sheets[0].Rows.SelectMany(r=>r.Cells.Values).Select(c=>c.Text).ToList();
        Assert.Equal(new[]{"Semana 1 · Clase","Semana 2 · Clase"},texts.Where(t=>t.StartsWith("Semana ")));Assert.Contains("P",texts);Assert.Contains(texts,t=>t.Contains("pendiente",StringComparison.OrdinalIgnoreCase));
    }
    [Fact]
    public void Reader_rejects_corrupt_workbook_without_side_effects()
    {
        using var input=new MemoryStream([1,2,3]);Assert.Throws<GestorDocente.Domain.AcademicValidationException>(()=>new XlsxStudentReader().Read(input));
    }
}
