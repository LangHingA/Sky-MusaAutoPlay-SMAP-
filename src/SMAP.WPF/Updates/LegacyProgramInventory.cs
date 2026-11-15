using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace SMAP_WPF.Updates;

public sealed record LegacyProgramFile(string Path, string Sha256);

public static class LegacyProgramInventory
{
    public static void RemoveEmptyProgramDirectories(string root, IEnumerable<string> paths)
    {
        root = System.IO.Path.GetFullPath(root).TrimEnd(System.IO.Path.DirectorySeparatorChar);
        foreach (string relative in paths)
        {
            string? directory = System.IO.Path.GetDirectoryName(System.IO.Path.Combine(root, relative.Replace('/', System.IO.Path.DirectorySeparatorChar)));
            while (directory != null && directory.StartsWith(root + System.IO.Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    if (!Directory.Exists(directory) || (File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0 || Directory.EnumerateFileSystemEntries(directory).Any()) break;
                    Directory.Delete(directory); directory = System.IO.Path.GetDirectoryName(directory);
                }
                catch (IOException) { break; }
                catch (UnauthorizedAccessException) { break; }
            }
        }
    }
    public static IEnumerable<LegacyProgramFile> Matching(string root)
    {
        root = System.IO.Path.GetFullPath(root);
        if (root == System.IO.Path.GetPathRoot(root)) throw new IOException("Independent program directory required.");
        using var resource = typeof(LegacyProgramInventory).Assembly.GetManifestResourceStream("SMAP.LegacyProgramFiles.json");
        if (resource == null) yield break;
        using var document = JsonDocument.Parse(resource);
        if (document.RootElement.GetProperty("schemaVersion").GetInt32() != 1) throw new InvalidDataException("Unknown legacy inventory.");
        foreach (var file in document.RootElement.GetProperty("files").EnumerateObject())
        {
            string relative = file.Name; var parts = relative.Split('/');
            bool application = parts.Length == 1 && (relative.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                || relative is "SMAP.exe" or "SMAP.Updater.exe" or "SMAP.pdb" or "createdump.exe" or "SMAP.deps.json" or "SMAP.runtimeconfig.json" or "LICENSE.txt" or "ThirdPartyNotices.txt");
            bool culture = parts.Length == 2 && parts[0] is "cs" or "de" or "es" or "fr" or "it" or "ja" or "ko" or "pl" or "pt-BR" or "ru" or "tr" or "zh-Hans" or "zh-Hant"
                && parts[1].EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase);
            if ((!application && !culture) || parts.Any(p => p is "" or "." or ".." || p.Contains(':') || p.Contains('\\'))) throw new InvalidDataException("Invalid legacy program inventory path.");
            string path = System.IO.Path.Combine(root, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));
            for (string? parent = path; parent != null; parent = System.IO.Path.GetDirectoryName(parent))
                if ((File.Exists(parent) || Directory.Exists(parent)) && (File.GetAttributes(parent) & FileAttributes.ReparsePoint) != 0) throw new IOException("Legacy program link requires review.");
            if (!File.Exists(path)) continue;
            string digest; using (var input = File.OpenRead(path)) digest = Convert.ToHexStringLower(SHA256.HashData(input));
            if (file.Value.EnumerateArray().Any(x => x.GetString() == digest)) yield return new(relative, digest);
        }
    }
}
