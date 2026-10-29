using GestorDocente.Application;
using GestorDocente.Domain;
using SkiaSharp;
using static GestorDocente.Infrastructure.NotesPdfExporter;
namespace GestorDocente.Infrastructure;

public sealed class ConsolidatedPdfExporter:IConsolidatedPdfExporter
{
    private const float Width=792,Height=612,Left=30,Right=762,Bottom=531,NameWidth=204,MinimumWidth=68,FinalWidth=60,StatusWidth=112;
    private sealed record Page(int FirstCut,int CutCount,int FirstRow,int RowCount,float HeaderHeight);
    public byte[] Export(ConsolidatedPdfReport report)
    {
        if(report.Cuts.Count==0 || report.Students.Any(s=>s.Grades.Count!=report.Cuts.Count))
            throw new AcademicValidationException("Los cortes del reporte no coinciden con sus notas. Actualiza la materia e intenta de nuevo.");
        using var regular=Paint(9,new(25,47,79));using var small=Paint(8,new(79,99,127));
        using var typeface=SKTypeface.FromFamilyName(null,SKFontStyle.Bold);using var bold=Paint(9,new(13,38,91),typeface);
        using var blue=Paint(10,new(0,103,245));using var header=Paint(10,new(233,241,251));using var stripe=Paint(10,new(247,249,252));
        using var final=Paint(10,new(230,240,255));using var line=Paint(10,new(214,224,237));line.StrokeWidth=.6f;
        var metadata=Wrap("Asignatura: "+report.Course,regular,Right-Left);
        metadata.AddRange(Wrap($"Código: {report.Code} · Período: {report.Period} · Grupo: {(report.Group.Length==0 ? "No especificado" : report.Group)}",regular,Right-Left));
        var top=90+metadata.Count*13+23;
        var heights=report.Students.Select(s=>Math.Max(32,Math.Max(Wrap(s.FullName,regular,NameWidth-14).Count*12+12,
            Wrap(s.Status,regular,StatusWidth-12).Count*12+12))).ToArray();
        List<Page> pages=[];
        for(var first=0;first<report.Cuts.Count;first+=3)
        {
            var count=Math.Min(3,report.Cuts.Count-first);var width=(Right-Left-NameWidth-MinimumWidth-FinalWidth-StatusWidth)/count;
            var headerHeight=Math.Max(48,report.Cuts.Skip(first).Take(count).Select(c=>Wrap($"{c.Label}\n{c.Weight:0.##} %",bold,width-12).Count*12+14).Max());
            if(top+headerHeight+Math.Max(40,heights.DefaultIfEmpty(32).Max())>Bottom)
                throw new AcademicValidationException("Los títulos o nombres son demasiado extensos para el PDF consolidado. Simplifica los títulos del reporte.");
            if(heights.Length==0)pages.Add(new(first,count,0,0,headerHeight));
            for(var row=0;row<heights.Length;)
            {
                var start=row;float y=top+headerHeight;
                while(row<heights.Length && y+heights[row]<=Bottom){y+=heights[row];row++;}
                pages.Add(new(first,count,start,row-start,headerHeight));
            }
        }
        using var stream=new MemoryStream();
        using var document=SKDocument.CreatePdf(stream,new SKDocumentPdfMetadata{Title="SIGNA - Nota consolidada",Author=ProductIdentity.Name,Subject=ProductIdentity.FullName,Creator=ProductIdentity.Name})
            ?? throw new IOException("No se pudo crear el reporte consolidado.");
        for(var p=0;p<pages.Count;p++)
        {
            var page=pages[p];var canvas=document.BeginPage(Width,Height);canvas.DrawRect(new SKRect(0,0,Width,6),blue);
            bold.TextSize=18;canvas.DrawText(ProductIdentity.Name,Left,30,bold);small.TextSize=9;canvas.DrawText(ProductIdentity.FullName,Left,46,small);
            bold.TextSize=16;canvas.DrawText("Reporte de nota consolidada",Left,72,bold);bold.TextSize=9;small.TextSize=8;
            float y=94;foreach(var text in metadata){canvas.DrawText(text,Left,y,regular);y+=13;}
            canvas.DrawText($"{report.Students.Count} estudiantes · Cortes {page.FirstCut+1}–{page.FirstCut+page.CutCount} de {report.Cuts.Count} · Aprobación: 3,0",Left,top-10,small);
            var cutWidth=(Right-Left-NameWidth-MinimumWidth-FinalWidth-StatusWidth)/page.CutCount;
            var minimumX=Right-StatusWidth-FinalWidth-MinimumWidth;var finalX=Right-StatusWidth-FinalWidth;var statusX=Right-StatusWidth;
            canvas.DrawRect(new SKRect(Left,top,Right,top+page.HeaderHeight),header);
            DrawLines(canvas,Wrap("Nombre completo del estudiante",bold,NameWidth-14),Left+7,top+17,12,bold);
            for(var c=0;c<page.CutCount;c++){var cut=report.Cuts[page.FirstCut+c];DrawLines(canvas,Wrap($"{cut.Label}\n{cut.Weight:0.##} %",bold,cutWidth-12),Left+NameWidth+c*cutWidth+6,top+17,12,bold);}
            DrawLines(canvas,["Nota","mínima"],minimumX+7,top+17,12,bold);DrawLines(canvas,["Nota","final"],finalX+7,top+17,12,bold);
            DrawLines(canvas,["Estado","académico"],statusX+7,top+17,12,bold);y=top+page.HeaderHeight;
            canvas.DrawLine(Left,y,Right,y,line);
            if(report.Students.Count==0){canvas.DrawText("No hay estudiantes registrados en esta materia.",Left+10,y+24,regular);y+=40;}
            for(var r=page.FirstRow;r<page.FirstRow+page.RowCount;r++)
            {
                var student=report.Students[r];var height=heights[r];if(r%2==0)canvas.DrawRect(new SKRect(Left,y,Right,y+height),stripe);
                canvas.DrawRect(new SKRect(finalX,y,statusX,y+height),final);
                var names=Wrap(student.FullName,regular,NameWidth-14);DrawLines(canvas,names,Left+7,y+(height-names.Count*12)/2+10,12,regular);
                for(var c=0;c<page.CutCount;c++)CenterText(canvas,student.Grades[page.FirstCut+c],Left+NameWidth+(c+.5f)*cutWidth,y+height/2+3,regular);
                CenterText(canvas,student.Minimum,minimumX+MinimumWidth/2,y+height/2+3,regular);CenterText(canvas,student.Final,finalX+FinalWidth/2,y+height/2+3,bold);
                var status=Wrap(student.Status,regular,StatusWidth-12);DrawLines(canvas,status,statusX+6,y+(height-status.Count*12)/2+10,12,regular);
                y+=height;canvas.DrawLine(Left,y,Right,y,line);
            }
            foreach(var x in new[]{Left,Left+NameWidth,minimumX,finalX,statusX,Right}.Concat(Enumerable.Range(1,page.CutCount-1).Select(c=>Left+NameWidth+c*cutWidth)))canvas.DrawLine(x,top,x,y,line);
            canvas.DrawLine(Left,543,Right,543,line);
            canvas.DrawText("* Corte pendiente: faltan notas o configuración. Las notas vacías aportan 0,0 a la nota final, sin modificar los datos.",Left,556,small);
            canvas.DrawText("Nota mínima: promedio ponderado requerido solo en cortes pendientes; se muestra hacia arriba a 2 decimales.",Left,569,small);
            canvas.DrawText(report.WeightsPending ? "Configura los porcentajes de los cortes para que sumen 100 %." : "— Sin cálculo: ningún corte completo, sin peso restante o sin configuración. Nombre y totales se repiten en cada sección.",Left,582,small);
            RightText(canvas,$"{p+1} / {pages.Count}",Right,601,small);document.EndPage();
        }
        document.Close();return stream.ToArray();
    }
}
