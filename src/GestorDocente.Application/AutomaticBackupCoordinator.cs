using System.Diagnostics;
using GestorDocente.Domain;

namespace GestorDocente.Application;

public interface ISavedChangesObserver { void Saved(); }
public interface IAutomaticBackupWriter { LocalBackupResult Create(string folder); }

/// <summary>Receives confirmed writes only. No I/O or UI work is performed by Saved.</summary>
public sealed class AutomaticBackupCoordinator:ISavedChangesObserver,IDisposable
{
    private static readonly TimeSpan Quiet=TimeSpan.FromSeconds(30),Maximum=TimeSpan.FromMinutes(2),Retry=TimeSpan.FromMinutes(1);
    private readonly object gate=new();
    private readonly IAutomaticBackupWriter writer;
    private readonly Func<string?> folder;
    private readonly TimeProvider clock;
    private readonly ITimer timer;
    private long version,backed;
    private int pauses;
    private bool stopped,running,disposed;
    private DateTimeOffset? first;
    private Task active=Task.CompletedTask;
    private string status="Automáticos: se protegen los cambios guardados al elegir una carpeta predeterminada. Si no hay cambios, no se generan copias.";
    private string? lastError;
    public string Status {get{lock(gate)return status;}}
    public string? LastError {get{lock(gate)return lastError;}}
    public event Action? Changed;

    public AutomaticBackupCoordinator(IAutomaticBackupWriter writer,Func<string?> folder,TimeProvider? clock=null)
    {
        this.writer=writer;this.folder=folder;this.clock=clock ?? TimeProvider.System;
        timer=this.clock.CreateTimer(_=>Start(),null,Timeout.InfiniteTimeSpan,Timeout.InfiniteTimeSpan);
    }
    public void Saved()
    {
        lock(gate)
        {
            if(stopped)return;
            version++;first ??=clock.GetUtcNow();Schedule();
        }
    }
    private void Schedule(TimeSpan? delay=null)
    {
        if(stopped || running || pauses>0 || version==backed)return;
        var due=delay ?? TimeSpan.FromTicks(Math.Max(0,Math.Min(Quiet.Ticks,((first ?? clock.GetUtcNow())+Maximum-clock.GetUtcNow()).Ticks)));
        timer.Change(due,Timeout.InfiniteTimeSpan);
    }
    private void Start()
    {
        lock(gate)
        {
            if(stopped || running || pauses>0 || version==backed)return;
            var target=version;first=null;running=true;
            active=Task.Run(()=>Execute(target));
        }
    }
    private void Execute(long target)
    {
        var failed=false;
        try
        {
            var destination=folder();
            if(string.IsNullOrWhiteSpace(destination))
            {SetStatus("Automáticos pendientes: elige la carpeta predeterminada en Respaldos.");return;}
            SetStatus("Generando respaldo automático en segundo plano…");
            _=writer.Create(destination);
            lock(gate){backed=target;lastError=null;}
            SetStatus($"Respaldo automático verificado · {clock.GetLocalNow():dd/MM/yyyy HH:mm}. Se conservan los 5 más recientes.");
        }
        catch(Exception ex)
        {
            failed=true;
            Trace.TraceError("Respaldo automático: {0}, código {1:X8}",ex.GetType().Name,ex.HResult);
            var message=ex is AcademicValidationException ? "Respaldo automático pendiente: "+ex.Message :
                $"No se pudo crear o rotar el respaldo automático ({ex.GetType().Name}, {ex.HResult:X8}). Revisa la carpeta, permisos y espacio disponible. Se reintentará en un minuto; tus datos guardados se conservan.";
            lock(gate)lastError=message;
            SetStatus(message);
        }
        finally
        {
            lock(gate)
            {
                running=false;
                // A missing folder waits for a real new save/selection, without polling SQLite.
                if(failed)Schedule(Retry);
                else if(backed==target || version>target)Schedule();
            }
        }
    }
    private void SetStatus(string value)
    {
        lock(gate)status=value;
        try{Changed?.Invoke();}
        catch(Exception ex){Trace.TraceError("Estado de respaldo automático: {0}",ex.GetType().Name);}
    }
    public IDisposable Pause()
    {
        lock(gate){pauses++;if(!disposed)timer.Change(Timeout.InfiniteTimeSpan,Timeout.InfiniteTimeSpan);}
        return new PauseLease(this);
    }
    private void Resume()
    {lock(gate){pauses--;Schedule();}}
    public Task WaitForIdleAsync(){lock(gate)return active;}
    public async Task FlushPausedAsync()
    {
        await WaitForIdleAsync().ConfigureAwait(false);
        Task finishing;
        lock(gate)
        {
            if(pauses==0)throw new InvalidOperationException("Pausa el coordinador antes de preparar la actualización.");
            if(stopped || disposed || version==backed)return;
            if(!running){var target=version;running=true;active=Task.Run(()=>Execute(target));}
            finishing=active;
        }
        await finishing.ConfigureAwait(false);
    }
    public void ResumeAfterCancelledClose(){lock(gate){if(disposed)return;stopped=false;Schedule();}}
    public async Task StopAsync(bool flushPending)
    {
        Task finishing;
        lock(gate){stopped=true;if(!disposed)timer.Change(Timeout.InfiniteTimeSpan,Timeout.InfiniteTimeSpan);finishing=active;}
        await finishing.ConfigureAwait(false);
        lock(gate)
        {
            if(flushPending && !disposed && pauses==0 && version>backed)
            {var target=version;running=true;active=Task.Run(()=>Execute(target));}
            finishing=active;
        }
        await finishing.ConfigureAwait(false);
    }
    public void Dispose()
    {
        lock(gate){stopped=true;if(disposed)return;disposed=true;timer.Dispose();}
    }
    private sealed class PauseLease(AutomaticBackupCoordinator owner):IDisposable
    {
        private AutomaticBackupCoordinator? current=owner;
        public void Dispose()=>Interlocked.Exchange(ref current,null)?.Resume();
    }
}
