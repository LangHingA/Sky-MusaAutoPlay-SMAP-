using System.IO;
using System.Text.Json.Nodes;
using SMAP_WPF;
using SMAP_WPF.Core;
using Xunit;

namespace SMAP.Tests;

public sealed class LegacyMetadataEditingTests
{
    [Theory]
    [InlineData(15, "ms")][InlineData(8, "ms")][InlineData(4, "ms")]
    [InlineData(15, "seconds")][InlineData(8, "ticks")]
    public void MetadataOnlyPreservesAllPerformanceAndUnknownFields(int keys, string unit)
    {
        using var temp = new TestProject(); string path = temp.PathFor("old.json");
        string text = """{"name":"old","author":"a","transcribedBy":"b","keyCount":$KEYS,"timeUnit":"$UNIT","origin":{"id":12},"bpm":120.125,"songNotes":[{"key":"1Key1","time":0.1234567890123456789,"duration":500,"endTime":900,"type":"hold","extension":{"v":1}}]}""".Replace("$KEYS", keys.ToString()).Replace("$UNIT", unit);
        File.WriteAllText(path, text); var before = JsonNode.Parse(text)!.AsObject();
        var edit = SongLibrary.LoadMetadata(path); edit.Name = "new"; edit.Author = "new author"; edit.TranscribedBy = "new arranger"; SongLibrary.SaveMetadata(edit);
        var after = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        Assert.Equal("new", after["name"]!.GetValue<string>());
        foreach (string field in before.Select(x => x.Key).Except(new[] { "name", "author", "transcribedBy" })) Assert.True(JsonNode.DeepEquals(before[field], after[field]), field);
        Assert.Single(Directory.GetFiles(Path.Combine(temp.DirectoryPath, ".smap-backups")));
    }
    [Fact]
    public void CollectionEditsOnlyExplicitSongAndKeepsPerformanceIdentity()
    {
        using var temp = new TestProject(); string path = temp.PathFor("collection.txt");
        File.WriteAllText(path, "[{\"name\":\"one\",\"keyCount\":15,\"songNotes\":[{\"key\":\"1Key1\",\"time\":0}]},{\"name\":\"two\",\"songNotes\":[{\"key\":\"1Key2\",\"time\":500}]}]");
        var before = JsonNode.Parse(File.ReadAllText(path))!.AsArray();
        string fingerprint = PerformanceIdentity.ForProject(LegacyProjectAdapter.Load(path, 0));
        var edit = SongLibrary.LoadMetadata(path); edit.Name = "renamed"; SongLibrary.SaveMetadata(edit);
        Assert.Equal(fingerprint, PerformanceIdentity.ForProject(LegacyProjectAdapter.Load(path, 0)));
        Assert.True(JsonNode.DeepEquals(before[1], JsonNode.Parse(File.ReadAllText(path))![1]));
        var second = SongLibrary.LoadMetadata(path, 1); second.Author = "second author"; SongLibrary.SaveMetadata(second);
        Assert.Equal("renamed", JsonNode.Parse(File.ReadAllText(path))![0]!["name"]!.GetValue<string>());
    }
    [Fact]
    public void InterruptionAndConcurrentEditNeverTruncateOriginal()
    {
        using var temp = new TestProject(); string path = temp.PathFor("old.txt"); string original = "{\"name\":\"old\",\"songNotes\":[]}";
        File.WriteAllText(path, original); var edit = SongLibrary.LoadMetadata(path); edit.Name = "new";
        Assert.Throws<IOException>(() => SongLibrary.SaveMetadata(edit, () => throw new IOException("Synthetic interrupted flush")));
        Assert.Equal(original, File.ReadAllText(path)); Assert.Empty(Directory.GetFiles(temp.DirectoryPath, "*.tmp"));
        File.WriteAllText(path, original + " "); Assert.Throws<IOException>(() => SongLibrary.SaveMetadata(edit)); Assert.Equal(original + " ", File.ReadAllText(path));
    }
}
