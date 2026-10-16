using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using GestorDocente.Desktop.Styles;

namespace GestorDocente.Desktop.Controls;

public sealed class HsvColorCanvas:Control
{
    public static readonly StyledProperty<double> HueProperty=AvaloniaProperty.Register<HsvColorCanvas,double>(nameof(Hue));
    public static readonly StyledProperty<double> SaturationProperty=AvaloniaProperty.Register<HsvColorCanvas,double>(nameof(Saturation));
    public static readonly StyledProperty<double> ValueProperty=AvaloniaProperty.Register<HsvColorCanvas,double>(nameof(Value),1);
    public double Hue{get=>GetValue(HueProperty);set=>SetValue(HueProperty,value);}
    public double Saturation{get=>GetValue(SaturationProperty);set=>SetValue(SaturationProperty,value);}
    public double Value{get=>GetValue(ValueProperty);set=>SetValue(ValueProperty,value);}
    static HsvColorCanvas()=>AffectsRender<HsvColorCanvas>(HueProperty,SaturationProperty,ValueProperty,IsFocusedProperty);
    public HsvColorCanvas(){Focusable=true;MinWidth=180;MinHeight=140;Cursor=new(StandardCursorType.Cross);}
    public override void Render(DrawingContext context)
    {
        var rect=new Rect(Bounds.Size);
        context.DrawRectangle(new SolidColorBrush(Color.Parse(new ColorSelection(Hue,1,1).ToHex())),null,rect,6,6);
        context.DrawRectangle(new LinearGradientBrush{StartPoint=new(0,0,RelativeUnit.Relative),EndPoint=new(1,0,RelativeUnit.Relative),GradientStops=[new(Colors.White,0),new(Colors.Transparent,1)]},null,rect,6,6);
        context.DrawRectangle(new LinearGradientBrush{StartPoint=new(0,0,RelativeUnit.Relative),EndPoint=new(0,1,RelativeUnit.Relative),GradientStops=[new(Colors.Transparent,0),new(Colors.Black,1)]},null,rect,6,6);
        var point=new Point(Math.Clamp(Saturation*Bounds.Width,7,Math.Max(7,Bounds.Width-7)),Math.Clamp((1-Value)*Bounds.Height,7,Math.Max(7,Bounds.Height-7)));
        context.DrawEllipse(null,new Pen(Brushes.Black,3),point,6,6);context.DrawEllipse(null,new Pen(Brushes.White,2),point,6,6);
        if(IsFocused)context.DrawRectangle(null,new Pen(Brushes.Navy,2),rect.Deflate(1),6,6);
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);if(!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.Pointer.Type!=PointerType.Touch)return;
        Focus();e.Pointer.Capture(this);Select(e.GetPosition(this));e.Handled=true;
    }
    protected override void OnPointerMoved(PointerEventArgs e){base.OnPointerMoved(e);if(ReferenceEquals(e.Pointer.Captured,this)){Select(e.GetPosition(this));e.Handled=true;}}
    protected override void OnPointerReleased(PointerReleasedEventArgs e){base.OnPointerReleased(e);if(ReferenceEquals(e.Pointer.Captured,this)){Select(e.GetPosition(this));e.Pointer.Capture(null);e.Handled=true;}}
    private void Select(Point point)
    {
        if(Bounds.Width<=0 || Bounds.Height<=0)return;
        SetCurrentValue(SaturationProperty,Math.Clamp(point.X/Bounds.Width,0,1));SetCurrentValue(ValueProperty,1-Math.Clamp(point.Y/Bounds.Height,0,1));
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);var step=e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? .1 : .01;
        switch(e.Key){case Key.Left:SetCurrentValue(SaturationProperty,Math.Max(0,Saturation-step));break;case Key.Right:SetCurrentValue(SaturationProperty,Math.Min(1,Saturation+step));break;case Key.Up:SetCurrentValue(ValueProperty,Math.Min(1,Value+step));break;case Key.Down:SetCurrentValue(ValueProperty,Math.Max(0,Value-step));break;default:return;}
        e.Handled=true;
    }
}

public sealed class HueColorSlider:Control
{
    public static readonly StyledProperty<double> HueProperty=AvaloniaProperty.Register<HueColorSlider,double>(nameof(Hue));
    public double Hue{get=>GetValue(HueProperty);set=>SetValue(HueProperty,value);}
    static HueColorSlider()=>AffectsRender<HueColorSlider>(HueProperty,IsFocusedProperty);
    public HueColorSlider(){Focusable=true;MinHeight=30;Cursor=new(StandardCursorType.Hand);}
    public override void Render(DrawingContext context)
    {
        var rect=new Rect(8,Bounds.Height/2-7,Math.Max(1,Bounds.Width-16),14);
        var brush=new LinearGradientBrush{StartPoint=new(0,0,RelativeUnit.Relative),EndPoint=new(1,0,RelativeUnit.Relative),GradientStops=[]};
        for(var i=0;i<=6;i++)brush.GradientStops.Add(new(Color.Parse(new ColorSelection(i*60,1,1).ToHex()),i/6d));
        context.DrawRectangle(brush,null,rect,7,7);
        var point=new Point(8+Math.Clamp(Hue/360,0,1)*rect.Width,Bounds.Height/2);
        context.DrawEllipse(Brushes.White,new Pen(IsFocused ? Brushes.Navy : Brushes.Gray,1.5),point,8,10);
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);if(!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && e.Pointer.Type!=PointerType.Touch)return;
        Focus();e.Pointer.Capture(this);Select(e.GetPosition(this));e.Handled=true;
    }
    protected override void OnPointerMoved(PointerEventArgs e){base.OnPointerMoved(e);if(ReferenceEquals(e.Pointer.Captured,this)){Select(e.GetPosition(this));e.Handled=true;}}
    protected override void OnPointerReleased(PointerReleasedEventArgs e){base.OnPointerReleased(e);if(ReferenceEquals(e.Pointer.Captured,this)){Select(e.GetPosition(this));e.Pointer.Capture(null);e.Handled=true;}}
    private void Select(Point point)=>SetCurrentValue(HueProperty,Math.Clamp((point.X-8)/Math.Max(1,Bounds.Width-16),0,1)*360);
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);var step=e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 10 : 1;
        if(e.Key is Key.Left or Key.Down)SetCurrentValue(HueProperty,Math.Max(0,Hue-step));else if(e.Key is Key.Right or Key.Up)SetCurrentValue(HueProperty,Math.Min(360,Hue+step));else return;
        e.Handled=true;
    }
}
