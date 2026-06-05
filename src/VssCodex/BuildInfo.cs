using System.Text.Json;

namespace VssCodex;

/// <summary>
/// Machine-readable handoff from the generator to the formatter (vss-codex.ps1): the version + counts
/// the formatter injects into the skill template and prints in its summary. Written to the output root
/// as build-info.json (gitignored, like everything under the proprietary tree).
/// </summary>
public sealed class BuildInfo
{
    public string VsVersion { get; set; } = "";
    public string GeneratedOn { get; set; } = "";
    public int ApiTypes { get; set; }
    public int ApiNamespaces { get; set; }
    public int ApiTypesDocumented { get; set; }
    public int CoveragePct { get; set; }
    public int Events { get; set; }
    public int Enums { get; set; }
    public int LibTypes { get; set; }
    public Dictionary<string, string> Harmony { get; set; } = new(); // assembly -> "patchable/total"

    public void Save(string outDir) =>
        File.WriteAllText(Path.Combine(outDir, "build-info.json"),
            JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
}
