using System.Diagnostics;
using GestorDocente.Application;

namespace GestorDocente.Desktop.ViewModels;

public enum ApplicationUpdateState { Idle,Checking,Current,Available,Downloading,Verifying,Restarting,Offline,Error }

public sealed class UpdatesViewModel:ObservableObject
{
    private readonly IApplicationUpdater? updater;
    private readonly IUpdateSafetyCheck? safety;
    private readonly Func<IDisposable>? acquireMaintenance;
    private CancellationTokenSource? cancellation;
    private AvailableApplicationUpdate? available;
    private bool automaticallyChecked,isInstalling,dismissed;
    private int progress;
    private string status="Busca nuevas versiones estables de SIGNA.";
    private ApplicationUpdateState state;
    public string CurrentVersion=>updater?.CurrentVersion ?? typeof(UpdatesViewModel).Assembly.GetName().Version!.ToString(3);
    public string AvailableVersion=>available?.Version ?? "—";
    public bool Enabled=>updater is not null;
    public bool HasUpdate=>available is not null;
    public bool ShowNotification=>HasUpdate && !dismissed;
    public bool IsBusy=>State==ApplicationUpdateState.Checking || isInstalling;
    public bool IsInstalling=>isInstalling;
    public bool CanUseApplication=>!IsInstalling;
    public bool CanInstall=>available?.CanInstall==true && !IsBusy;
    public bool CanCancel=>IsBusy && State!=ApplicationUpdateState.Restarting;
    public string CompatibilityMessage=>!Enabled ? "Las actualizaciones se comprueban en la edición instalada, fuera de la demostración." :
        !HasUpdate || available!.CanInstall ? "" : updater!.IsManagedInstallation ?
            "Esta publicación todavía no incluye un paquete instalable para este equipo. Consulta la página oficial o inténtalo más tarde." :
            "Esta instalación requiere instalar una vez la edición preparada con Velopack. En Linux usa AppImage para actualizaciones internas; los paquetes DEB/RPM/Arch se administran con su gestor. Tus datos conservan su ubicación.";
    public string Notification=>$"SIGNA {AvailableVersion} disponible · Versión actual {CurrentVersion}";
    public string Status{get=>status;private set=>Set(ref status,value);}
    public int Progress{get=>progress;private set=>Set(ref progress,value);}
    public ApplicationUpdateState State{get=>state;private set{if(Set(ref state,value))NotifyState();}}
    public RelayCommand CheckCommand{get;}
    public RelayCommand InstallCommand{get;}
    public RelayCommand CancelCommand{get;}
    public RelayCommand DismissCommand{get;}
    public event Action? RestartRequested;

    public UpdatesViewModel(IApplicationUpdater? updater,IUpdateSafetyCheck? safety,Func<IDisposable>? acquireMaintenance=null)
    {
        this.updater=updater;this.safety=safety;this.acquireMaintenance=acquireMaintenance;
        CheckCommand=new(_=>_ = CheckAsync(),_=>Enabled && !IsBusy);
        InstallCommand=new(_=>_ = InstallAsync(),_=>CanInstall);
        CancelCommand=new(_=>Cancel(),_=>CanCancel);
        DismissCommand=new(_=>{dismissed=true;Raise(nameof(ShowNotification));});
    }
    public async Task CheckAsync(bool automatic=false)
    {
        if(!Enabled || IsBusy || automatic && automaticallyChecked)return;
        if(automatic)automaticallyChecked=true;
        using var tokenSource=new CancellationTokenSource();cancellation=tokenSource;
        State=ApplicationUpdateState.Checking;Status="Buscando actualizaciones estables…";
        try
        {
            var candidate=await updater!.CheckAsync(tokenSource.Token);
            if(candidate is not null && !ApplicationUpdateRules.IsNewStableVersion(CurrentVersion,candidate.Version))
                throw new ApplicationUpdateException(ApplicationUpdateFailure.FeedUnavailable,"La versión recibida no es una actualización estable válida.");
            available=candidate;dismissed=false;
            State=candidate is null ? ApplicationUpdateState.Current : ApplicationUpdateState.Available;
            Status=candidate is null ? $"SIGNA {CurrentVersion} está actualizado." : $"Está disponible SIGNA {candidate.Version}. Tú decides cuándo actualizar.";
        }
        catch(OperationCanceledException) when(tokenSource.IsCancellationRequested){State=ApplicationUpdateState.Idle;Status="Comprobación cancelada. Puedes continuar trabajando.";}
        catch(Exception ex){ShowError(ex);}
        finally{cancellation=null;NotifyState();}
    }
    public async Task InstallAsync()
    {
        if(!CanInstall || updater is null || available is not {} update)return;
        using var tokenSource=new CancellationTokenSource();cancellation=tokenSource;
        try
        {
            if(safety is null || RestartRequested is null)
                throw new ApplicationUpdateException(ApplicationUpdateFailure.Busy,"No se pudo preparar un reinicio seguro. Cierra este diálogo e intenta de nuevo.");
            using var maintenance=acquireMaintenance?.Invoke();
            isInstalling=true;Progress=0;State=ApplicationUpdateState.Downloading;
            Status="Descargando y verificando la actualización…";
            var reporter=new Progress<int>(value=>{if(State==ApplicationUpdateState.Downloading)Progress=Math.Clamp(value,0,100);});
            await updater.DownloadAsync(update,reporter,tokenSource.Token);Progress=100;
            tokenSource.Token.ThrowIfCancellationRequested();
            State=ApplicationUpdateState.Verifying;Status="Comprobando la integridad y ubicación de tus datos locales…";
            await safety.VerifyAsync(tokenSource.Token);tokenSource.Token.ThrowIfCancellationRequested();
            State=ApplicationUpdateState.Restarting;Status="Instalando la actualización. SIGNA se reiniciará conservando tus datos…";
            await updater.PrepareRestartAsync(update,tokenSource.Token);RestartRequested.Invoke();
        }
        catch(OperationCanceledException) when(tokenSource.IsCancellationRequested){State=ApplicationUpdateState.Available;Status="Actualización cancelada. Tus datos y la versión actual se conservan.";}
        catch(Exception ex){ShowError(ex);}
        finally
        {
            cancellation=null;if(State!=ApplicationUpdateState.Restarting)isInstalling=false;NotifyState();
        }
    }
    public void Cancel()=>cancellation?.Cancel();
    private void ShowError(Exception exception)
    {
        Trace.TraceError("Actualización SIGNA: {0}, código {1}",exception.GetType().Name,SafeErrorCode.From(exception));
        State=exception is ApplicationUpdateException {Failure:ApplicationUpdateFailure.Offline} ? ApplicationUpdateState.Offline : ApplicationUpdateState.Error;
        Status=exception is ApplicationUpdateException or GestorDocente.Domain.AcademicValidationException ? exception.Message :
            $"No se completó la actualización ({SafeErrorCode.From(exception)}). Puedes continuar con la versión actual e intentar de nuevo.";
    }
    private void NotifyState()
    {
        foreach(var property in new[]{nameof(IsBusy),nameof(IsInstalling),nameof(CanUseApplication),nameof(CanInstall),nameof(CanCancel),nameof(HasUpdate),nameof(AvailableVersion),nameof(CompatibilityMessage),nameof(ShowNotification),nameof(Notification)})Raise(property);
        CheckCommand.Refresh();InstallCommand.Refresh();CancelCommand.Refresh();
    }
}
