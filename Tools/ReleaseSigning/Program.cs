using System.Security.Cryptography;
using System.Text.Json;
using SMAP_WPF.Updates;

if (args.Length == 2 && args[0] == "inspect-public")
{
    string pem = File.ReadAllText(args[1]);
    if (!pem.Contains("BEGIN PUBLIC KEY") || pem.Contains("PRIVATE KEY")) throw new InvalidDataException("Supply only the approved public key.");
    using var key = ECDsa.Create(); key.ImportFromPem(pem);
    if (key.KeySize != 256) throw new InvalidDataException("A P-256 public key is required.");
    Console.WriteLine(JsonSerializer.Serialize(new { publicKey = Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()), sha256 = UpdateFormat.Hash(key.ExportSubjectPublicKeyInfo()) })); return;
}
if (args.Length == 4 && args[0] == "verify-installer")
{
    if (new FileInfo(args[3]).Length > InstallerReleaseProof.MaxProofBytes) throw new InvalidDataException("Installer proof too large.");
    var verifiedRelease = InstallerReleaseProof.Verify(File.ReadAllBytes(args[3]), UpdateTrust.Keys, args[2]);
    InstallerReleaseProof.VerifyFile(args[1], verifiedRelease);
    Console.WriteLine(JsonSerializer.Serialize(new { verified = true, verifiedRelease.Version, verifiedRelease.Sha256 })); return;
}
if (args.Length == 5 && args[0] == "verify-update")
{
    if (new FileInfo(args[2]).Length > UpdateFormat.MaxManifestBytes) throw new InvalidDataException("Update manifest too large.");
    var manifest = UpdateFormat.Verify(File.ReadAllBytes(args[2]), UpdateTrust.Keys, args[3], args[4]);
    void Match(string path, long size, string hash)
    {
        UpdateFormat.RejectLinks(path);
        using var input = File.OpenRead(path);
        if (input.Length != size || Convert.ToHexStringLower(SHA256.HashData(input)) != hash) throw new InvalidDataException("Signed content mismatch: " + Path.GetFileName(path));
    }
    foreach (var publishedFile in manifest.Files) Match(UpdateFormat.Under(args[1], publishedFile.Path), publishedFile.Size, publishedFile.Sha256);
    foreach (var pack in manifest.Packs) Match(Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[2]))!, pack.Name), pack.Size, pack.Sha256);
    Console.WriteLine(JsonSerializer.Serialize(new { verified = true, manifest.Version, manifest.Channel, files = manifest.Files.Length, packs = manifest.Packs.Length })); return;
}
if (args.Length != 6 || args[0] != "sign-installer") throw new ArgumentException("inspect-public public.pem | sign-installer installer.exe version key-id private.pem output.json | verify-installer installer.exe version proof.json | verify-update published-directory manifest.json channel tag");
using var signer = ECDsa.Create(); signer.ImportFromPem(File.ReadAllText(args[4]));
var trusted = UpdateTrust.Keys.SingleOrDefault(k => k.Channel == "stable" && k.Id == args[3] && k.PublicKey == Convert.ToBase64String(signer.ExportSubjectPublicKeyInfo()))
    ?? throw new InvalidDataException("The approved Stable public identity must be compiled into the signing tool first.");
using var file = File.OpenRead(args[1]);
var release = new InstallerRelease(1, "SMAP", args[2], "v" + args[2], "SMAP-Setup.exe", file.Length, Convert.ToHexStringLower(SHA256.HashData(file)));
byte[] proof = InstallerReleaseProof.Sign(release, trusted.Id, signer); InstallerReleaseProof.Verify(proof, UpdateTrust.Keys, release.Version);
using (var output = new FileStream(args[5], FileMode.CreateNew)) { output.Write(proof); output.Flush(true); }
Console.WriteLine(JsonSerializer.Serialize(new { release.Version, release.Size, release.Sha256, keyId = trusted.Id }));
