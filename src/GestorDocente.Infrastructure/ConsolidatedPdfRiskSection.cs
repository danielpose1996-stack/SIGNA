using GestorDocente.Application;
using GestorDocente.Domain;
using SkiaSharp;
using static GestorDocente.Infrastructure.NotesPdfExporter;
namespace GestorDocente.Infrastructure;

/// <summary>Paginated risk cards; separate from the grade table and its restricted fields.</summary>
internal static class ConsolidatedPdfRiskSection
{
    private const float Left=30,Right=762,Bottom=531,Gap=14,CardWidth=(Right-Left-Gap)/2,TextWidth=CardWidth-28;
    internal sealed record Card(ConsolidatedPdfRiskCard Data,IReadOnlyList<string> Name,IReadOnlyList<string> Email,IReadOnlyList<string> Minimum,float Height);
    internal sealed record Row(Card First,Card? Second,float Height);
    internal sealed record Page(IReadOnlyList<Row> Rows);
    internal static IReadOnlyList<Page> Plan(IReadOnlyList<ConsolidatedPdfRiskCard> cards,float top)
    {
        using var face=SKTypeface.FromFamilyName(null,SKFontStyle.Bold);
        using var name=Paint(10,new(13,38,91),face);using var body=Paint(9,new(79,99,127));
        var layouts=cards.Select(card=>
        {
            if(card.Risk is not (AcademicRisk.Critical or AcademicRisk.Delicate))throw new AcademicValidationException("Las tarjetas del reporte solo pueden mostrar estudiantes críticos o delicados.");
            var names=Wrap(card.FullName,name,TextWidth);var emails=Wrap(card.EmailLabel,body,TextWidth);var minimum=Wrap(card.MinimumLabel,body,TextWidth);
            return new Card(card,names,emails,minimum,57+names.Count*13+emails.Count*12+minimum.Count*12);
        }).ToArray();
        List<Page> pages=[];List<Row> rows=[];var y=top;
        for(var i=0;i<layouts.Length;i+=2)
        {
            var first=layouts[i];var second=i+1<layouts.Length ? layouts[i+1] : null;var height=Math.Max(first.Height,second?.Height ?? 0);
            if(top+height>Bottom)throw new AcademicValidationException("El nombre o correo de una tarjeta es demasiado extenso para el reporte. Revisa la información del estudiante.");
            if(rows.Count>0 && y+height>Bottom){pages.Add(new(rows.ToArray()));rows.Clear();y=top;}
            rows.Add(new(first,second,height));y+=height+Gap;
        }
        if(rows.Count>0)pages.Add(new(rows.ToArray()));return pages;
    }
    internal static void Render(SKDocument document,ConsolidatedPdfReport report,IReadOnlyList<Page> pages,IReadOnlyList<string> metadata,float top,int previousPages,int totalPages)
    {
        using var face=SKTypeface.FromFamilyName(null,SKFontStyle.Bold);
        using var heading=Paint(18,new(13,38,91),face);using var name=Paint(10,new(13,38,91),face);
        using var body=Paint(9,new(79,99,127));using var minimum=Paint(9,new(25,47,79));using var small=Paint(8,new(79,99,127));
        using var accent=Paint(10,new(0,103,245));using var fill=Paint(10,new(234,240,250));
        using var border=Paint(10,new(202,217,237));border.Style=SKPaintStyle.Stroke;border.StrokeWidth=.7f;
        using var critical=Paint(9,new(163,43,55),face);using var delicate=Paint(9,new(138,88,0),face);
        for(var p=0;p<pages.Count;p++)
        {
            var canvas=document.BeginPage(792,612);canvas.DrawRect(new SKRect(0,0,792,6),accent);
            heading.TextSize=18;canvas.DrawText(ProductIdentity.Name,Left,30,heading);canvas.DrawText(ProductIdentity.FullName,Left,46,body);
            heading.TextSize=16;canvas.DrawText("Atención académica",Left,72,heading);
            float y=94;foreach(var line in metadata){canvas.DrawText(line,Left,y,minimum);y+=13;}
            canvas.DrawText($"{report.RiskCards.Count} estudiantes · Casos críticos primero · Reporte de nota consolidada",Left,top-10,small);
            y=top;
            foreach(var row in pages[p].Rows)
            {
                DrawCard(row.First,Left,y,row.Height);if(row.Second is {} second)DrawCard(second,Left+CardWidth+Gap,y,row.Height);y+=row.Height+Gap;
            }
            canvas.DrawLine(Left,543,Right,543,border);
            canvas.DrawText("La mínima representa el promedio ponderado necesario en los cortes pendientes para alcanzar 3,0.",Left,556,small);
            canvas.DrawText("Los datos de contacto se incluyen únicamente en las tarjetas de estudiantes en riesgo.",Left,569,small);
            RightText(canvas,$"{previousPages+p+1} / {totalPages}",Right,601,small);document.EndPage();

            void DrawCard(Card card,float x,float cardTop,float height)
            {
                var bounds=new SKRect(x,cardTop,x+CardWidth,cardTop+height);canvas.DrawRoundRect(bounds,6,6,fill);canvas.DrawRoundRect(bounds,6,6,border);
                canvas.DrawText(card.Data.Status,x+14,cardTop+21,card.Data.Risk==AcademicRisk.Critical ? critical : delicate);
                DrawLines(canvas,card.Name,x+14,cardTop+42,13,name);
                var emailTop=cardTop+47+card.Name.Count*13;
                DrawLines(canvas,card.Email,x+14,emailTop,12,body);
                DrawLines(canvas,card.Minimum,x+14,emailTop+card.Email.Count*12+8,12,minimum);
            }
        }
    }
}
