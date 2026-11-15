using System.IO;
using System.Security.Cryptography;
using SMAP_WPF.Updates;
using Xunit;

namespace SMAP.Tests;

public sealed class StableTrustTests
{
    [Fact]
    public void ApprovedStableIdentityIsPinnedAndIndependentFromAlpha()
    {
        Assert.True(UpdateTrust.StableConfigured);
        var stable=Assert.Single(UpdateTrust.Keys,k=>k.Channel=="stable");
        var alpha=Assert.Single(UpdateTrust.Keys,k=>k.Channel=="alpha");
        Assert.Equal("stable-20261115",stable.Id);
        Assert.Equal("e6a2f70178628257b373d56dd38c6824aec581016c960a9ae870efc6bb351f3b",UpdateFormat.Hash(Convert.FromBase64String(stable.PublicKey)));
        Assert.NotEqual(alpha.PublicKey,stable.PublicKey);Assert.NotEqual(alpha.Id,stable.Id);
        using var key=ECDsa.Create();key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(stable.PublicKey),out _);Assert.Equal(256,key.KeySize);
    }
    [Fact]
    public void MerelyUsingTheApprovedKeyIdCannotForgeAnInstallerProof()
    {
        using var other=ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var record=new InstallerRelease(1,"SMAP","3.0.0","v3.0.0","SMAP-Setup.exe",2,UpdateFormat.Hash(new byte[]{77,90}));
        var proof=InstallerReleaseProof.Sign(record,"stable-20261115",other);
        Assert.Throws<InvalidDataException>(()=>InstallerReleaseProof.Verify(proof,UpdateTrust.Keys,"3.0.0"));
    }
}
