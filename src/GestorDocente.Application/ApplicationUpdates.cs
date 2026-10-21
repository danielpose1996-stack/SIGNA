namespace GestorDocente.Application;

public sealed record AvailableApplicationUpdate(string Version,bool CanInstall);

public interface IApplicationUpdater
{
    string CurrentVersion {get;}
    bool IsManagedInstallation {get;}
    Task<AvailableApplicationUpdate?> CheckAsync(CancellationToken cancellationToken=default);
    Task DownloadAsync(AvailableApplicationUpdate update,IProgress<int> progress,CancellationToken cancellationToken=default);
    Task PrepareRestartAsync(AvailableApplicationUpdate update,CancellationToken cancellationToken=default);
}

public interface IUpdateSafetyCheck
{
    Task VerifyAsync(CancellationToken cancellationToken=default);
}

public enum ApplicationUpdateFailure { Offline,FeedUnavailable,InvalidPackage,NotManaged,Busy,UnsafeData }
public sealed class ApplicationUpdateException(ApplicationUpdateFailure failure,string message,Exception? inner=null):Exception(message,inner)
{
    public ApplicationUpdateFailure Failure {get;}=failure;
}

public static class ApplicationUpdateRules
{
    public const string Repository="https://github.com/danielpose1996-stack/SIGNA";
    public const string ReleasesPage=Repository+"/releases/latest";
    public static bool IsNewStableVersion(string current,string candidate)=>
        ParseStable(current) is {} installed && ParseStable(candidate) is {} offered && offered>installed;
    public static Version? ParseStable(string value)
    {
        var parts=value.Split('.');
        return parts.Length==3 && parts.All(p=>p.Length>0 && (p.Length==1 || p[0]!='0') && p.All(char.IsAsciiDigit)) &&
            Version.TryParse(value,out var version) ? version : null;
    }
    public static string Channel(string os,string architecture)=>(os,architecture) switch
    {
        ("win","x64") or ("osx","x64" or "arm64") or ("linux","x64" or "arm64")=>$"{os}-{architecture}",
        _=>throw new NotSupportedException("Esta plataforma todavía no dispone de un canal de actualizaciones de SIGNA.")
    };
}
