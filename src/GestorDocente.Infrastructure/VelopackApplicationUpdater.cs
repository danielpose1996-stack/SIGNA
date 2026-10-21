using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using GestorDocente.Application;
using Velopack;
using Velopack.Sources;
using Velopack.Locators;

namespace GestorDocente.Infrastructure;

/// <summary>Actualizaciones estables del repositorio oficial; nunca accede a datos académicos.</summary>
public sealed class VelopackApplicationUpdater:IApplicationUpdater
{
    private static readonly HttpClient DefaultHttp=CreateHttpClient();
    private readonly HttpClient http;
    private readonly UpdateManager manager;
    private readonly IVelopackLocator locator;
    private readonly string channel,currentVersion;
    private readonly SemaphoreSlim operation=new(1,1);
    private AvailableApplicationUpdate? offered;
    private UpdateInfo? package;
    private string? downloadedVersion;
    public string CurrentVersion=>manager.IsInstalled ? manager.CurrentVersion!.ToString() : currentVersion;
    public bool IsManagedInstallation=>manager.IsInstalled;
    public VelopackApplicationUpdater(string currentVersion,UpdateManager? manager=null,HttpClient? http=null,string? channel=null,IVelopackLocator? locator=null)
    {
        this.currentVersion=currentVersion;this.http=http ?? DefaultHttp;
        this.locator=locator ?? VelopackLocator.Current;
        this.channel=channel ?? ApplicationUpdateRules.Channel(OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux",
            RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant());
        this.manager=manager ?? new UpdateManager(new GithubSource(ApplicationUpdateRules.Repository,null,false,new BoundedDownloader()),
            new UpdateOptions{ExplicitChannel=this.channel,AllowVersionDowngrade=false,MaximumDeltasBeforeFallback=-1},this.locator);
    }
    public async Task<AvailableApplicationUpdate?> CheckAsync(CancellationToken cancellationToken=default)
    {
        await operation.WaitAsync(cancellationToken);
        try
        {
            using var response=await http.GetAsync("https://api.github.com/repos/danielpose1996-stack/SIGNA/releases/latest",cancellationToken);
            if(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.TooManyRequests)
                throw new ApplicationUpdateException(ApplicationUpdateFailure.FeedUnavailable,"GitHub limitó temporalmente las consultas. Puedes seguir trabajando e intentar más tarde.");
            if(response.StatusCode==HttpStatusCode.NotFound)
                throw new ApplicationUpdateException(ApplicationUpdateFailure.FeedUnavailable,"Todavía no hay una versión estable publicada para comprobar.");
            response.EnsureSuccessStatusCode();
            using var json=JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));var release=json.RootElement;
            if(release.GetProperty("draft").GetBoolean() || release.GetProperty("prerelease").GetBoolean())
                throw new ApplicationUpdateException(ApplicationUpdateFailure.FeedUnavailable,"El repositorio no devolvió una versión estable válida.");
            var version=(release.GetProperty("tag_name").GetString() ?? "").TrimStart('v');
            if(ApplicationUpdateRules.ParseStable(version) is null)
                throw new ApplicationUpdateException(ApplicationUpdateFailure.FeedUnavailable,"La versión publicada no tiene un formato estable compatible.");
            if(!ApplicationUpdateRules.IsNewStableVersion(CurrentVersion,version)){offered=null;package=null;downloadedVersion=null;return null;}
            var hasFeed=release.GetProperty("assets").EnumerateArray().Any(a=>a.GetProperty("name").GetString()==$"releases.{channel}.json");
            UpdateInfo? candidate=null;
            if(IsManagedInstallation && hasFeed)
            {
                candidate=await manager.CheckForUpdatesAsync().WaitAsync(TimeSpan.FromSeconds(25),cancellationToken);
                if(candidate?.TargetFullRelease.Version.ToString()!=version)
                    throw new ApplicationUpdateException(ApplicationUpdateFailure.FeedUnavailable,"La versión publicada no tiene un paquete válido para este equipo. Intenta más tarde.");
            }
            package=candidate;offered=new(version,candidate is not null);downloadedVersion=null;return offered;
        }
        catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested){throw;}
        catch(Exception ex) when(ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {throw new ApplicationUpdateException(ApplicationUpdateFailure.Offline,"No se pudo conectar con GitHub. Puedes seguir usando SIGNA y buscar actualizaciones más tarde.",ex);}
        catch(JsonException ex){throw new ApplicationUpdateException(ApplicationUpdateFailure.FeedUnavailable,"La información de actualización no es válida. Intenta más tarde.",ex);}
        finally{operation.Release();}
    }
    public async Task DownloadAsync(AvailableApplicationUpdate update,IProgress<int> progress,CancellationToken cancellationToken=default)
    {
        await operation.WaitAsync(cancellationToken);
        try
        {
            VerifyOffer(update);downloadedVersion=null;
            await manager.DownloadUpdatesAsync(package!,value=>progress.Report(Math.Clamp(value,0,100)),cancellationToken);
            await VerifyDownloadedPackageAsync(cancellationToken);
            downloadedVersion=update.Version;
        }
        catch(OperationCanceledException) when(cancellationToken.IsCancellationRequested){throw;}
        catch(ApplicationUpdateException){throw;}
        catch(HttpRequestException ex){throw new ApplicationUpdateException(ApplicationUpdateFailure.Offline,"La descarga se interrumpió. Comprueba tu conexión y vuelve a intentar.",ex);}
        catch(Exception ex){throw new ApplicationUpdateException(ApplicationUpdateFailure.InvalidPackage,"No se pudo descargar o verificar el paquete. SIGNA seguirá usando la versión actual; puedes reintentar.",ex);}
        finally{operation.Release();}
    }
    public async Task PrepareRestartAsync(AvailableApplicationUpdate update,CancellationToken cancellationToken=default)
    {
        await operation.WaitAsync(cancellationToken);
        try
        {
            VerifyOffer(update);
            if(downloadedVersion!=update.Version)
                throw new ApplicationUpdateException(ApplicationUpdateFailure.InvalidPackage,"Primero descarga y verifica la actualización completa.");
            await VerifyDownloadedPackageAsync(cancellationToken);cancellationToken.ThrowIfCancellationRequested();
            manager.WaitExitThenApplyUpdates(package!.TargetFullRelease,silent:false,restart:true);
        }
        finally{operation.Release();}
    }
    private void VerifyOffer(AvailableApplicationUpdate update)
    {
        if(!IsManagedInstallation || !update.CanInstall || offered!=update || package?.TargetFullRelease.Version.ToString()!=update.Version)
            throw new ApplicationUpdateException(ApplicationUpdateFailure.NotManaged,"Esta instalación o versión no permite aplicar el paquete seleccionado. Busca actualizaciones de nuevo.");
        var asset=package.TargetFullRelease;
        if(asset.PackageId!="SIGNA" || Path.GetFileName(asset.FileName)!=asset.FileName || !asset.FileName.EndsWith(".nupkg",StringComparison.Ordinal) ||
            asset.Size<1 || asset.SHA256 is not {Length:64} || !asset.SHA256.All(char.IsAsciiHexDigit))
            throw new ApplicationUpdateException(ApplicationUpdateFailure.InvalidPackage,"El paquete publicado no incluye una identidad y verificación SHA-256 válidas.");
    }
    private async Task VerifyDownloadedPackageAsync(CancellationToken cancellationToken)
    {
        var asset=package!.TargetFullRelease;
        var path=Path.Combine(locator.PackagesDir ?? throw new ApplicationUpdateException(ApplicationUpdateFailure.NotManaged,"No se pudo identificar la caché del actualizador."),asset.FileName);
        byte[] actual;long size;
        await using(var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read,65536,FileOptions.Asynchronous))
        {size=stream.Length;actual=await SHA256.HashDataAsync(stream,cancellationToken);}
        if(size!=asset.Size || !CryptographicOperations.FixedTimeEquals(actual,Convert.FromHexString(asset.SHA256)))
        {
            // Solo caché de Velopack: descartar el archivo inválido permite una descarga limpia al reintentar.
            File.Delete(path);downloadedVersion=null;
            throw new ApplicationUpdateException(ApplicationUpdateFailure.InvalidPackage,"El paquete descargado no superó la verificación SHA-256. Se descartó la copia inválida; vuelve a descargarlo.");
        }
    }
    private static HttpClient CreateHttpClient()
    {
        var client=new HttpClient{Timeout=TimeSpan.FromSeconds(10)};client.DefaultRequestHeaders.UserAgent.ParseAdd("SIGNA-Updater/1.0");return client;
    }
    private sealed class BoundedDownloader:IFileDownloader
    {
        private readonly HttpClientFileDownloader inner=new();
        public Task DownloadFile(string url,string targetFile,Action<int> progress,IDictionary<string,string>? headers=null,double timeout=30,CancellationToken cancelToken=default)=>
            inner.DownloadFile(url,targetFile,progress,headers,Math.Min(timeout,10),cancelToken);
        public Task<byte[]> DownloadBytes(string url,IDictionary<string,string>? headers=null,double timeout=30)=>inner.DownloadBytes(url,headers,Math.Min(timeout,1d/6));
        public Task<string> DownloadString(string url,IDictionary<string,string>? headers=null,double timeout=30)=>inner.DownloadString(url,headers,Math.Min(timeout,1d/6));
    }
}
