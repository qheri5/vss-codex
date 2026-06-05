using System.Text;

namespace VssCodex;

/// <summary>
/// Diffs the current symbol snapshot against the previous run's snapshot (of a different VS version)
/// and writes CHANGELOG-&lt;old&gt;-to-&lt;new&gt;.md: API members added/removed and patchability flags
/// flipped. Turns the "re-verify after a VS update" warning into a concrete checklist. Snapshots are
/// gitignored and kept latest-only.
/// </summary>
public sealed class ChangelogGenerator
{
    private readonly string _genDate;
    public ChangelogGenerator(string genDate) => _genDate = genDate;

    /// <summary>Returns the changelog filename written, or null if this is the first/same-version run.</summary>
    public string? Process(SymbolSnapshot current, string outDir)
    {
        Directory.CreateDirectory(outDir);
        var prev = Directory.GetFiles(outDir, ".snapshot-*.json")
            .Select(SymbolSnapshot.Load)
            .Where(s => s != null && s.Version != current.Version)
            .OrderBy(s => VersionKey(s!.Version))   // numeric, so 1.10 sorts after 1.9
            .LastOrDefault();

        string? written = null;
        if (prev != null)
            written = WriteChangelog(prev, current, outDir);

        // keep only the current snapshot
        foreach (var p in Directory.GetFiles(outDir, ".snapshot-*.json")) File.Delete(p);
        current.Save(Path.Combine(outDir, $".snapshot-{current.Version}.json"));
        return written;
    }

    private string WriteChangelog(SymbolSnapshot old, SymbolSnapshot cur, string outDir)
    {
        var added = cur.Symbols.Keys.Where(k => !old.Symbols.ContainsKey(k)).OrderBy(k => k, StringComparer.Ordinal).ToList();
        var removed = old.Symbols.Keys.Where(k => !cur.Symbols.ContainsKey(k)).OrderBy(k => k, StringComparer.Ordinal).ToList();
        var flipped = cur.Symbols.Keys
            .Where(k => old.Symbols.TryGetValue(k, out var f) && f != cur.Symbols[k] && (f is "P" or "N"))
            .OrderBy(k => k, StringComparer.Ordinal)
            .Select(k => (k, from: old.Symbols[k], to: cur.Symbols[k]))
            .ToList();

        string file = $"CHANGELOG-{old.Version}-to-{cur.Version}.md";
        var sb = new StringBuilder(MarkdownWriter.Header(
            $"Reference changelog — VS {old.Version} → {cur.Version}", "snapshot diff", _genDate));
        sb.AppendLine($"Diff of the reference surface between two VS builds. **Re-verify `high-value-targets.md` line numbers** for anything below.").AppendLine();
        sb.AppendLine($"- **Added:** {added.Count} · **Removed:** {removed.Count} · **Patchability changed:** {flipped.Count}").AppendLine();

        Section(sb, $"Removed ({removed.Count}) — patches/refs targeting these will break", removed);
        if (flipped.Count > 0)
        {
            sb.AppendLine($"## Patchability flipped ({flipped.Count})").AppendLine();
            foreach (var (k, from, to) in flipped)
                sb.AppendLine($"- `{k}` — {Flag(from)} → {Flag(to)}");
            sb.AppendLine();
        }
        Section(sb, $"Added ({added.Count})", added);

        MarkdownWriter.Write(outDir, file, sb.ToString());
        return file;
    }

    private static void Section(StringBuilder sb, string title, List<string> ids)
    {
        sb.AppendLine($"## {title}").AppendLine();
        if (ids.Count == 0) { sb.AppendLine("_none_").AppendLine(); return; }
        foreach (var id in ids.Take(2000)) sb.AppendLine($"- `{id}`");
        if (ids.Count > 2000) sb.AppendLine($"- … and {ids.Count - 2000} more");
        sb.AppendLine();
    }

    private static string Flag(string f) => f switch { "P" => "✓ patchable", "N" => "✗ not", _ => f };

    /// <summary>Numeric version key so version strings sort correctly (1.10 &gt; 1.9, not ordinal).</summary>
    private static (int, int, int, int) VersionKey(string v)
    {
        var p = v.Split('.', '-', '+');
        int At(int i) => i < p.Length && int.TryParse(p[i], out var n) ? n : 0;
        return (At(0), At(1), At(2), At(3));
    }
}
