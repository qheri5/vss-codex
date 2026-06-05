using System.Text.Json;
using Mono.Cecil;

namespace VssCodex;

/// <summary>
/// A versioned fingerprint of the reference surface: doc-id -> flag. Persisted (gitignored) so the
/// next run, after a VS update, can diff against it (see <see cref="ChangelogGenerator"/>).
/// Flags: "" for API types/members, "P"/"N" for patchable/non-patchable methods.
/// </summary>
public sealed class SymbolSnapshot
{
    public string Version { get; set; } = "";
    public Dictionary<string, string> Symbols { get; set; } = new();

    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = false };

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, Opts));

    public static SymbolSnapshot? Load(string path)
    {
        try { return JsonSerializer.Deserialize<SymbolSnapshot>(File.ReadAllText(path)); }
        catch { return null; }
    }

    /// <summary>
    /// Build a snapshot from the API module (public API types + members, flag "") and the Harmony
    /// assemblies (method doc-ids with patchable flag "P"/"N").
    /// </summary>
    public static SymbolSnapshot Build(string version, ModuleDefinition apiModule, IEnumerable<ModuleDefinition> harmonyModules)
    {
        var snap = new SymbolSnapshot { Version = version };

        foreach (var t in apiModule.Types.SelectMany(ApiReferenceGenerator.Flatten)
                     .Where(ApiReferenceGenerator.IsPublicSurface)
                     .Where(t => !ApiReferenceGenerator.IsCompilerGenerated(t))
                     .Where(t => SignatureFormatter.RootNamespace(t).StartsWith("Vintagestory.API", StringComparison.Ordinal)))
        {
            snap.Symbols[DocId.ForType(t)] = "";
            foreach (var m in t.Methods.Where(m => m.IsPublic || m.IsFamily)) snap.Symbols[DocId.ForMethod(m)] = "";
            foreach (var p in t.Properties.Where(p => (p.GetMethod ?? p.SetMethod) is { IsPublic: true } or { IsFamily: true }))
                snap.Symbols[DocId.ForProperty(p)] = "";
            foreach (var f in t.Fields.Where(f => (f.IsPublic || f.IsFamily) && !f.IsSpecialName)) snap.Symbols[DocId.ForField(f)] = "";
        }

        foreach (var module in harmonyModules)
            foreach (var t in module.Types.SelectMany(ApiReferenceGenerator.Flatten).Where(t => !t.IsInterface))
                foreach (var m in t.Methods)
                {
                    if (m.Name.StartsWith('<')) continue;
                    snap.Symbols[DocId.ForMethod(m)] = HarmonyTargetGenerator.Patchability(m).ok ? "P" : "N";
                }

        return snap;
    }
}
