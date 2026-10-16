using System.Globalization;
using GestorDocente.Domain;

namespace GestorDocente.Desktop.Styles;

public readonly record struct ColorSelection(double Hue,double Saturation,double Value)
{
    public string ToHex()
    {
        var hue=((Hue%360)+360)%360/60;var value=Math.Clamp(Value,0,1);var chroma=value*Math.Clamp(Saturation,0,1);var x=chroma*(1-Math.Abs(hue%2-1));
        var (r,g,b)=hue switch{<1=>(chroma,x,0d),<2=>(x,chroma,0d),<3=>(0d,chroma,x),<4=>(0d,x,chroma),<5=>(x,0d,chroma),_=>(chroma,0d,x)};
        byte Channel(double channel)=>(byte)Math.Round((channel+value-chroma)*255);
        return $"#{Channel(r):X2}{Channel(g):X2}{Channel(b):X2}";
    }
    public static ColorSelection FromHex(string hex,double preferredHue=0)
    {
        new InterfacePreferences(Accent:hex).Validate();
        var r=int.Parse(hex.AsSpan(1,2),NumberStyles.HexNumber)/255d;var g=int.Parse(hex.AsSpan(3,2),NumberStyles.HexNumber)/255d;var b=int.Parse(hex.AsSpan(5,2),NumberStyles.HexNumber)/255d;
        var max=Math.Max(r,Math.Max(g,b));var min=Math.Min(r,Math.Min(g,b));var delta=max-min;
        var hue=delta==0 ? preferredHue : max==r ? 60*((g-b)/delta%6) : max==g ? 60*((b-r)/delta+2) : 60*((r-g)/delta+4);
        if(hue<0)hue+=360;return new(hue,max==0 ? 0 : delta/max,max);
    }
}
