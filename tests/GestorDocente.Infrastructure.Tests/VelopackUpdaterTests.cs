using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using GestorDocente.Application;
using GestorDocente.Infrastructure;
using Velopack;
using Velopack.Locators;
using Velopack.Sources;
using Xunit;

namespace GestorDocente.Infrastructure.Tests;

public class VelopackUpdaterTests
{
    [Fact]
    public async Task Real_sdk_reads_local_feed_and_downloads_only_the_reviewed_stable_package()
    {
        using var fixture=new Fixture();var update=await fixture.Service.CheckAsync();Assert.NotNull(update);Assert.True(update.CanInstall);
        await Assert.ThrowsAsync<ApplicationUpdateException>(()=>fixture.Service.DownloadAsync(new("2.0.0",true),new Progress<int>()));
        await fixture.Service.DownloadAsync(update,new Progress<int>());
        Assert.Equal(fixture.Payload,File.ReadAllBytes(fixture.CachedPackage));Assert.Equal("1.6.0",fixture.Service.CurrentVersion);
    }

    [Fact]
    public async Task Corrupt_preexisting_cache_is_rejected_even_when_sdk_would_skip_its_download()
    {
        using var fixture=new Fixture();var update=(await fixture.Service.CheckAsync())!;
        File.WriteAllBytes(fixture.CachedPackage,Enumerable.Repeat((byte)9,fixture.Payload.Length).ToArray());
        var error=await Assert.ThrowsAsync<ApplicationUpdateException>(()=>fixture.Service.DownloadAsync(update,new Progress<int>()));
        Assert.Equal(ApplicationUpdateFailure.InvalidPackage,error.Failure);Assert.False(File.Exists(fixture.CachedPackage));
        await fixture.Service.DownloadAsync(update,new Progress<int>());Assert.Equal(fixture.Payload,File.ReadAllBytes(fixture.CachedPackage));
    }

    [Theory]
    [InlineData("1.6.0",false,false)][InlineData("1.5.0",false,false)]
    [InlineData("1.7.0",true,false)][InlineData("1.7.0-beta",false,false)]
    public async Task Same_older_prerelease_or_malformed_releases_never_offer_automatic_install(string version,bool prerelease,bool draft)
    {
        using var fixture=new Fixture(version,prerelease,draft);
        if(prerelease || ApplicationUpdateRules.ParseStable(version) is null)
            await Assert.ThrowsAsync<ApplicationUpdateException>(()=>fixture.Service.CheckAsync());
        else Assert.Null(await fixture.Service.CheckAsync());
        Assert.False(File.Exists(fixture.CachedPackage));
    }

    [Fact]
    public async Task Missing_channel_is_not_a_false_up_to_date_result_and_offline_is_recoverable()
    {
        using var fixture=new Fixture(feed:false);var update=await fixture.Service.CheckAsync();Assert.Equal("1.7.0",update?.Version);Assert.False(update?.CanInstall);
        fixture.Handler.Offline=true;var error=await Assert.ThrowsAsync<ApplicationUpdateException>(()=>fixture.Service.CheckAsync());
        Assert.Equal(ApplicationUpdateFailure.Offline,error.Failure);fixture.Handler.Offline=false;Assert.NotNull(await fixture.Service.CheckAsync());
    }

    [Fact]
    public async Task Tampering_after_download_is_rejected_before_any_updater_process_can_start()
    {
        using var fixture=new Fixture();var update=(await fixture.Service.CheckAsync())!;
        await fixture.Service.DownloadAsync(update,new Progress<int>());
        File.WriteAllBytes(fixture.CachedPackage,[1,2,3]);
        var error=await Assert.ThrowsAsync<ApplicationUpdateException>(()=>fixture.Service.PrepareRestartAsync(update));
        Assert.Equal(ApplicationUpdateFailure.InvalidPackage,error.Failure);Assert.False(File.Exists(fixture.CachedPackage));
    }

    private sealed class Fixture:IDisposable
    {
        private const string FileName="SIGNA-1.7.0-win-x64-full.nupkg";
        private readonly string directory=Path.Combine(Path.GetTempPath(),"signa-vpk-"+Guid.NewGuid().ToString("N"));
        public byte[] Payload{get;}="paquete ficticio para validar transporte e integridad"u8.ToArray();
        public string CachedPackage=>Path.Combine(directory,"packages",FileName);
        public Handler Handler{get;}
        public VelopackApplicationUpdater Service{get;}
        private readonly HttpClient http;
        public Fixture(string version="1.7.0",bool prerelease=false,bool draft=false,bool feed=true)
        {
            Directory.CreateDirectory(directory);var source=Path.Combine(directory,"feed");Directory.CreateDirectory(source);
            Directory.CreateDirectory(Path.GetDirectoryName(CachedPackage)!);File.WriteAllBytes(Path.Combine(source,FileName),Payload);
            File.WriteAllText(Path.Combine(source,"releases.win-x64.json"),JsonSerializer.Serialize(new {Assets=new[]{new {
                PackageId="SIGNA",Version="1.7.0",Type="Full",FileName,SHA1=Convert.ToHexString(SHA1.HashData(Payload)),SHA256=Convert.ToHexString(SHA256.HashData(Payload)),Size=Payload.Length}}}));
            var locator=new TestVelopackLocator("SIGNA","1.6.0",Path.GetDirectoryName(CachedPackage)!,directory,directory,Path.Combine(directory,"Update.exe"),"win-x64");
            var manager=new UpdateManager(new SimpleFileSource(new DirectoryInfo(source)),new UpdateOptions{ExplicitChannel="win-x64",AllowVersionDowngrade=false,MaximumDeltasBeforeFallback=-1},locator);
            Handler=new(JsonSerializer.Serialize(new{tag_name="v"+version,prerelease,draft,assets=feed ? new[]{new{name="releases.win-x64.json"}} : []}));
            http=new(Handler);Service=new("1.6.0",manager,http,"win-x64",locator);
        }
        public void Dispose(){http.Dispose();Directory.Delete(directory,true);}
    }
    public sealed class Handler(string json):HttpMessageHandler
    {
        public bool Offline;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {
            Assert.Equal("https://api.github.com/repos/danielpose1996-stack/SIGNA/releases/latest",request.RequestUri?.ToString());Assert.Null(request.Headers.Authorization);
            return Offline ? Task.FromException<HttpResponseMessage>(new HttpRequestException("Fallo ficticio")) : Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(json)});
        }
    }
}
