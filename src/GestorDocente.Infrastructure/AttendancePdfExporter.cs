using GestorDocente.Application;
using GestorDocente.Domain;
using SkiaSharp;

namespace GestorDocente.Infrastructure;

public sealed class AttendancePdfExporter : IAttendancePdfExporter
{
    private const float PageWidth=595, PageHeight=842, Left=46, Right=549;
    private static readonly SKColor Navy=new(13,38,91), Blue=new(0,103,245), Muted=new(78,100,133);
    public byte[] Export(AcademicWorkspace workspace,int cutNumber,string studentId)
    {
        var report=AttendancePdfProjection.Build(workspace,cutNumber,studentId);
        using var stream=new MemoryStream();
        using var document=SKDocument.CreatePdf(stream,new SKDocumentPdfMetadata
        {Title=ProductIdentity.Name+" - Reporte individual de asistencia",Author=ProductIdentity.Name,Subject=ProductIdentity.FullName,Creator=ProductIdentity.Name}) ?? throw new IOException("No se pudo crear el documento PDF.");
        using var regular=new SKPaint{IsAntialias=true,Typeface=SKTypeface.Default,TextSize=10,Color=Navy};
        using var boldTypeface=SKTypeface.FromFamilyName(null,SKFontStyle.Bold);
        using var bold=new SKPaint{IsAntialias=true,Typeface=boldTypeface,TextSize=10,Color=Navy};
        using var small=new SKPaint{IsAntialias=true,Typeface=SKTypeface.Default,TextSize=9,Color=Muted};
        using var accent=new SKPaint{IsAntialias=true,Color=Blue};
        using var line=new SKPaint{IsAntialias=true,Color=new SKColor(215,228,243),StrokeWidth=1};
        using var headerFill=new SKPaint{Color=new SKColor(234,242,252)};
        using var stripeFill=new SKPaint{Color=new SKColor(248,250,253)};
        var canvas=document.BeginPage(PageWidth,PageHeight);
        canvas.DrawRect(new SKRect(0,0,PageWidth,12),accent);
        bold.TextSize=19;canvas.DrawText(ProductIdentity.Name,Left,44,bold);
        small.TextSize=10;canvas.DrawText(ProductIdentity.FullName,Left,63,small);
        bold.TextSize=17;canvas.DrawText("Reporte individual de asistencia",Left,88,bold);
        canvas.DrawLine(Left,106,Right,106,line);
        bold.TextSize=10;regular.TextSize=10;
        var y=131f;
        y=Field(canvas,"Asignatura",report.Course,Left,y,regular,bold);
        y=Field(canvas,"Código / período",$"{report.Code}  /  {report.Period}",Left,y,regular,bold);
        var location=string.Join(" / ",new[]{report.School,report.Group}.Where(value=>!string.IsNullOrWhiteSpace(value)));
        y=Field(canvas,"Escuela / grupo",location.Length==0 ? "No especificado" : location,Left,y,regular,bold);
        y=Field(canvas,"Corte",$"{report.Cut}",Left,y,regular,bold);
        y=Field(canvas,"Estudiante",report.Student,Left,y,regular,bold);
        y=Field(canvas,"Documento",report.Document,Left,y,regular,bold)+12;
        if(y+170>PageHeight-85)
        {
            Footer(canvas,report.Cut,small,line);document.EndPage();canvas=document.BeginPage(PageWidth,PageHeight);y=48;
        }
        y=Summary(canvas,report,y,regular,bold,small);
        if(y+65>PageHeight-85)
        {
            Footer(canvas,report.Cut,small,line);document.EndPage();canvas=document.BeginPage(PageWidth,PageHeight);y=48;
        }
        y=TableHeader(canvas,y,bold,headerFill);
        if(report.Lines.Count==0)
        {
            canvas.DrawText("No hay semanas de asistencia registradas en este corte.",Left+12,y+22,regular);y+=39;
        }
        foreach(var entry in report.Lines)
        {
            var rowHeight=entry.Date is null ? 28 : 42;
            if(y+rowHeight+3>PageHeight-85)
            {
                Footer(canvas,report.Cut,small,line);
                document.EndPage();canvas=document.BeginPage(PageWidth,PageHeight);y=TableHeader(canvas,48,bold,headerFill);
            }
            if(entry.Week%2!=0)canvas.DrawRect(new SKRect(Left,y,Right,y+rowHeight),stripeFill);
            canvas.DrawText($"Semana {entry.Week}",Left+12,y+18,regular);
            if(entry.Date is {} date)canvas.DrawText(date.ToString("dd/MM/yyyy",System.Globalization.CultureInfo.InvariantCulture),Left+12,y+34,small);
            canvas.DrawText(entry.Kind,Left+150,y+18,regular);
            canvas.DrawText(entry.State,Left+320,y+18,regular);
            canvas.DrawLine(Left,y+rowHeight,Right,y+rowHeight,line);y+=rowHeight;
        }
        Footer(canvas,report.Cut,small,line);document.EndPage();document.Close();
        return stream.ToArray();
    }
    private static float TableHeader(SKCanvas canvas,float y,SKPaint bold,SKPaint fill)
    {
        canvas.DrawRect(new SKRect(Left,y,Right,y+28),fill);
        bold.TextSize=10;canvas.DrawText("SEMANA",Left+12,y+18,bold);
        canvas.DrawText("TIPO",Left+150,y+18,bold);
        canvas.DrawText("REGISTRO",Left+320,y+18,bold);
        return y+28;
    }
    private static float Summary(SKCanvas canvas,AttendancePdfReport report,float y,SKPaint regular,SKPaint bold,SKPaint small)
    {
        bold.TextSize=12;canvas.DrawText("Asistencia del estudiante",Left,y+12,bold);
        var center=new SKPoint(Left+58,y+77);var radius=43f;
        using var track=new SKPaint{IsAntialias=true,Style=SKPaintStyle.Stroke,StrokeWidth=11,Color=new SKColor(223,230,238)};
        using var progress=new SKPaint{IsAntialias=true,Style=SKPaintStyle.Stroke,StrokeWidth=11,StrokeCap=SKStrokeCap.Round,Color=new SKColor(8,164,91)};
        canvas.DrawCircle(center,radius,track);
        if(report.Percentage==100)canvas.DrawCircle(center,radius,progress);
        else if(report.Percentage is >0)
            canvas.DrawArc(new SKRect(center.X-radius,center.Y-radius,center.X+radius,center.Y+radius),-90,(float)report.Percentage.Value*3.6f,false,progress);
        bold.TextSize=16;
        var label=AttendanceFormatting.Percentage(report.Percentage).Replace(" %","%");
        canvas.DrawText(label,center.X-bold.MeasureText(label)/2,center.Y+5,bold);
        var x=Left+140;
        bold.TextSize=12;canvas.DrawText(report.Status,x,y+42,bold);
        regular.TextSize=10;
        canvas.DrawText($"Presentes: {report.Summary.Present}     Tardanzas: {report.Summary.Late}",x,y+65,regular);
        canvas.DrawText($"Ausentes: {report.Summary.Absent}     Justificadas: {report.Summary.Justified}",x,y+85,regular);
        canvas.DrawText($"Asistencias (P + T): {report.Summary.Attended} / {report.Summary.Evaluable} evaluables",x,y+106,regular);
        small.TextSize=9;
        canvas.DrawText($"Sin diligenciar: {report.Unfilled}     Registros diligenciados: {report.Summary.Recorded}",x,y+125,small);
        canvas.DrawText("P y T cuentan como asistencia. J, sin clase y pendientes se excluyen del denominador.",Left,y+151,small);
        return y+173;
    }
    private static float Field(SKCanvas canvas,string label,string value,float left,float y,SKPaint regular,SKPaint bold)
    {
        bold.TextSize=10;canvas.DrawText(label+":",left,y,bold);
        var x=left+125;
        var current="";
        foreach(var rune in value.EnumerateRunes())
        {
            var character=rune.ToString();
            var candidate=current+character;
            if(current.Length>0 && regular.MeasureText(candidate)>Right-x)
            {
                canvas.DrawText(current.TrimEnd(),x,y,regular);y+=17;current=char.IsWhiteSpace(character[0]) ? "" : character;
            }
            else current=candidate;
        }
        if(current.Length>0)canvas.DrawText(current,x,y,regular);
        return y+25;
    }
    private static void Footer(SKCanvas canvas,int cut,SKPaint small,SKPaint line)
    {
        canvas.DrawLine(Left,PageHeight-55,Right,PageHeight-55,line);
        canvas.DrawText($"{ProductIdentity.Name} · Corte {cut} · Reporte generado desde los registros locales",Left,PageHeight-38,small);
    }
}
