using System.Reflection;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Security.Cryptography;
using SMAP_WPF.Updates;
using SMAP_WPF;
using Xunit;

namespace SMAP.Tests;

public sealed class ReleaseCandidateVersionTests
{
    sealed class AssetReply(Func<HttpRequestMessage, HttpResponseMessage> response, byte[]? artifact = null) : HttpMessageHandler
    {
        readonly ECDsa _signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        public UpdateKey[] Keys => [new("synthetic-installer", Convert.ToBase64String(_signer.ExportSubjectPublicKeyInfo()), "stable")];
        public int Requests;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            if (request.RequestUri!.AbsolutePath.EndsWith("SMAP-installer-manifest.json"))
            {
                byte[] bytes = artifact ?? Encoding.ASCII.GetBytes("MZ-synthetic-installer");
                var proof = new InstallerRelease(1, "SMAP", "3.0.0", "v3.0.0", "SMAP-Setup.exe", bytes.Length, UpdateFormat.Hash(bytes));
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(InstallerReleaseProof.Sign(proof, Keys[0].Id, _signer)) });
            }
            return Task.FromResult(response(request));
        }
        protected override void Dispose(bool disposing) { if (disposing) _signer.Dispose(); base.Dispose(disposing); }
    }
    const string Asset = "https://github.com/langhinga/Sky-MusaAutoPlay-SMAP-/releases/download/v3.0.0/SMAP-Setup.exe";
    [Theory]
    [InlineData("3.0.0", "3.0.0-rc.1", 1)]
    [InlineData("3.0.0-rc.2", "3.0.0-rc.1", 1)]
    [InlineData("3.0.0-rc.11", "3.0.0-rc.2", 1)]
    [InlineData("3.0.0-beta.2", "3.0.0-rc.1", -1)]
    [InlineData("2.2", "3.0.0-rc.1", -1)]
    [InlineData("1.1-wpf", "1.1", 0)]
    public void RcCanUpgradeToFinalWithoutDowngradingToOldReleases(string left, string right, int sign)
    {
        var method = typeof(UpdateChecker).GetMethod("Compare", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.Equal(sign, Math.Sign((int)method.Invoke(null, [left, right])!));
    }

    [Theory]
    [InlineData(Asset)]
    [InlineData("https://github.com/LangHingA/Sky-MusaAutoPlay-SMAP-/releases/download/v3.0.0/SMAP-Setup.exe")]
    public async Task ValidInstallerDownloadClosesOutputBeforePublishingFile(string asset)
    {
        using var files = new TestProject();byte[] data=Encoding.ASCII.GetBytes("MZ-synthetic-installer");
        var reply=new AssetReply(_=>new(HttpStatusCode.OK){Content=new ByteArrayContent(data)});using var http=new HttpClient(reply);
        string path=await UpdateChecker.DownloadToAsync(new("3.0.0","stable","",asset,data.Length),http,files.DirectoryPath,publisherKeys:reply.Keys);
        Assert.Equal(data,File.ReadAllBytes(path));Assert.False(File.Exists(path+".download"));Assert.Equal(2,reply.Requests);
    }
    [Theory]
    [InlineData("http://github.com/langhinga/Sky-MusaAutoPlay-SMAP-/releases/download/v3.0.0/SMAP-Setup.exe")]
    [InlineData("https://foreign.invalid/SMAP-Setup.exe")]
    [InlineData("https://github.com/other-owner/Sky-MusaAutoPlay-SMAP-/releases/download/v3.0.0/SMAP-Setup.exe")]
    [InlineData("https://github.com/langhinga/other-repository/releases/download/v3.0.0/SMAP-Setup.exe")]
    public async Task UntrustedInitialAddressIsRejectedBeforeNetwork(string asset)
    {
        using var files=new TestProject();var reply=new AssetReply(_=>new(HttpStatusCode.OK));using var http=new HttpClient(reply);
        await Assert.ThrowsAsync<IOException>(()=>UpdateChecker.DownloadToAsync(new("3.0.0","","",asset,10),http,files.DirectoryPath,publisherKeys:reply.Keys));Assert.Equal(0,reply.Requests);
    }
    [Theory]
    [InlineData("http://release-assets.githubusercontent.com/install.exe")]
    [InlineData("https://foreign.invalid/install.exe")]
    public async Task UnsafeRedirectsAreRejectedWithoutFollowingThem(string location)
    {
        using var files=new TestProject();var reply=new AssetReply(_=>{var response=new HttpResponseMessage(HttpStatusCode.Found);response.Headers.Location=new Uri(location);return response;});using var http=new HttpClient(reply);
        await Assert.ThrowsAsync<IOException>(()=>UpdateChecker.DownloadToAsync(new("3.0.0","","",Asset,Encoding.ASCII.GetByteCount("MZ-synthetic-installer")),http,files.DirectoryPath,publisherKeys:reply.Keys));Assert.Equal(2,reply.Requests);Assert.Empty(Directory.GetFiles(files.DirectoryPath));
    }
    [Fact]
    public async Task TrustedHttpsReleaseRedirectWorks()
    {
        using var files=new TestProject();int step=0;byte[] bytes=Encoding.ASCII.GetBytes("MZ-installer");
        var reply=new AssetReply(request=>{if(step++==0){var redirect=new HttpResponseMessage(HttpStatusCode.Found);redirect.Headers.Location=new Uri("https://release-assets.githubusercontent.com/installer");return redirect;}return new(HttpStatusCode.OK){Content=new ByteArrayContent(bytes)};},bytes);using var http=new HttpClient(reply);
        string path=await UpdateChecker.DownloadToAsync(new("3.0.0","","",Asset,bytes.Length),http,files.DirectoryPath,publisherKeys:reply.Keys);Assert.Equal(bytes,File.ReadAllBytes(path));Assert.Equal(3,reply.Requests);
    }
    [Fact]
    public async Task HtmlAndSizeMismatchNeverBecomeAnExecutable()
    {
        using var files=new TestProject();var reply=new AssetReply(_=>new(HttpStatusCode.OK){Content=new StringContent("<html>gateway</html>")},new byte[100]);using var http=new HttpClient(reply);
        await Assert.ThrowsAsync<IOException>(()=>UpdateChecker.DownloadToAsync(new("3.0.0","","",Asset,100),http,files.DirectoryPath,publisherKeys:reply.Keys));Assert.Empty(Directory.GetFiles(files.DirectoryPath));
    }
}
