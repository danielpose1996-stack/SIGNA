using Avalonia.Controls;
using Avalonia.Interactivity;
using GestorDocente.Application;
using GestorDocente.Desktop.ViewModels;

namespace GestorDocente.Desktop.Views;

public partial class UpdatesWindow:Window
{
    public UpdatesWindow()
    {
        InitializeComponent();Controls.WindowLayout.Configure(this);
        Closed+=(_,_)=>{if(DataContext is UpdatesViewModel {CanCancel:true} model)model.Cancel();};
    }
    private async void OpenDownloads(object? sender,RoutedEventArgs e)
    {
        try{if(!await Launcher.LaunchUriAsync(new Uri(ApplicationUpdateRules.ReleasesPage)))throw new InvalidOperationException("No hay navegador disponible.");}
        catch(Exception ex){if(DataContext is UpdatesViewModel)await new ConfirmWindow($"No se pudo abrir el navegador ({SafeErrorCode.From(ex)}). Visita {ApplicationUpdateRules.ReleasesPage}.","Cerrar").ShowDialog<bool>(this);}
    }
}
