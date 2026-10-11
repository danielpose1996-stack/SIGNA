using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using GestorDocente.Application;
using GestorDocente.Domain;
namespace GestorDocente.Infrastructure;

public sealed class XlsxReportExporter : IAcademicReportExporter
{
    private static readonly XNamespace S="http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    public byte[] Export(AcademicWorkspace workspace,int cut,AcademicReportKind kind)
    {
        var report=AcademicReportProjection.Build(workspace,cut,kind);
        if(report.Columns.Count>256)throw new AcademicValidationException("El reporte supera 256 columnas. Reduce las actividades del corte antes de exportar.");
        using var buffer=new MemoryStream();
        using(var zip=new ZipArchive(buffer,ZipArchiveMode.Create,true))
        {
            Write(zip,"[Content_Types].xml","""
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/></Types>
                """);
            Write(zip,"_rels/.rels","""
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
                """);
            Write(zip,"xl/_rels/workbook.xml.rels","""
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>
                """);
            XNamespace rel="http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            var last=Column(report.Columns.Count-1);var end=report.Rows.Count+5;
            var book=new XElement(S+"workbook",new XAttribute(XNamespace.Xmlns+"r",rel),new XElement(S+"sheets",new XElement(S+"sheet",new XAttribute("name",report.Sheet),new XAttribute("sheetId",1),new XAttribute(rel+"id","rId1"))),
                new XElement(S+"definedNames",new XElement(S+"definedName",new XAttribute("name","_xlnm.Print_Titles"),new XAttribute("localSheetId",0),$"'{report.Sheet}'!$1:$5"),new XElement(S+"definedName",new XAttribute("name","_xlnm.Print_Area"),new XAttribute("localSheetId",0),$"'{report.Sheet}'!$A$1:${last}${end}")));
            Write(zip,"xl/workbook.xml",book.ToString());Write(zip,"xl/styles.xml",Styles());
            var headerHeight=Math.Max(44,report.Columns.Max(c=>TextHeight(c.Title,c.Width)));
            var data=new XElement(S+"sheetData",Row(1,[report.Title],1,30),Row(2,[report.Context],2,28),Row(3,[report.Note],2,32),Row(4,[],0,8),Row(5,report.Columns.Select(c=>(object?)c.Title).ToList(),3,headerHeight));
            for(int i=0;i<report.Rows.Count;i++){var height=Math.Max(24,report.Rows[i].Select((v,c)=>v is string t ? TextHeight(t,report.Columns[c].Width) : 24).Max());data.Add(Row(i+6,report.Rows[i],i%2==0 ? 5 : 0,height));}
            var sheet=new XElement(S+"worksheet",new XElement(S+"dimension",new XAttribute("ref",$"A1:{last}{end}")),
                new XElement(S+"sheetViews",new XElement(S+"sheetView",new XAttribute("workbookViewId",0),new XAttribute("showGridLines",0),new XElement(S+"pane",new XAttribute("xSplit",3),new XAttribute("ySplit",5),new XAttribute("topLeftCell","D6"),new XAttribute("activePane","bottomRight"),new XAttribute("state","frozen")),new XElement(S+"selection",new XAttribute("pane","bottomRight"),new XAttribute("activeCell","D6"),new XAttribute("sqref","D6")))),
                new XElement(S+"sheetFormatPr",new XAttribute("defaultRowHeight",24)),
                new XElement(S+"cols",report.Columns.Select((c,i)=>new XElement(S+"col",new XAttribute("min",i+1),new XAttribute("max",i+1),new XAttribute("width",c.Width),new XAttribute("customWidth",1)))),data,
                new XElement(S+"autoFilter",new XAttribute("ref",$"A5:{last}{end}")),
                new XElement(S+"mergeCells",new XAttribute("count",3),Enumerable.Range(1,3).Select(i=>new XElement(S+"mergeCell",new XAttribute("ref",$"A{i}:{last}{i}")))),
                new XElement(S+"printOptions",new XAttribute("horizontalCentered",1)),new XElement(S+"pageMargins",new XAttribute("left",0.3),new XAttribute("right",0.3),new XAttribute("top",0.5),new XAttribute("bottom",0.5),new XAttribute("header",0.2),new XAttribute("footer",0.2)),new XElement(S+"pageSetup",new XAttribute("orientation","landscape"),new XAttribute("paperSize",9),new XAttribute("fitToWidth",1),new XAttribute("fitToHeight",0)));
            Write(zip,"xl/worksheets/sheet1.xml",sheet.ToString());
        }
        return buffer.ToArray();
    }
    private static XElement Row(int number,IReadOnlyList<object?> values,int style,double height)=>new(S+"row",new XAttribute("r",number),new XAttribute("ht",height),new XAttribute("customHeight",1),values.Select((v,i)=>Cell($"{Column(i)}{number}",v,style)));
    private static XElement Cell(string address,object? value,int style)
    {
        bool numeric=value is decimal or int or long;
        var cellStyle=value is int or long ? style==5 ? 8 : 7 : numeric ? style==5 ? 6 : 4 : style;
        var cell=new XElement(S+"c",new XAttribute("r",address),new XAttribute("s",cellStyle));
        if(value is null)return cell;
        if(numeric){cell.Add(new XAttribute("t","n"),new XElement(S+"v",Convert.ToString(value,CultureInfo.InvariantCulture)));}
        else{cell.Add(new XAttribute("t","inlineStr"),new XElement(S+"is",new XElement(S+"t",new XAttribute(XNamespace.Xml+"space","preserve"),Convert.ToString(value,CultureInfo.InvariantCulture))));}
        return cell;
    }
    private static void Write(ZipArchive zip,string path,string xml){using var stream=zip.CreateEntry(path,CompressionLevel.Optimal).Open();using var writer=new StreamWriter(stream,new UTF8Encoding(false));writer.Write("<?xml version=\"1.0\" encoding=\"utf-8\"?>");writer.Write(xml);}
    public static string Column(int index){string name="";for(int n=index+1;n>0;n=(n-1)/26)name=(char)('A'+(n-1)%26)+name;return name;}
    private static double TextHeight(string text,double width)=>Math.Min(400,Math.Max(24,text.Split('\n').Sum(line=>Math.Max(1,Math.Ceiling(line.Length/Math.Max(1,width-2))))*14+10));
    private static string Styles()=>"""
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <fonts count="4"><font><sz val="11"/><color rgb="FF10205C"/><name val="Calibri"/></font><font><b/><sz val="17"/><color rgb="FF10205C"/><name val="Calibri"/></font><font><sz val="10"/><color rgb="FF60779D"/><name val="Calibri"/></font><font><b/><sz val="11"/><color rgb="FFFFFFFF"/><name val="Calibri"/></font></fonts>
          <fills count="4"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FF195AAF"/><bgColor indexed="64"/></patternFill></fill><fill><patternFill patternType="solid"><fgColor rgb="FFF2F6FC"/><bgColor indexed="64"/></patternFill></fill></fills>
          <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
          <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
          <cellXfs count="9"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf><xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1" applyAlignment="1"><alignment vertical="center"/></xf><xf numFmtId="0" fontId="2" fillId="0" borderId="0" xfId="0" applyFont="1" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf><xf numFmtId="0" fontId="3" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf><xf numFmtId="2" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1" applyAlignment="1"><alignment horizontal="right" vertical="center"/></xf><xf numFmtId="0" fontId="0" fillId="3" borderId="0" xfId="0" applyFill="1" applyAlignment="1"><alignment vertical="center" wrapText="1"/></xf><xf numFmtId="2" fontId="0" fillId="3" borderId="0" xfId="0" applyNumberFormat="1" applyFill="1" applyAlignment="1"><alignment horizontal="right" vertical="center"/></xf><xf numFmtId="1" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1" applyAlignment="1"><alignment horizontal="right" vertical="center"/></xf><xf numFmtId="1" fontId="0" fillId="3" borderId="0" xfId="0" applyNumberFormat="1" applyFill="1" applyAlignment="1"><alignment horizontal="right" vertical="center"/></xf></cellXfs>
          <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
        </styleSheet>
        """;
}
