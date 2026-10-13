using Avalonia.Controls;
using Avalonia.Interactivity;
using GestorDocente.Desktop.ViewModels;

namespace GestorDocente.Desktop.Views;
public partial class AppearanceWindow:Window
{
    public AppearanceWindow(){InitializeComponent();Controls.WindowLayout.Configure(this);Closing+=(_,e)=>{if(DataContext is AppearanceViewModel{IsBusy:true})e.Cancel=true;};}
    private void Cancel(object? sender,RoutedEventArgs e)=>Close();
    private async void Save(object? sender,RoutedEventArgs e){if(DataContext is AppearanceViewModel model && await model.SaveAsync())Close();}
}
