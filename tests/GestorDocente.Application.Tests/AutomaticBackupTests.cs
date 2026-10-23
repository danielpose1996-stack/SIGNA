using GestorDocente.Application;
using Xunit;

namespace GestorDocente.Application.Tests;

public class AutomaticBackupTests
{
    [Fact]
    public async Task Commits_are_debounced_and_continuous_changes_have_a_deadline()
    {
        var clock=new Clock();var writer=new Writer();
        using var worker=new AutomaticBackupCoordinator(writer,()=>"folder",clock);
        clock.Advance(500);Assert.Equal(0,writer.Count);
        worker.Saved();clock.Advance(20);worker.Saved();clock.Advance(20);
        Assert.Equal(0,writer.Count);clock.Advance(10);await worker.WaitForIdleAsync();Assert.Equal(1,writer.Count);
        worker.Saved();for(var i=0;i<5;i++){clock.Advance(20);worker.Saved();}
        clock.Advance(20);await worker.WaitForIdleAsync();Assert.Equal(2,writer.Count);
    }

    [Fact]
    public async Task Pausing_waits_for_running_work_and_changes_during_copy_are_not_lost()
    {
        var clock=new Clock();using var started=new ManualResetEventSlim();using var release=new ManualResetEventSlim();
        var writer=new Writer(()=>{started.Set();Assert.True(release.Wait(TimeSpan.FromSeconds(10)));});
        using var worker=new AutomaticBackupCoordinator(writer,()=>"folder",clock);
        worker.Saved();clock.Advance(30);Assert.True(started.Wait(TimeSpan.FromSeconds(5)));
        worker.Saved();using(var pause=worker.Pause())
        {
            var idle=worker.WaitForIdleAsync();Assert.False(idle.IsCompleted);
            release.Set();await idle;clock.Advance(300);Assert.Equal(1,writer.Count);
        }
        clock.Advance(30);await worker.WaitForIdleAsync();Assert.Equal(2,writer.Count);
        await worker.StopAsync(false);worker.Saved();clock.Advance(300);Assert.Equal(2,writer.Count);
    }

    [Fact]
    public async Task Missing_folder_and_failures_are_visible_and_retry_uses_current_folder()
    {
        var clock=new Clock();string? folder=null;var fail=true;
        var writer=new Writer(()=>{if(fail)throw new IOException("detalle privado");});
        using var worker=new AutomaticBackupCoordinator(writer,()=>folder,clock);
        worker.Saved();clock.Advance(30);await worker.WaitForIdleAsync();Assert.Equal(0,writer.Count);
        Assert.Contains("carpeta",worker.Status);
        folder="first";worker.Saved();clock.Advance(30);await worker.WaitForIdleAsync();
        Assert.Contains("No se pudo",worker.Status);Assert.DoesNotContain("detalle privado",worker.Status);
        folder="second";fail=false;clock.Advance(60);await worker.WaitForIdleAsync();
        Assert.Equal("second",writer.Folder);Assert.Contains("verificado",worker.Status);
        clock.Advance(500);await worker.WaitForIdleAsync();Assert.Equal(2,writer.Count);
    }

    [Fact]
    public async Task Normal_close_flushes_once_but_discarded_model_does_not_create_empty_backup()
    {
        var clock=new Clock();var writer=new Writer();
        using(var worker=new AutomaticBackupCoordinator(writer,()=>"folder",clock))
        {worker.Saved();await worker.StopAsync(true);Assert.Equal(1,writer.Count);}
        using(var worker=new AutomaticBackupCoordinator(writer,()=>"folder",clock))
        {worker.Saved();using var pause=worker.Pause();await worker.StopAsync(false);}
        clock.Advance(300);Assert.Equal(1,writer.Count);
    }

    [Fact]
    public async Task Updating_flushes_pending_commits_while_regular_scheduling_is_paused()
    {
        var clock=new Clock();var writer=new Writer();using var worker=new AutomaticBackupCoordinator(writer,()=>"folder",clock);
        worker.Saved();using(var pause=worker.Pause())
        {await worker.FlushPausedAsync();Assert.Equal(1,writer.Count);clock.Advance(300);Assert.Equal(1,writer.Count);}
        clock.Advance(300);Assert.Equal(1,writer.Count);
    }

    private sealed class Writer(Action? action=null):IAutomaticBackupWriter
    {
        public int Count;public string? Folder;
        public LocalBackupResult Create(string folder){Interlocked.Increment(ref Count);Folder=folder;action?.Invoke();return new("copy.aulabk",1,1);}
    }
    private sealed class Clock:TimeProvider
    {
        private DateTimeOffset now=DateTimeOffset.UnixEpoch;
        private readonly List<Timer> timers=[];
        public override DateTimeOffset GetUtcNow()=>now;
        public override ITimer CreateTimer(TimerCallback callback,object? state,TimeSpan dueTime,TimeSpan period)
        {var timer=new Timer(this,callback,state);timers.Add(timer);timer.Change(dueTime,period);return timer;}
        public void Advance(int seconds)
        {
            now=now.AddSeconds(seconds);
            foreach(var timer in timers.ToArray())timer.Fire();
        }
        private sealed class Timer(Clock clock,TimerCallback callback,object? state):ITimer
        {
            private DateTimeOffset? due;
            public bool Change(TimeSpan time,TimeSpan period){due=time==Timeout.InfiniteTimeSpan ? null : clock.now+time;return true;}
            public void Fire(){if(due is {} at && clock.now>=at){due=null;callback(state);}}
            public void Dispose()=>due=null;
            public ValueTask DisposeAsync(){Dispose();return ValueTask.CompletedTask;}
        }
    }
}
