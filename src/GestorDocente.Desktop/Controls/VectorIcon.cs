using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
namespace GestorDocente.Desktop.Controls;

public sealed class VectorIcon : Control
{
    public static readonly StyledProperty<string> KindProperty=AvaloniaProperty.Register<VectorIcon,string>(nameof(Kind),"document");
    public static readonly StyledProperty<IBrush> StrokeProperty=AvaloniaProperty.Register<VectorIcon,IBrush>(nameof(Stroke),new SolidColorBrush(Color.Parse("#27528B")));
    public string Kind {get=>GetValue(KindProperty);set=>SetValue(KindProperty,value);}
    public IBrush Stroke {get=>GetValue(StrokeProperty);set=>SetValue(StrokeProperty,value);}
    static VectorIcon()=>AffectsRender<VectorIcon>(KindProperty,StrokeProperty);
    private static readonly Dictionary<string,Geometry> Shapes=new Dictionary<string,string> {
        ["home"]="M3 11 L12 3 L21 11 M5 10 L5 21 L10 21 L10 15 L14 15 L14 21 L19 21 L19 10",
        ["book"]="M12 6 C8 3 4 3 3 4 L3 20 C6 19 9 19 12 21 C15 19 18 19 21 20 L21 4 C18 3 15 3 12 6 L12 21",
        ["users"]="M9 11 A4 4 0 1 0 9 3 A4 4 0 1 0 9 11 M2 21 L2 18 C2 12 16 12 16 18 L16 21 Z M17 4 C22 4 22 11 17 11 M19 14 C22 14 23 18 22 21",
        ["document"]="M5 2 L14 2 L20 8 L20 22 L5 22 Z M14 2 L14 8 L20 8 M8 12 L16 12 M8 16 L16 16 M8 19 L13 19",
        ["grid"]="M4 3 L20 3 L20 21 L4 21 Z M4 9 L20 9 M4 15 L20 15 M10 3 L10 21 M15 3 L15 21",
        ["settings"]="M12 8 A4 4 0 1 0 12 16 A4 4 0 1 0 12 8 M9 2 L15 2 L16 6 L20 7 L22 11 L19 14 L20 18 L16 21 L12 19 L8 21 L4 18 L5 14 L2 11 L4 7 L8 6 Z",
        ["calendar"]="M4 5 L20 5 L20 21 L4 21 Z M4 10 L20 10 M8 2 L8 8 M16 2 L16 8 M8 14 L11 14 L11 17 L8 17 Z",
        ["search"]="M10 3 A7 7 0 1 0 10 17 A7 7 0 1 0 10 3 M15 15 L22 22",
        ["monitor"]="M3 4 L21 4 L21 17 L3 17 Z M12 17 L12 21 M6 21 L18 21 M12 8 L12 13",
        ["chart"]="M4 20 L4 13 M10 20 L10 8 M16 20 L16 3 M22 20 L22 11",
        ["plus"]="M12 3 L12 21 M3 12 L21 12",
        ["close"]="M5 5 L19 19 M19 5 L5 19",
        ["eye"]="M2 12 C7 3 17 3 22 12 C17 21 7 21 2 12 Z M12 8 A4 4 0 1 0 12 16 A4 4 0 1 0 12 8",
        ["trash"]="M4 6 L20 6 M8 6 L8 3 L16 3 L16 6 M6 6 L7 21 L17 21 L18 6 M10 10 L10 17 M14 10 L14 17",
        ["edit"]="M4 17 L17 4 L21 8 L8 21 L3 22 Z M14 7 L18 11",
        ["chevron"]="M8 5 L15 12 L8 19",
        ["info"]="M12 2 A10 10 0 1 0 12 22 A10 10 0 1 0 12 2 M12 10 L12 17 M12 6 L12 7"
    }.ToDictionary(x=>x.Key,x=>Geometry.Parse(x.Value));
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        using(context.PushTransform(Matrix.CreateScale(Bounds.Width/24,Bounds.Height/24)))context.DrawGeometry(null,new Pen(Stroke,1.65, lineCap:PenLineCap.Round,lineJoin:PenLineJoin.Round),Shapes.GetValueOrDefault(Kind,Shapes["document"]));
    }
}
