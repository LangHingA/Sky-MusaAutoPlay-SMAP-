using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using SMAP_Installer;
using SMAP_WPF.Updates;

if (args.Length != 4) throw new ArgumentException("payload.zip legacy-install-directory new-empty-work-directory report.json");
string payload = Path.GetFullPath(args[0]), legacy = Path.GetFullPath(args[1]), work = Path.GetFullPath(args[2]);
if (Directory.Exists(work)) throw new IOException("Use a new isolated workspace; existing data is never replaced.");
Directory.CreateDirectory(work); var checks = new List<object>();
void Check(string name, bool ok) { checks.Add(new { name, status = ok ? "PASS" : "FAIL" }); if (!ok) throw new InvalidDataException(name); }
string Hash(string file) { using var stream = File.OpenRead(file); return Convert.ToHexStringLower(SHA256.HashData(stream)); }
void CopyTree(string from, string to) { foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories)) { string next = Path.Combine(to, Path.GetRelativePath(from, file)); Directory.CreateDirectory(Path.GetDirectoryName(next)!); File.Copy(file, next); } }
void Extract(string target, Action<int, string>? progress = null) { using var file = File.OpenRead(payload); InstallPayload.Extract(file, target, progress); }
try {
string fresh = Path.Combine(work, "fresh"); Extract(fresh);
Check("fresh compact files and helper", Directory.GetFiles(fresh).Select(Path.GetFileName).Order().SequenceEqual(new[] { "SMAP.Updater.exe", "SMAP.exe" }.Order()));
Check("immutable resources and licenses", Directory.Exists(Path.Combine(fresh,"Assets","Instruments")) && File.Exists(Path.Combine(fresh,"Licenses","ThirdPartyNotices.txt")));
string target = Path.Combine(work, "upgrade"); CopyTree(legacy, target);
File.WriteAllText(Path.Combine(target,"unrecognized.dll"), "unknown user file must stay");
Directory.CreateDirectory(Path.Combine(target,"songs")); File.WriteAllText(Path.Combine(target,"songs","user-original.json"), """{"name":"synthetic user song","custom":true}""");
var personal = Directory.GetFiles(Path.Combine(target,"songs"),"*",SearchOption.AllDirectories).ToDictionary(f=>Path.GetRelativePath(target,f),Hash);
var oldHashes = Directory.GetFiles(target,"*",SearchOption.AllDirectories).ToDictionary(f=>Path.GetRelativePath(target,f),Hash);
int rootBefore = Directory.GetFiles(target).Length; var known = LegacyProgramInventory.Matching(target).ToArray();
Check("official legacy fixture contains verified obsolete runtime",known.Length > 100);
Extract(target); Check("legacy upgrade unknown file retained",File.ReadAllText(Path.Combine(target,"unrecognized.dll"))=="unknown user file must stay");
Check("all legacy songs byte identical",personal.All(f=>File.Exists(Path.Combine(target,f.Key))&&Hash(Path.Combine(target,f.Key))==f.Value));
Check("known obsolete runtime archived", known.Where(f=>!File.Exists(Path.Combine(fresh,f.Path))).All(f=>!File.Exists(Path.Combine(target,f.Path))));
Check("upgrade root compact plus unknown user file",Directory.GetFiles(target).Length == 3);
string failed=Path.Combine(work,"interrupted"); CopyTree(legacy,failed);
var originals=Directory.GetFiles(failed,"*",SearchOption.AllDirectories).ToDictionary(f=>Path.GetRelativePath(failed,f),Hash);
bool interrupted=false; try {Extract(failed,(percent,_)=>{if(percent>0)throw new IOException("synthetic interrupted install");});}catch(IOException){interrupted=true;}
Check("interruption restores every original byte",interrupted&&originals.All(f=>File.Exists(Path.Combine(failed,f.Key))&&Hash(Path.Combine(failed,f.Key))==f.Value));
using var signer=ECDsa.Create(ECCurve.NamedCurves.nistP256); UpdateKey[] keys=[new("isolated-layout",Convert.ToBase64String(signer.ExportSubjectPublicKeyInfo()),"alpha")];
var manifest=UpdateBuilder.Build(fresh,Path.Combine(work,"packs"),"alpha","3.0.0-rc.3"); byte[] signed=UpdateBuilder.Sign(manifest,keys[0].Id,signer);
// Signatures here belong to an ephemeral synthetic test identity, never production.
string delta=Path.Combine(work,"delta"); CopyTree(legacy,delta); UpdateTransaction.Apply(delta,fresh,signed,keys,"alpha",manifest.Tag);
Check("real legacy layout accepts signed compact transaction",manifest.Files.All(f=>Hash(Path.Combine(delta,f.Path))==f.Sha256));
Check("delta preserves old songs", Directory.GetFiles(Path.Combine(legacy,"songs"),"*",SearchOption.AllDirectories).All(f=>Hash(f)==Hash(Path.Combine(delta,Path.GetRelativePath(legacy,f)))));
string rollback=Path.Combine(work,"delta-rollback"); CopyTree(legacy,rollback); bool rolledBack=false;
try{ UpdateTransaction.Apply(rollback,fresh,signed,keys,"alpha",manifest.Tag,_=>throw new IOException("synthetic interrupted delta")); }catch(IOException){rolledBack=true;}
Check("signed delta interruption restores originals",rolledBack&&Directory.GetFiles(legacy,"*",SearchOption.AllDirectories).All(f=>Hash(f)==Hash(Path.Combine(rollback,Path.GetRelativePath(legacy,f)))));
File.WriteAllText(args[3],JsonSerializer.Serialize(new{checks,rootBefore,rootAfter=Directory.GetFiles(target).Length,knownFiles=known.Length,legacySongs=personal.Count,installedFiles=Directory.GetFiles(fresh,"*",SearchOption.AllDirectories).Select(f=>Path.GetRelativePath(fresh,f)).ToArray(),testSigningIdentity="ephemeral synthetic, not production",work},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine($"PASS {checks.Count}, root {rootBefore} -> {Directory.GetFiles(target).Length}");
}catch(Exception error){File.WriteAllText(args[3],JsonSerializer.Serialize(new{checks,error=error.ToString()},new JsonSerializerOptions{WriteIndented=true}));throw;}
