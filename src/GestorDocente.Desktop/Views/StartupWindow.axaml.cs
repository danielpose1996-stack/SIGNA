using Avalonia.Controls;
using GestorDocente.Desktop.ViewModels;

namespace GestorDocente.Desktop.Views;

public partial class StartupWindow:Window
{
    private bool closed,started;
    public StartupWindow(){InitializeComponent();Controls.WindowLayout.Configure(this);Closed+=(_,_)=>closed=true;}
    public async Task LoadAsync(Func<Task<Window>> load,Action<Window> activate,Func<Exception,Window> recover)
    {
        if(started || closed)return;started=true;
        try
        {
            Window next;
            try{next=await load();}
            catch(Exception ex){if(closed)return;next=recover(ex);}
            if(closed){next.Close();return;}
            activate(next);next.Show();Close();
        }
        catch(Exception ex)
        {
            if(closed)return;
            StartupProgress.IsVisible=false;
            StartupStatus.Text=$"No se pudo abrir la interfaz ({SafeErrorCode.From(ex)}). Cierra SIGNA e intenta de nuevo. Los datos no se reemplazaron.";
        }
    }
}
