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
if (args.Length != 6 || args[0] != "sign-installer") throw new ArgumentException("inspect-public public.pem | sign-installer installer.exe version key-id private.pem output.json");
using var signer = ECDsa.Create(); signer.ImportFromPem(File.ReadAllText(args[4]));
var trusted = UpdateTrust.Keys.SingleOrDefault(k => k.Channel == "stable" && k.Id == args[3] && k.PublicKey == Convert.ToBase64String(signer.ExportSubjectPublicKeyInfo()))
    ?? throw new InvalidDataException("The approved Stable public identity must be compiled into the signing tool first.");
using var file = File.OpenRead(args[1]);
var release = new InstallerRelease(1, "SMAP", args[2], "v" + args[2], "SMAP-Setup.exe", file.Length, Convert.ToHexStringLower(SHA256.HashData(file)));
byte[] proof = InstallerReleaseProof.Sign(release, trusted.Id, signer); InstallerReleaseProof.Verify(proof, UpdateTrust.Keys, release.Version);
using (var output = new FileStream(args[5], FileMode.CreateNew)) { output.Write(proof); output.Flush(true); }
Console.WriteLine(JsonSerializer.Serialize(new { release.Version, release.Size, release.Sha256, keyId = trusted.Id }));
