using Avalonia.Controls;
namespace GestorDocente.Desktop.Controls;
public partial class CutSelector : UserControl
{
    public CutSelector()
    {
        InitializeComponent();
        SizeChanged+=(_,e)=>
        {
            CutBar.Classes.Set("compact",e.NewSize.Width<1050);
            CutBar.Classes.Set("narrow",e.NewSize.Width<850);
        };
    }
}
