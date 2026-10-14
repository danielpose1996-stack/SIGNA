using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using GestorDocente.Desktop.ViewModels;

namespace GestorDocente.Desktop.Controls;

/// <summary>Presentation only: the original decimal value never changes.</summary>
public sealed class ResultGrade:Border
{
    public static readonly StyledProperty<decimal?> ValueProperty=AvaloniaProperty.Register<ResultGrade,decimal?>(nameof(Value));
    public static readonly StyledProperty<bool> ColorizeProperty=AvaloniaProperty.Register<ResultGrade,bool>(nameof(Colorize));
    private readonly TextBlock text=new(){FontSize=12,TextAlignment=TextAlignment.Center,VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Stretch};
    public decimal? Value{get=>GetValue(ValueProperty);set=>SetValue(ValueProperty,value);}
    public bool Colorize{get=>GetValue(ColorizeProperty);set=>SetValue(ColorizeProperty,value);}
    public ResultGrade(){Child=text;CornerRadius=new(4);Padding=new(5,3);Margin=new(3,1);MinHeight=26;Refresh();}
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if(change.Property==ValueProperty || change.Property==ColorizeProperty)Refresh();
    }
    private void Refresh(){if(text is null)return;text.Text=AcademicFormatting.Grade(Value);ToolTip.SetTip(this,text.Text);Background=new ImmutableSolidColorBrush(ColorFor(Value,Colorize));}
    public static Color ColorFor(decimal? value,bool enabled)=>!enabled || value is null ? Colors.Transparent : Color.Parse(value switch
    {
        <3m=>"#FDEBEC",<4m=>"#FFF3D8",<4.6m=>"#E5F5EB",_=>"#E5F0FF"
    });
}
