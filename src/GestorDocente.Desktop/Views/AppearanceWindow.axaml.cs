using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using GestorDocente.Desktop.ViewModels;

namespace GestorDocente.Desktop.Views;
public partial class AppearanceWindow:Window
{
    public AppearanceWindow()
    {
        InitializeComponent();Controls.WindowLayout.Configure(this);
        ColorCanvas.SelectionChanged+=(_,_)=>{if(DataContext is AppearanceViewModel model)model.SetSaturationAndBrightness(ColorCanvas.Saturation,ColorCanvas.Value);};
        foreach(var control in new InputElement[]{ColorCanvas,HueSlider})
        {
            control.AddHandler(PointerPressedEvent,(_,_)=>{if(DataContext is AppearanceViewModel model)model.BeginColorInteraction();},RoutingStrategies.Tunnel);
            control.AddHandler(PointerReleasedEvent,(_,_)=>{if(DataContext is AppearanceViewModel model)model.EndColorInteraction();},RoutingStrategies.Bubble,handledEventsToo:true);
            control.PointerCaptureLost+=(_,_)=>{if(DataContext is AppearanceViewModel model)model.EndColorInteraction();};
        }
        Closing+=(_,e)=>{if(DataContext is AppearanceViewModel model){if(model.IsBusy)e.Cancel=true;else model.CancelPreview();}};
    }
    private void Cancel(object? sender,RoutedEventArgs e)=>Close();
    private async void Save(object? sender,RoutedEventArgs e){if(DataContext is AppearanceViewModel model && await model.SaveAsync())Close();}
}
