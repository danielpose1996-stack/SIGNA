using Avalonia.Threading;

namespace GestorDocente.Desktop.ViewModels;

/// <summary>Agrupa trabajo visual pendiente y permite confirmarlo o cancelarlo explícitamente.</summary>
public sealed class DeferredUiAction
{
    private readonly DispatcherTimer timer;
    private readonly Action action;
    private readonly bool restart;
    private bool pending;
    public DeferredUiAction(TimeSpan interval,Action action,bool restart=true)
    {
        this.action=action;this.restart=restart;
        timer=new DispatcherTimer{Interval=interval};timer.Tick+=(_,_)=>Flush();
    }
    public void Schedule()
    {
        pending=true;
        if(restart)timer.Stop();
        if(!timer.IsEnabled)timer.Start();
    }
    public void Flush()
    {
        timer.Stop();if(!pending)return;pending=false;action();
    }
    public void Cancel(){timer.Stop();pending=false;}
}
