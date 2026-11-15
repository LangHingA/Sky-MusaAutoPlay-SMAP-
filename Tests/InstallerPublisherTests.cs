using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using SMAP_WPF.Updates;
using Xunit;

namespace SMAP.Tests;

public sealed class InstallerPublisherTests
{
    [Fact]
    public void InstallerRequiresDistinctTrustedStableIdentityAndExactContent()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256); byte[] bytes = [77, 90, 1, 2, 3];
        var release = new InstallerRelease(1, "SMAP", "3.0.0", "v3.0.0", "SMAP-Setup.exe", bytes.Length, UpdateFormat.Hash(bytes));
        byte[] proof = InstallerReleaseProof.Sign(release, "synthetic-stable", key);
        var stable = new UpdateKey("synthetic-stable", Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()), "stable");
        Assert.Equal(release, InstallerReleaseProof.Verify(proof, [stable], "3.0.0"));
        Assert.Throws<InvalidDataException>(() => InstallerReleaseProof.Verify(proof, [], "3.0.0"));
        Assert.Throws<InvalidDataException>(() => InstallerReleaseProof.Verify(proof, [stable with { Channel = "alpha" }], "3.0.0"));
        Assert.Throws<InvalidDataException>(() => InstallerReleaseProof.Verify(proof, [stable], "3.1.0"));
        using var files = new TestProject(); string path = files.PathFor("SMAP-Setup.exe"); File.WriteAllBytes(path, bytes); InstallerReleaseProof.VerifyFile(path, release);
        File.WriteAllBytes(path, [77, 90, 9, 9, 9]); Assert.Throws<InvalidDataException>(() => InstallerReleaseProof.VerifyFile(path, release));
        var signed = JsonSerializer.Deserialize<SignedUpdate>(proof, UpdateFormat.Json)!; byte[] signature = Convert.FromBase64String(signed.Signature); signature[0] ^= 1;
        byte[] tampered = JsonSerializer.SerializeToUtf8Bytes(signed with { Signature = Convert.ToBase64String(signature) }, UpdateFormat.Json);
        Assert.Throws<InvalidDataException>(() => InstallerReleaseProof.Verify(tampered, [stable], "3.0.0"));
    }
}
