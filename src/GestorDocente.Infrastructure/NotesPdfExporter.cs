using System.Globalization;
using GestorDocente.Application;
using GestorDocente.Domain;
using SkiaSharp;

namespace GestorDocente.Infrastructure;

/// <summary>Renders only the privacy-filtered academic projection.</summary>
public sealed class NotesPdfExporter:INotesPdfExporter
{
    private const float Width=792,Height=612,Left=32,Right=760,Bottom=548,NameWidth=226,FinalWidth=72;
    private static readonly CultureInfo Culture=CultureInfo.GetCultureInfo("es-CO");
    private sealed record Page(int Panel,int FirstColumn,int ColumnCount,int FirstRow,int RowCount,float TableTop,float HeaderHeight);

    public byte[] Export(NotesPdfReport report)
    {
        if(report.Students.Any(s=>s.Grades.Count!=report.Columns.Count))throw new AcademicValidationException("La tabla del reporte no coincide con sus calificaciones. Actualiza la materia.");
        using var regular=Paint(9.5f,new(25,47,79));
        using var boldTypeface=SKTypeface.FromFamilyName(null,SKFontStyle.Bold);
        using var bold=Paint(9.5f,new(13,38,91),boldTypeface);
        using var small=Paint(8,new(79,99,127));
        using var blue=Paint(10,new(0,103,245));
        using var headerFill=Paint(10,new(233,241,251));
        using var stripe=Paint(10,new(247,249,252));
        using var finalFill=Paint(10,new(230,240,255));
        using var line=Paint(10,new(214,224,237));line.StrokeWidth=.6f;
        var metadata=Metadata(report,regular);
        var tableTop=88+metadata.Count*14+24;
        var heights=report.Students.Select(s=>Math.Max(27f,Wrap(s.FullName,regular,NameWidth-16).Count*12+12)).ToArray();
        var pages=Plan(report,tableTop,heights,small);
        using var stream=new MemoryStream();
        using var document=SKDocument.CreatePdf(stream,new SKDocumentPdfMetadata{Title=$"SIGNA - Calificaciones del corte {report.Cut}",Author=ProductIdentity.Name,Subject=ProductIdentity.FullName,Creator=ProductIdentity.Name})
            ?? throw new IOException("No se pudo crear el PDF de notas.");
        for(var pageIndex=0;pageIndex<pages.Count;pageIndex++)
        {
            var page=pages[pageIndex];var canvas=document.BeginPage(Width,Height);
            canvas.DrawRect(new SKRect(0,0,Width,6),blue);
            bold.TextSize=18;canvas.DrawText(ProductIdentity.Name,Left,31,bold);
            small.TextSize=9;canvas.DrawText(ProductIdentity.FullName,Left,47,small);
            bold.TextSize=16;canvas.DrawText("Reporte de calificaciones",Left,72,bold);
            bold.TextSize=14;RightText(canvas,$"Corte {report.Cut}",Right,72,bold);bold.TextSize=9.5f;
            float y=94;
            foreach(var text in metadata){canvas.DrawText(text,Left,y,regular);y+=14;}
            small.TextSize=8;
            var panelLabel=report.Columns.Count==0 ? "Sin actividades configuradas" : $"Columnas de evaluación {page.FirstColumn+1} a {page.FirstColumn+page.ColumnCount} de {report.Columns.Count}";
            canvas.DrawText($"{report.Students.Count} estudiantes · {panelLabel}",Left,page.TableTop-10,small);
            var nameWidth=page.ColumnCount==0 ? Right-Left-FinalWidth : NameWidth;
            var gradeWidth=page.ColumnCount==0 ? 0 : (Right-Left-nameWidth-FinalWidth)/page.ColumnCount;
            y=page.TableTop;
            canvas.DrawRect(new SKRect(Left,y,Right,y+page.HeaderHeight),headerFill);
            DrawLines(canvas,Wrap("Nombre completo del estudiante",bold,nameWidth-16),Left+8,y+17,12,bold);
            for(var c=0;c<page.ColumnCount;c++)
            {
                var x=Left+nameWidth+c*gradeWidth;
                DrawLines(canvas,Wrap(report.Columns[page.FirstColumn+c].Header,small,gradeWidth-14),x+7,y+16,11,small);
            }
            DrawLines(canvas,Wrap("Nota definitiva del corte",bold,FinalWidth-12),Right-FinalWidth+6,y+17,12,bold);
            canvas.DrawLine(Left,y+page.HeaderHeight,Right,y+page.HeaderHeight,line);y+=page.HeaderHeight;
            if(report.Students.Count==0){canvas.DrawText("No hay estudiantes registrados en esta materia.",Left+10,y+24,regular);y+=40;}
            for(var r=page.FirstRow;r<page.FirstRow+page.RowCount;r++)
            {
                var student=report.Students[r];var rowHeight=heights[r];
                if(r%2==0)canvas.DrawRect(new SKRect(Left,y,Right,y+rowHeight),stripe);
                canvas.DrawRect(new SKRect(Right-FinalWidth,y,Right,y+rowHeight),finalFill);
                var names=Wrap(student.FullName,regular,nameWidth-16);
                DrawLines(canvas,names,Left+8,y+(rowHeight-names.Count*12)/2+10,12,regular);
                for(var c=0;c<page.ColumnCount;c++)
                {
                    var value=student.Grades[page.FirstColumn+c];
                    CenterText(canvas,value?.ToString("0.00",Culture) ?? "",Left+nameWidth+(c+.5f)*gradeWidth,y+rowHeight/2+3,regular);
                }
                var total=student.Total?.ToString("0.00",Culture);
                if(total is null)DrawLines(canvas,["Sin","configurar"],Right-FinalWidth+10,y+rowHeight/2-3,11,small);
                else CenterText(canvas,total,Right-FinalWidth/2,y+rowHeight/2+3,bold);
                y+=rowHeight;canvas.DrawLine(Left,y,Right,y,line);
            }
            canvas.DrawLine(Left,page.TableTop,Left,y,line);canvas.DrawLine(Right,page.TableTop,Right,y,line);
            canvas.DrawLine(Left+nameWidth,page.TableTop,Left+nameWidth,y,line);
            for(var c=1;c<=page.ColumnCount;c++)canvas.DrawLine(Left+nameWidth+c*gradeWidth,page.TableTop,Left+nameWidth+c*gradeWidth,y,line);
            canvas.DrawLine(Left,563,Right,563,line);
            canvas.DrawText("Celdas vacías: pendientes; aportan 0,0 solo en los cálculos. Subactividades: promedio simple o ponderado según configuración.",Left,577,small);
            canvas.DrawText("% del corte: actividad principal. % interno: dentro de la actividad. Nombre y definitiva se repiten por sección.",Left,590,small);
            RightText(canvas,$"{pageIndex+1} / {pages.Count}",Right,603,small);
            document.EndPage();
        }
        document.Close();return stream.ToArray();
    }
    private static List<Page> Plan(NotesPdfReport report,float tableTop,float[] heights,SKPaint small)
    {
        List<Page> pages=[];var panels=Math.Max(1,(int)Math.Ceiling(report.Columns.Count/5d));
        for(var panel=0;panel<panels;panel++)
        {
            var first=panel*5;var count=Math.Min(5,report.Columns.Count-first);var width=count==0 ? 1 : (Right-Left-NameWidth-FinalWidth)/count;
            var headerHeight=Math.Max(56,report.Columns.Skip(first).Take(count).Select(c=>Wrap(c.Header,small,width-14).Count*11+15).DefaultIfEmpty(56).Max());
            if(tableTop+headerHeight+Math.Max(40,heights.DefaultIfEmpty(27).Max())>Bottom)
                throw new AcademicValidationException("Los nombres de la asignatura o las actividades son demasiado extensos para los encabezados del PDF. Simplifica esos títulos antes de generar el reporte.");
            if(heights.Length==0){pages.Add(new(panel,first,count,0,0,tableTop,headerHeight));continue;}
            for(var row=0;row<heights.Length;)
            {
                var start=row;var y=tableTop+headerHeight;
                while(row<heights.Length && y+heights[row]<=Bottom){y+=heights[row];row++;}
                pages.Add(new(panel,first,count,start,row-start,tableTop,headerHeight));
            }
        }
        return pages;
    }
    private static List<string> Metadata(NotesPdfReport report,SKPaint paint)
    {
        var result=Wrap("Asignatura: "+report.Course,paint,Right-Left);
        result.AddRange(Wrap($"Código: {report.Code} · Período académico: {report.Period} · Grupo: {(report.Group.Length==0 ? "No especificado" : report.Group)}",paint,Right-Left));
        return result;
    }
    private static SKPaint Paint(float size,SKColor color,SKTypeface? typeface=null)=>new(){IsAntialias=true,TextSize=size,Color=color,Typeface=typeface ?? SKTypeface.Default};
    private static void CenterText(SKCanvas canvas,string value,float center,float baseline,SKPaint paint)=>canvas.DrawText(value,center-paint.MeasureText(value)/2,baseline,paint);
    private static void RightText(SKCanvas canvas,string value,float right,float baseline,SKPaint paint)=>canvas.DrawText(value,right-paint.MeasureText(value),baseline,paint);
    private static void DrawLines(SKCanvas canvas,IEnumerable<string> lines,float x,float y,float spacing,SKPaint paint){foreach(var text in lines){canvas.DrawText(text,x,y,paint);y+=spacing;}}
    private static List<string> Wrap(string value,SKPaint paint,float width)
    {
        List<string> result=[];
        foreach(var paragraph in value.Replace("\r","").Split('\n'))
        {
            var current="";
            foreach(var word in paragraph.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries))
            {
                if(current.Length>0 && paint.MeasureText(current+" "+word)>width){result.Add(current);current="";}
                foreach(var rune in word.EnumerateRunes())
                {
                    if(current.Length>0 && paint.MeasureText(current+rune)>width){result.Add(current);current="";}
                    current+=rune.ToString();
                }
                current+=" ";
            }
            result.Add(current.TrimEnd());
        }
        return result;
    }
}
