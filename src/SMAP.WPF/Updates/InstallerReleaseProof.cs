using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace SMAP_WPF.Updates;

public sealed record InstallerRelease(int SchemaVersion, string Product, string Version, string Tag, string Artifact, long Size, string Sha256);

public static class InstallerReleaseProof
{
    public const int MaxProofBytes = 16384;
    public static byte[] Sign(InstallerRelease release, string keyId, ECDsa signer)
    {
        Validate(release, release.Version);
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(release, UpdateFormat.Json);
        return JsonSerializer.SerializeToUtf8Bytes(new SignedUpdate(keyId, Convert.ToBase64String(payload),
            Convert.ToBase64String(signer.SignData(payload, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))), UpdateFormat.Json);
    }
    public static InstallerRelease Verify(byte[] bytes, IReadOnlyList<UpdateKey> keys, string version)
    {
        if (bytes.Length > MaxProofBytes) throw new InvalidDataException("Installer proof too large.");
        using var document = JsonDocument.Parse(bytes); UpdateFormat.Unique(document.RootElement);
        var signed = document.Deserialize<SignedUpdate>(UpdateFormat.Json) ?? throw new InvalidDataException("Missing installer signature.");
        var key = keys.SingleOrDefault(k => k.Id == signed.KeyId && k.Channel == "stable") ?? throw new InvalidDataException("Untrusted installer publisher.");
        byte[] payload = Convert.FromBase64String(signed.Payload), signature = Convert.FromBase64String(signed.Signature);
        using var verifier = ECDsa.Create(); verifier.ImportSubjectPublicKeyInfo(Convert.FromBase64String(key.PublicKey), out _);
        if (signature.Length != 64 || !verifier.VerifyData(payload, signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation)) throw new InvalidDataException("Invalid installer signature.");
        using var content = JsonDocument.Parse(payload); UpdateFormat.Unique(content.RootElement);
        var release = content.Deserialize<InstallerRelease>(UpdateFormat.Json) ?? throw new InvalidDataException("Invalid installer metadata.");
        Validate(release, version); return release;
    }
    static void Validate(InstallerRelease release, string version)
    {
        if (release.SchemaVersion != 1 || release.Product != "SMAP" || !UpdateFormat.IsVersion(version) || release.Version != version || release.Tag != "v" + version
            || release.Artifact != "SMAP-Setup.exe" || release.Size is < 2 or > 512L * 1024 * 1024 || !UpdateFormat.IsHash(release.Sha256)) throw new InvalidDataException("Installer release identity mismatch.");
    }
    public static void VerifyFile(string path, InstallerRelease release)
    {
        using var input = File.OpenRead(path);
        if (input.Length != release.Size || Convert.ToHexStringLower(SHA256.HashData(input)) != release.Sha256) throw new InvalidDataException("Installer content hash mismatch.");
    }
}
