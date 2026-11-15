using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SMAP_WPF;

public sealed record MigratedFile(string Source, string Destination, long Size, string Sha256);
public sealed record DataMigrationResult(string Source, string Destination, MigratedFile[] Files, bool Complete);

public static class UserDataMigration
{
    public static void RejectLinks(string path)
    {
        for (string? item = Path.GetFullPath(path); item != null; item = Path.GetDirectoryName(item))
            if ((File.Exists(item) || Directory.Exists(item)) && (File.GetAttributes(item) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("数据迁移不跟随重解析链接，请先核对真实目录。");
    }
    static string Digest(string path) { using var input = File.OpenRead(path); return Convert.ToHexStringLower(SHA256.HashData(input)); }
    static void Atomic(string path, byte[] bytes)
    {
        RejectLinks(path); Directory.CreateDirectory(Path.GetDirectoryName(path)!); string next = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var output = new FileStream(next, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { output.Write(bytes); output.Flush(true); }
            File.Move(next, path, true);
        }
        finally { if (File.Exists(next)) File.Delete(next); }
    }
    static IEnumerable<string> Walk(string directory)
    {
        RejectLinks(directory);
        foreach (string file in Directory.EnumerateFileSystemEntries(directory).Order(StringComparer.Ordinal))
        {
            RejectLinks(file);
            if (Directory.Exists(file)) { foreach (var nested in Walk(file)) yield return nested; }
            else yield return file;
        }
    }
    static void Backup(string path, string directory)
    {
        if (!File.Exists(path)) return;
        string digest = Digest(path), name = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(path))))[..16] + "-" + digest + ".bak";
        string backup = Path.Combine(directory, name); RejectLinks(backup); Directory.CreateDirectory(directory);
        if (!File.Exists(backup))
        {
            using var input = File.OpenRead(path); using var output = new FileStream(backup, FileMode.CreateNew, FileAccess.Write, FileShare.None); input.CopyTo(output); output.Flush(true);
        }
        if (Digest(backup) != digest) throw new IOException("迁移备份校验失败，原数据未删除。");
    }
    static string LinkPath(string profile, string file) => Path.Combine(profile, "NativeSharing", "link-" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(file).ToUpperInvariant()))) + ".json");
    static bool CloudConflict(string profile, string source, string destination)
    {
        string old = LinkPath(profile, source), next = LinkPath(profile, destination);
        if (!File.Exists(old) || !File.Exists(next)) return false;
        RejectLinks(old); RejectLinks(next);
        var a = JsonNode.Parse(File.ReadAllText(old))!.AsObject(); var b = JsonNode.Parse(File.ReadAllText(next))!.AsObject();
        a["localFile"] = destination; return !JsonNode.DeepEquals(a, b);
    }
    public static DataMigrationResult Migrate(string source, string destination, string profile, string backups, Action<int>? checkpoint = null)
    {
        source = Path.GetFullPath(source); destination = Path.GetFullPath(destination);
        if (!Directory.Exists(source)) return new(source, destination, [], true);
        if (source.Equals(destination, StringComparison.OrdinalIgnoreCase) || destination.StartsWith(source + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            || source.StartsWith(destination + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new IOException("源与目标数据目录不能重叠。");
        foreach (string path in new[] { source, destination, profile, backups }) RejectLinks(path);
        Directory.CreateDirectory(destination); Directory.CreateDirectory(profile);
        string work = Path.Combine(backups, "migration-" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(source.ToUpperInvariant())))[..16]);
        RejectLinks(work); Directory.CreateDirectory(work);
        string journal = Path.Combine(work, "manifest.json"), profileBackups = Path.Combine(work, "profile");
        var records = new List<MigratedFile>(); var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); int step = 0;
        void Save(bool complete) => Atomic(journal, JsonSerializer.SerializeToUtf8Bytes(new DataMigrationResult(source, destination, records.ToArray(), complete)));
        Save(false);
        foreach (string file in Walk(source))
        {
            string relative = Path.GetRelativePath(source, file), target = Path.GetFullPath(Path.Combine(destination, relative));
            if (!target.StartsWith(destination + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new IOException("迁移路径越界。");
            string digest = Digest(file); long size = new FileInfo(file).Length;
            if (File.Exists(target) && (Digest(target) != digest || CloudConflict(profile, file, target)))
            {
                string identity = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(file.ToUpperInvariant())))[..8];
                target = Path.Combine(Path.GetDirectoryName(target)!, Path.GetFileNameWithoutExtension(target) + "~legacy-" + identity + "-" + digest[..12] + Path.GetExtension(target));
            }
            RejectLinks(target); Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            if (File.Exists(target))
            {
                if (new FileInfo(target).Length != size || Digest(target) != digest || CloudConflict(profile, file, target)) throw new IOException("同名文件或作品关联冲突，请核对迁移清单。原文件保留。");
            }
            else
            {
                string temp = target + ".migration-" + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    using (var input = File.OpenRead(file))
                    using (var output = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { input.CopyTo(output); output.Flush(true); }
                    if (new FileInfo(temp).Length != size || Digest(temp) != digest || Digest(file) != digest) throw new IOException("曲谱在复制时发生变化，请重试。");
                    File.Move(temp, target, false);
                }
                finally { if (File.Exists(temp)) File.Delete(temp); }
            }
            map[file] = target; records.Add(new(file, target, size, digest)); if (records.Count % 32 == 0) Save(false); checkpoint?.Invoke(++step);
        }
        string Mapped(string value) => map.GetValueOrDefault(value, value);
        void RewriteNode(JsonNode? node)
        {
            if (node is JsonObject obj)
                foreach (string key in obj.Select(x => x.Key).ToArray())
                { if (obj[key] is JsonValue value && value.TryGetValue<string>(out var text) && map.TryGetValue(text, out var replacement)) obj[key] = replacement; else RewriteNode(obj[key]); }
            else if (node is JsonArray array)
                for (int index = 0; index < array.Count; index++)
                { if (array[index] is JsonValue value && value.TryGetValue<string>(out var text) && map.TryGetValue(text, out var replacement)) array[index] = replacement; else RewriteNode(array[index]); }
        }
        foreach (string name in new[] { "folders.json", "playlist.txt", "nowplaying.txt" })
        {
            string path = Path.Combine(profile, name); if (!File.Exists(path)) continue; RejectLinks(path);
            if (new FileInfo(path).Length > 16 * 1024 * 1024) throw new IOException("配置过大，需要人工核对；原文件保留。");
            byte[] original = File.ReadAllBytes(path); string text = File.ReadAllText(path), rewritten;
            if (name.EndsWith(".json")) { var node = JsonNode.Parse(text); RewriteNode(node); rewritten = node!.ToJsonString(); }
            else if (name == "playlist.txt") rewritten = string.Join(Environment.NewLine, File.ReadAllLines(path).Select(Mapped));
            else { int separator = text.LastIndexOf('|'); rewritten = separator >= 0 ? Mapped(text[..separator]) + text[separator..] : text; }
            if (rewritten != text)
            {
                Backup(path, profileBackups);
                if (!File.ReadAllBytes(path).SequenceEqual(original)) throw new IOException("配置在迁移中被修改，请重试。");
                Atomic(path, Encoding.UTF8.GetBytes(rewritten)); checkpoint?.Invoke(++step);
            }
        }
        string sharing = Path.Combine(profile, "NativeSharing");
        if (Directory.Exists(sharing))
        {
            foreach (string oldLink in Directory.EnumerateFiles(sharing, "link-*.json").ToArray())
            {
                RejectLinks(oldLink); if (new FileInfo(oldLink).Length > 16384) throw new IOException("分享关联异常，原记录保留。");
                var link = JsonNode.Parse(File.ReadAllText(oldLink))!.AsObject(); string? oldFile = link["localFile"]?.GetValue<string>();
                if (oldFile == null || !map.TryGetValue(oldFile, out var nextFile)) continue;
                link["localFile"] = nextFile; string nextLink = LinkPath(profile, nextFile); RejectLinks(nextLink);
                if (File.Exists(nextLink)) { if (!JsonNode.DeepEquals(link, JsonNode.Parse(File.ReadAllText(nextLink)))) throw new IOException("作品关联冲突，未覆盖已有记录。"); }
                else Atomic(nextLink, Encoding.UTF8.GetBytes(link.ToJsonString()));
            }
        }
        // Filename-based favorites survive conflict renames; existing entries are retained.
        string library = Path.Combine(profile, "library.json");
        if (File.Exists(library))
        {
            RejectLinks(library); var json = JsonNode.Parse(File.ReadAllText(library))!.AsObject(); bool changed = false;
            foreach (var file in records.Where(x => Path.GetFileName(x.Source) != Path.GetFileName(x.Destination)))
            {
                string oldName = Path.GetFileName(file.Source), nextName = Path.GetFileName(file.Destination);
                if (json["Favorites"] is JsonArray favorites && favorites.Any(x => x?.GetValue<string>() == oldName) && !favorites.Any(x => x?.GetValue<string>() == nextName)) { favorites.Add(nextName); changed = true; }
                if (json["Tags"] is JsonObject tags && tags.ContainsKey(oldName) && !tags.ContainsKey(nextName)) { tags[nextName] = tags[oldName]?.DeepClone(); changed = true; }
            }
            if (changed) { Backup(library, profileBackups); Atomic(library, Encoding.UTF8.GetBytes(json.ToJsonString())); }
        }
        Save(true); return new(source, destination, records.ToArray(), true);
    }
}
