using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using GestorDocente.Application;

namespace GestorDocente.Desktop.Controls;

public sealed class AttendanceRing : Control
{
    public static readonly StyledProperty<decimal?> PercentageProperty=AvaloniaProperty.Register<AttendanceRing,decimal?>(nameof(Percentage));
    public decimal? Percentage {get=>GetValue(PercentageProperty);set=>SetValue(PercentageProperty,value);}
    public static readonly StyledProperty<IBrush> TextBrushProperty=AvaloniaProperty.Register<AttendanceRing,IBrush>(nameof(TextBrush),Brushes.Black);
    public static readonly StyledProperty<IBrush> TrackBrushProperty=AvaloniaProperty.Register<AttendanceRing,IBrush>(nameof(TrackBrush),Brushes.LightGray);
    public IBrush TextBrush{get=>GetValue(TextBrushProperty);set=>SetValue(TextBrushProperty,value);}
    public IBrush TrackBrush{get=>GetValue(TrackBrushProperty);set=>SetValue(TrackBrushProperty,value);}
    static AttendanceRing()=>AffectsRender<AttendanceRing>(PercentageProperty,TextBrushProperty,TrackBrushProperty);
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var diameter=Math.Min(Bounds.Width,Bounds.Height);
        if(diameter<20)return;
        var center=new Point(Bounds.Width/2,Bounds.Height/2);var radius=(diameter-12)/2;
        var track=new Pen(TrackBrush,8);
        var progress=new Pen(new SolidColorBrush(Color.Parse("#08A45B")),8,lineCap:PenLineCap.Round);
        context.DrawEllipse(null,track,center,radius,radius);
        if(Percentage is >0 and <100)
        {
            var angle=(double)Percentage.Value/100*Math.PI*2-Math.PI/2;
            var end=new Point(center.X+radius*Math.Cos(angle),center.Y+radius*Math.Sin(angle));
            var arc=new StreamGeometry();
            using(var path=arc.Open())
            {
                path.BeginFigure(new Point(center.X,center.Y-radius),false);
                path.ArcTo(end,new Size(radius,radius),0,Percentage.Value>50,SweepDirection.Clockwise);
                path.EndFigure(false);
            }
            context.DrawGeometry(null,progress,arc);
        }
        else if(Percentage==100)context.DrawEllipse(null,progress,center,radius,radius);
        if(diameter<70)return;
        var label=AttendanceFormatting.Percentage(Percentage).Replace(" %","%");
        var text=new FormattedText(label,CultureInfo.GetCultureInfo("es-CO"),FlowDirection.LeftToRight,
            new Typeface("Segoe UI",FontStyle.Normal,FontWeight.Bold),diameter>=90 ? 17 : 13,
            TextBrush);
        context.DrawText(text,new Point(center.X-text.Width/2,center.Y-text.Height/2));
    }
}
