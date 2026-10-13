using Avalonia;
using Avalonia.Controls;

namespace GestorDocente.Desktop.Controls;

/// <summary>Distribuye tarjetas de igual tamaño según el ancho real disponible.</summary>
public sealed class AdaptiveCardPanel : Panel
{
    private const double MinimumCardWidth=330;
    private const double Gap=16;
    private const int MaximumColumns=3;

    private static int Columns(double width)
    {
        if(double.IsInfinity(width) || double.IsNaN(width))return MaximumColumns;
        if(width>=MaximumColumns*MinimumCardWidth+(MaximumColumns-1)*Gap)return MaximumColumns;
        return width>=2*MinimumCardWidth+Gap ? 2 : 1;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width=double.IsInfinity(availableSize.Width) ? MaximumColumns*MinimumCardWidth+(MaximumColumns-1)*Gap : Math.Max(0,availableSize.Width);
        var columns=Columns(width);
        var cardWidth=Math.Max(0,(width-(columns-1)*Gap)/columns);
        var cardHeight=0d;
        foreach(var child in Children)
        {
            child.Measure(new Size(cardWidth,double.PositiveInfinity));
            cardHeight=Math.Max(cardHeight,child.DesiredSize.Height);
        }
        var rows=(Children.Count+columns-1)/columns;
        return new Size(width,rows==0 ? 0 : rows*cardHeight+(rows-1)*Gap);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var columns=Columns(finalSize.Width);
        var cardWidth=Math.Max(0,(finalSize.Width-(columns-1)*Gap)/columns);
        var cardHeight=Children.Count==0 ? 0 : Children.Max(c=>c.DesiredSize.Height);
        for(var i=0;i<Children.Count;i++)
            Children[i].Arrange(new Rect((i%columns)*(cardWidth+Gap),(i/columns)*(cardHeight+Gap),cardWidth,cardHeight));
        return finalSize;
    }
}

/// <summary>Coloca título y acciones en una línea cuando caben, o en dos líneas al reducir la ventana.</summary>
public sealed class AdaptiveCatalogHeader : Panel
{
    private const double Gap=14;
    protected override Size MeasureOverride(Size availableSize)
    {
        if(Children.Count==0)return new Size();
        foreach(var child in Children)child.Measure(availableSize);
        var title=Children[0].DesiredSize;
        var actions=Children.Count>1 ? Children[1].DesiredSize : new Size();
        var width=double.IsInfinity(availableSize.Width) ? title.Width+Gap+actions.Width : availableSize.Width;
        var horizontal=title.Width+Gap+actions.Width<=width;
        return new Size(width,horizontal ? Math.Max(title.Height,actions.Height) : title.Height+Gap+actions.Height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if(Children.Count==0)return finalSize;
        var title=Children[0];
        if(Children.Count==1){title.Arrange(new Rect(0,0,finalSize.Width,title.DesiredSize.Height));return finalSize;}
        var actions=Children[1];
        var horizontal=title.DesiredSize.Width+Gap+actions.DesiredSize.Width<=finalSize.Width;
        if(horizontal)
        {
            title.Arrange(new Rect(0,0,title.DesiredSize.Width,finalSize.Height));
            actions.Arrange(new Rect(finalSize.Width-actions.DesiredSize.Width,0,actions.DesiredSize.Width,finalSize.Height));
        }
        else
        {
            title.Arrange(new Rect(0,0,finalSize.Width,title.DesiredSize.Height));
            actions.Arrange(new Rect(0,title.DesiredSize.Height+Gap,finalSize.Width,actions.DesiredSize.Height));
        }
        return finalSize;
    }
}

/// <summary>Mantiene abrir a la izquierda y las acciones secundarias a la derecha; las pasa abajo si falta espacio.</summary>
public sealed class AdaptiveCardActions : Panel
{
    private const double Gap=8;
    protected override Size MeasureOverride(Size availableSize)
    {
        if(Children.Count==0)return new Size();
        foreach(var child in Children)child.Measure(availableSize);
        var width=double.IsInfinity(availableSize.Width) ? Children.Sum(c=>c.DesiredSize.Width)+Gap*(Children.Count-1) : availableSize.Width;
        var horizontal=Children.Sum(c=>c.DesiredSize.Width)+Gap*(Children.Count-1)<=width;
        var height=horizontal ? Children.Max(c=>c.DesiredSize.Height) :
            Children[0].DesiredSize.Height+Gap+Children.Skip(1).Select(c=>c.DesiredSize.Height).DefaultIfEmpty(0).Max();
        return new Size(width,height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if(Children.Count==0)return finalSize;
        var first=Children[0];
        var horizontal=Children.Sum(c=>c.DesiredSize.Width)+Gap*(Children.Count-1)<=finalSize.Width;
        first.Arrange(new Rect(0,0,Math.Min(first.DesiredSize.Width,finalSize.Width),first.DesiredSize.Height));
        var x=finalSize.Width;
        for(var i=Children.Count-1;i>=1;i--)
        {
            var child=Children[i];x-=child.DesiredSize.Width;
            child.Arrange(new Rect(Math.Max(0,x),horizontal ? 0 : first.DesiredSize.Height+Gap,child.DesiredSize.Width,child.DesiredSize.Height));
            x-=Gap;
        }
        return finalSize;
    }
}
