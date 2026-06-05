using System.Text;
using Mono.Cecil;

namespace VssCodex;

/// <summary>
/// Doc B — the exhaustive catalog of Harmony-patchable methods across the VS-authored
/// non-API assemblies (engine internals + first-party mods). Every method is flagged
/// ✓ patchable (has IL body) or ✗ (abstract / extern / runtime / no body), with the reason.
/// One file per assembly + an INDEX with per-assembly summary tables.
/// </summary>
public sealed class HarmonyTargetGenerator
{
    private readonly string _genDate;
    public HarmonyTargetGenerator(string genDate) => _genDate = genDate;

    public readonly record struct AssemblyStats(string Name, string Label, int Total, int Patchable, string File);

    public AssemblyStats Generate(ModuleDefinition module, string outDir)
    {
        string label = MarkdownWriter.AssemblyLabel(module);
        string asmName = module.Assembly.Name.Name;
        string file = $"patchable-{asmName}.md";

        var types = module.Types.SelectMany(ApiReferenceGenerator.Flatten)
            .Where(t => !t.Name.StartsWith('<') && !t.IsInterface) // interfaces have no bodies to patch
            .GroupBy(t => string.IsNullOrEmpty(t.Namespace) ? "(global)" : t.Namespace)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        int total = 0, patchable = 0;
        var sb = new StringBuilder(MarkdownWriter.Header($"Harmony-patchable methods — {asmName}", label, _genDate));
        sb.AppendLine("[← Harmony index](INDEX.md)").AppendLine();
        sb.AppendLine("Legend: **✓** = has IL body, can be Prefix/Postfix/Transpiler/Finalizer-patched · " +
                      "**✗** = no IL body (cannot be patched), reason in the last column.").AppendLine();

        foreach (var g in types)
        {
            var rows = new List<(bool ok, string sig, string reason, string type)>();
            foreach (var t in g.OrderBy(t => t.Name, StringComparer.Ordinal))
            {
                foreach (var m in t.Methods.OrderBy(m => m.Name, StringComparer.Ordinal))
                {
                    if (m.Name.StartsWith('<')) continue; // compiler-generated
                    var (ok, reason) = Patchability(m);
                    string sig = SignatureFormatter.FriendlyTypeName(t) + "." +
                                 SignatureFormatter.MethodSignature(m, withModifiers: false);
                    rows.Add((ok, sig, reason, t.Name));
                    total++;
                    if (ok) patchable++;
                }
            }
            if (rows.Count == 0) continue;

            sb.AppendLine($"### `{g.Key}`").AppendLine();
            sb.AppendLine("| | Method | If not patchable |");
            sb.AppendLine("|:-:|---|---|");
            foreach (var r in rows)
                sb.AppendLine($"| {(r.ok ? "✓" : "✗")} | `{MarkdownWriter.EscapePipes(r.sig)}` | {r.reason} |");
            sb.AppendLine();
        }

        MarkdownWriter.Write(outDir, file, sb.ToString());
        return new AssemblyStats(asmName, label, total, patchable, file);
    }

    public void WriteIndex(IReadOnlyList<AssemblyStats> stats, string outDir)
    {
        var sb = new StringBuilder(MarkdownWriter.Header("Harmony patch reference — index", "VS-authored non-API assemblies", _genDate));
        sb.AppendLine("Exhaustive catalog of every method in the VS-authored engine + first-party mods, flagged");
        sb.AppendLine("by whether Harmony can patch it (has an IL body). For the **curated, hand-written** list of");
        sb.AppendLine("high-value performance patch targets, see [`high-value-targets.md`](high-value-targets.md).");
        sb.AppendLine();
        sb.AppendLine("> **Scope:** the 6 server-relevant VS-authored assemblies. `VintagestoryAPI` is not listed here —");
        sb.AppendLine("> it is the public modding contract, covered in the [API reference](../api/INDEX.md) (and is also");
        sb.AppendLine("> patchable). `ModMaker` / `VSCrashReporter*` are tooling and intentionally omitted. (Note: most");
        sb.AppendLine("> server logic lives in `VintagestoryLib`; `VintagestoryServer.dll` is just the entry point.)");
        sb.AppendLine();
        sb.AppendLine("See also the skill's `references/harmony-usage.md` for *how* to write a patch safely.");
        sb.AppendLine();
        sb.AppendLine("| Assembly | Methods | Patchable | % | File |");
        sb.AppendLine("|---|--:|--:|--:|---|");
        foreach (var s in stats)
        {
            double pct = s.Total == 0 ? 0 : 100.0 * s.Patchable / s.Total;
            sb.AppendLine($"| `{s.Label}` | {s.Total} | {s.Patchable} | {pct:F1}% | [{s.File}]({s.File}) |");
        }
        sb.AppendLine();
        sb.AppendLine("> **What Harmony CANNOT patch:** abstract methods (no body), `extern`/`[MethodImpl(InternalCall)]`,");
        sb.AppendLine("> runtime-implemented methods, and (at runtime, unpredictably) very small JIT-inlined methods.");
        sb.AppendLine("> Interfaces are excluded entirely (no bodies). Generic method definitions are patchable but the");
        sb.AppendLine("> patch applies to the shared body — mind type-specific instantiations.");
        MarkdownWriter.Write(outDir, "INDEX.md", sb.ToString());
    }

    /// <summary>Can Harmony patch this method? Patchable iff it carries IL to rewrite.</summary>
    public static (bool ok, string reason) Patchability(MethodDefinition m)
    {
        if (m.IsAbstract) return (false, "abstract (no IL body)");
        if (m.IsPInvokeImpl) return (false, "P/Invoke (extern)");
        if (m.IsInternalCall) return (false, "InternalCall (runtime intrinsic)");
        if (m.IsRuntime) return (false, "runtime-implemented");
        if (!m.HasBody) return (false, "no IL body");
        return (true, "");
    }

}
