using System.IO;
using VssCodex;
using Xunit;

namespace VssCodex.Tests;

public class SnapshotChangelogTests
{
    [Fact]
    public void Snapshot_save_load_roundtrip()
    {
        var s = new SymbolSnapshot { Version = "1.0" };
        s.Symbols["T:A"] = "";
        s.Symbols["M:B"] = "P";
        var path = Path.Combine(Directory.CreateTempSubdirectory().FullName, "snap.json");
        s.Save(path);

        var loaded = SymbolSnapshot.Load(path);
        Assert.NotNull(loaded);
        Assert.Equal("1.0", loaded!.Version);
        Assert.Equal("P", loaded.Symbols["M:B"]);
    }

    [Fact]
    public void Load_missing_returns_null() =>
        Assert.Null(SymbolSnapshot.Load(Path.Combine(Path.GetTempPath(), "nope.json")));

    [Fact]
    public void Changelog_reports_added_removed_flipped_and_rolls_snapshot()
    {
        string dir = Directory.CreateTempSubdirectory().FullName;

        var old = new SymbolSnapshot { Version = "1.0" };
        old.Symbols["T:Keep"] = "";
        old.Symbols["T:Removed"] = "";
        old.Symbols["M:Flip"] = "N";
        old.Save(Path.Combine(dir, ".snapshot-1.0.json"));

        var cur = new SymbolSnapshot { Version = "1.1" };
        cur.Symbols["T:Keep"] = "";
        cur.Symbols["T:Added"] = "";
        cur.Symbols["M:Flip"] = "P";

        string? file = new ChangelogGenerator("2026-01-01").Process(cur, dir);

        Assert.Equal("CHANGELOG-1.0-to-1.1.md", file);
        string md = File.ReadAllText(Path.Combine(dir, file!));
        Assert.Contains("T:Added", md);
        Assert.Contains("T:Removed", md);
        Assert.Contains("M:Flip", md);

        // Only the current snapshot is kept after rolling.
        var snaps = Directory.GetFiles(dir, ".snapshot-*.json");
        Assert.Single(snaps);
        Assert.EndsWith(".snapshot-1.1.json", snaps[0]);
    }

    [Fact]
    public void First_run_writes_no_changelog()
    {
        string dir = Directory.CreateTempSubdirectory().FullName;
        var cur = new SymbolSnapshot { Version = "1.0" };
        cur.Symbols["T:A"] = "";
        Assert.Null(new ChangelogGenerator("2026-01-01").Process(cur, dir));
        Assert.Single(Directory.GetFiles(dir, ".snapshot-*.json"));
    }

    [Fact]
    public void BuildInfo_saves_expected_json()
    {
        string dir = Directory.CreateTempSubdirectory().FullName;
        new BuildInfo { VsVersion = "1.22.3", ApiTypes = 1095, CoveragePct = 37 }.Save(dir);
        string json = File.ReadAllText(Path.Combine(dir, "build-info.json"));
        Assert.Contains("\"VsVersion\": \"1.22.3\"", json);
        Assert.Contains("\"ApiTypes\": 1095", json);
    }
}
