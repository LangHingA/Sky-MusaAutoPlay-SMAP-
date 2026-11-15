using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using SMAP_WPF;
using Xunit;

namespace SMAP.Tests;

public sealed class DataMigrationTests
{
    static string Hash(string path) => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
    static AppPaths Paths(TestProject temp)
    {
        string program = temp.PathFor("Program"); Directory.CreateDirectory(program);
        return AppPaths.ForInstallation(program, temp.PathFor("Local"), temp.PathFor("Roaming"));
    }
    [Fact]
    public void PortableModeRequiresExplicitMarker()
    {
        using var temp = new TestProject(); var normal = Paths(temp); Directory.CreateDirectory(Path.Combine(normal.Installation, "songs"));
        Assert.False(AppPaths.ForInstallation(normal.Installation, temp.PathFor("Local"), temp.PathFor("Roaming")).Portable);
        File.WriteAllText(Path.Combine(normal.Installation, "portable.mode"), "SMAP portable");
        var portable = AppPaths.ForInstallation(normal.Installation, temp.PathFor("Local"), temp.PathFor("Roaming"));
        Assert.True(portable.Portable); Assert.Equal(Path.Combine(normal.Installation, "Data", "Songs"), portable.Songs);
        Assert.Equal(Path.Combine(normal.Installation, "Data", "Settings"), portable.Settings);
    }
    [Fact]
    public void LargeLibraryResumeIsIdempotentAndPreservesSourceAndConflicts()
    {
        using var temp = new TestProject(); var paths = Paths(temp); string source = Path.Combine(paths.Installation, "songs"); Directory.CreateDirectory(source); Directory.CreateDirectory(paths.Songs);
        for (int i = 0; i < 1639; i++) File.WriteAllText(Path.Combine(source, "song-" + i + ".json"), "{\"name\":\"fixture " + i + "\",\"keyCount\":8,\"songNotes\":[{\"time\":0.125,\"key\":1,\"type\":\"hold\",\"duration\":500}],\"extra\":\"" + new string('x', 500) + "\"}");
        var original = Directory.GetFiles(source).ToDictionary(p => p, Hash);
        string collision = Path.Combine(paths.Songs, "song-0.json"); File.WriteAllText(collision, "existing user edit");
        Assert.Throws<IOException>(() => paths.Prepare(step => { if (step == 17) throw new IOException("Synthetic interruption"); }));
        var result = paths.Prepare(); Assert.True(result.Complete); Assert.Equal(1639, result.Files.Length);
        Assert.All(result.Files, x => { Assert.Equal(original[x.Source], Hash(x.Source)); Assert.Equal(x.Sha256, Hash(x.Destination)); });
        Assert.Equal("existing user edit", File.ReadAllText(collision));
        string[] first = Directory.GetFiles(paths.Songs).Order().ToArray(); paths.Prepare(); Assert.Equal(first, Directory.GetFiles(paths.Songs).Order());
        Assert.Equal(1640, first.Length); Assert.Empty(Directory.GetFiles(paths.Songs, "*.tmp"));
    }
    [Fact]
    public void ReferencesAndCloudIdentityMoveButSessionsAndPreferencesStayByteIdentical()
    {
        using var temp = new TestProject(); var p = Paths(temp); string source = Path.Combine(p.Installation, "songs"); Directory.CreateDirectory(source); Directory.CreateDirectory(p.Settings);
        string old = Path.Combine(source, "native.smap"); File.WriteAllText(old, "synthetic bytes");
        File.WriteAllText(p.Setting("folders.json"), JsonSerializer.Serialize(new[] { new Folder { Name = "Own", Files = [old] } }));
        File.WriteAllText(p.Setting("playlist.txt"), old); File.WriteAllText(p.Setting("nowplaying.txt"), old + "|12345");
        File.WriteAllText(p.Setting("prefs.txt"), "keep prefs"); File.WriteAllBytes(p.Setting("session.dat"), [0, 1, 2, 3, 99]);
        var untouched = new[] { "prefs.txt", "session.dat" }.ToDictionary(x => x, x => Hash(p.Setting(x)));
        Directory.CreateDirectory(p.Setting("NativeSharing")); Guid work = Guid.NewGuid(), version = Guid.NewGuid();
        var link = new NativeCloudLink("https://musetreehouse.com/", work, version, Hash(old), old);
        string oldLink = Path.Combine(p.Setting("NativeSharing"), "link-" + Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(old.ToUpperInvariant()))) + ".json");
        File.WriteAllBytes(oldLink, JsonSerializer.SerializeToUtf8Bytes(link, NativeSharingClient.Json));
        var result = p.Prepare(); string next = result.Files.Single().Destination;
        Assert.Equal(next, JsonSerializer.Deserialize<Folder[]>(File.ReadAllText(p.Setting("folders.json")))![0].Files[0]);
        Assert.Equal(next, File.ReadAllText(p.Setting("playlist.txt"))); Assert.Equal(next + "|12345", File.ReadAllText(p.Setting("nowplaying.txt")));
        var migrated = new NativeSharingStore(p.Setting("NativeSharing")).Find(next, new Uri(link.Origin)); Assert.NotNull(migrated); Assert.Equal(work, migrated.WorkId); Assert.Equal(version, migrated.VersionId);
        Assert.All(untouched, x => Assert.Equal(x.Value, Hash(p.Setting(x.Key)))); Assert.True(File.Exists(oldLink)); Assert.True(File.Exists(old));
        Assert.NotEmpty(Directory.GetFiles(p.Backups, "*.bak", SearchOption.AllDirectories));
    }
    [Fact]
    public void InterruptedReferenceRewriteCanResumeWithoutOverwritingModifiedSongs()
    {
        using var temp = new TestProject(); var p = Paths(temp); string source = Path.Combine(p.Installation, "songs"); Directory.CreateDirectory(source); Directory.CreateDirectory(p.Settings);
        string old = Path.Combine(source, "one.txt"); File.WriteAllText(old, "original"); File.WriteAllText(p.Setting("playlist.txt"), old); File.WriteAllText(p.Setting("nowplaying.txt"), old + "|88");
        Assert.Throws<IOException>(() => p.Prepare(step => { if (step == 2) throw new IOException("Synthetic profile interruption"); }));
        var result = p.Prepare(); Assert.True(result.Complete); Assert.Equal(result.Files[0].Destination + "|88", File.ReadAllText(p.Setting("nowplaying.txt")));
        File.WriteAllText(result.Files[0].Destination, "user edited migrated song"); var resumed = p.Prepare(); Assert.Equal("user edited migrated song", File.ReadAllText(result.Files[0].Destination));
        Assert.Equal("original", File.ReadAllText(resumed.Files[0].Destination)); Assert.NotEqual(result.Files[0].Destination, resumed.Files[0].Destination);
    }

    [Fact]
    public void SampleSeedingNeverReplacesAUserEditedFile()
    {
        using var temp = new TestProject(); var paths = Paths(temp);
        string samples = Path.Combine(paths.Installation, "Assets", "Samples"); Directory.CreateDirectory(samples);
        Directory.CreateDirectory(paths.Songs); File.WriteAllText(Path.Combine(paths.Songs, "same.txt"), "user edit");
        File.WriteAllText(Path.Combine(samples, "same.txt"), "bundled sample"); File.WriteAllText(Path.Combine(samples, "new.txt"), "new sample");
        paths.Prepare(); paths.Prepare();
        Assert.Equal("user edit", File.ReadAllText(Path.Combine(paths.Songs, "same.txt")));
        Assert.Equal("new sample", File.ReadAllText(Path.Combine(paths.Songs, "new.txt")));
        Assert.Equal(2, Directory.GetFiles(paths.Songs).Length);
    }

    [Fact]
    public void DestinationJunctionIsRejectedWithoutWritingOutsideTheDataRoot()
    {
        using var temp = new TestProject(); var paths = Paths(temp);
        string source = Path.Combine(paths.Installation, "songs"); Directory.CreateDirectory(Path.Combine(source, "nested"));
        File.WriteAllText(Path.Combine(source, "nested", "private.txt"), "source content");
        Directory.CreateDirectory(paths.Songs); string outside = temp.PathFor("outside"); Directory.CreateDirectory(outside);
        string link = Path.Combine(paths.Songs, "nested");
        using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/d /c mklink /J \"{link}\" \"{outside}\"") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true })!;
        process.WaitForExit(); Assert.Equal(0, process.ExitCode);
        try { Assert.Throws<IOException>(() => paths.Prepare()); Assert.Empty(Directory.GetFiles(outside)); Assert.True(File.Exists(Path.Combine(source,"nested","private.txt"))); }
        finally { Directory.Delete(link); }
    }
}
