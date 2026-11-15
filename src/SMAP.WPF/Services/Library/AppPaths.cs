using System.IO;

namespace SMAP_WPF;

public sealed class AppPaths
{
    public static AppPaths Current { get; } = ForInstallation(AppContext.BaseDirectory);
    public string Installation { get; }
    public bool Portable { get; }
    public string Songs { get; }
    public string Settings { get; }
    public string Cache { get; }
    public string Updates { get; }
    public string Backups { get; }
    readonly string _previousSongs;
    public string Instruments => Path.Combine(Installation, "Assets", "Instruments");
    public string Setting(string name) => Path.Combine(Settings, name);
    AppPaths(string installation, string local, string roaming, bool portable)
    {
        Installation = Path.GetFullPath(installation); Portable = portable;
        string data = portable ? Path.Combine(Installation, "Data") : Path.GetFullPath(local);
        Songs = Path.Combine(Installation, "Song"); _previousSongs = Path.Combine(data, "Songs");
        Settings = portable ? Path.Combine(data, "Settings") : Path.GetFullPath(roaming);
        Cache = Path.Combine(data, "Cache"); Updates = Path.Combine(data, "Updates"); Backups = Path.Combine(data, "Backups");
    }
    public static AppPaths ForInstallation(string installation, string? local = null, string? roaming = null)
    {
        string marker = Path.Combine(installation, "portable.mode");
        if (File.Exists(marker)) UserDataMigration.RejectLinks(marker);
        return new(installation,
            local ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SMAP"),
            roaming ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SMAP"), File.Exists(marker));
    }
    public DataMigrationResult Prepare(Action<int>? checkpoint = null)
    {
        foreach (string path in new[] { Songs, Settings, Cache, Updates, Backups }) { UserDataMigration.RejectLinks(path); Directory.CreateDirectory(path); }
        var migrated = new List<MigratedFile>();
        string legacy = Path.Combine(Installation, "songs");
        foreach (string source in new[] { _previousSongs, legacy })
            migrated.AddRange(UserDataMigration.Migrate(source, Songs, Settings, Backups, checkpoint).Files);
        var migration = new DataMigrationResult(legacy, Songs, migrated.ToArray(), true);
        string samples = Path.Combine(Installation, "Assets", "Samples");
        if (Directory.Exists(samples))
            foreach (string file in Directory.EnumerateFiles(samples))
            {
                if (Path.GetExtension(file).ToLowerInvariant() is not (".txt" or ".json" or ".smap")) continue;
                UserDataMigration.RejectLinks(file); string destination = Path.Combine(Songs, Path.GetFileName(file)); UserDataMigration.RejectLinks(destination);
                if (File.Exists(destination)) continue;
                string temporary = destination + ".seed-" + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    using (var input = File.OpenRead(file))
                    using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { input.CopyTo(output); output.Flush(true); }
                    using (var before = File.OpenRead(file))
                    using (var after = File.OpenRead(temporary))
                        if (!System.Security.Cryptography.SHA256.HashData(before).SequenceEqual(System.Security.Cryptography.SHA256.HashData(after))) throw new IOException("示例曲谱复制校验失败。");
                    File.Move(temporary, destination, false);
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
            }
        return migration;
    }
}
