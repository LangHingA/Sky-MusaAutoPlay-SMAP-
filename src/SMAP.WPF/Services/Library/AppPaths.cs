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
    public string Instruments => Path.Combine(Installation, "Assets", "Instruments");
    public string Setting(string name) => Path.Combine(Settings, name);
    AppPaths(string installation, string local, string roaming, bool portable)
    {
        Installation = Path.GetFullPath(installation); Portable = portable;
        string data = portable ? Path.Combine(Installation, "Data") : Path.GetFullPath(local);
        Songs = Path.Combine(data, "Songs"); Settings = portable ? Path.Combine(data, "Settings") : Path.GetFullPath(roaming);
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
        return UserDataMigration.Migrate(Path.Combine(Installation, "songs"), Songs, Settings, Backups, checkpoint);
    }
}
