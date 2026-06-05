using System.Text;
using Mono.Cecil;

namespace VssCodex;

/// <summary>
/// Doc A — the public API endpoints reference. In normal mode: VintagestoryAPI.dll filtered to the
/// Vintagestory.API.* namespaces, enriched with official &lt;summary&gt; from VintagestoryAPI.xml
/// (+ inherited summaries). In engineInternal mode: any public type of an engine assembly
/// (e.g. VintagestoryLib) into a subdir, no namespace filter, summaries mostly empty (no XML).
/// One file per (root) namespace + an INDEX. "Endpoints" = the public/protected surface a mod
/// consumes or overrides.
/// </summary>
public sealed class ApiReferenceGenerator
{
    private readonly XmlDocIndex _xml;
    private readonly InheritDocResolver? _inherit;
    private readonly string _genDate;

    public ApiReferenceGenerator(XmlDocIndex xml, InheritDocResolver? inherit, string genDate)
    {
        _xml = xml;
        _inherit = inherit;
        _genDate = genDate;
    }

    /// <summary>Official summary, falling back to an inherited one (tagged) when absent.</summary>
    private string? Summary(IMemberDefinition member, string docId)
        => _xml.Get(docId) ?? _inherit?.Resolve(member);

    public (int types, int namespaces, int documented) Generate(ModuleDefinition module, string outDir, bool engineInternal = false)
    {
        string label = MarkdownWriter.AssemblyLabel(module);

        IEnumerable<TypeDefinition> q = module.Types
            .SelectMany(Flatten)
            .Where(IsPublicSurface)
            .Where(t => !IsCompilerGenerated(t));
        // Normal mode: only the real public modding contract. Drops bundled OSS (CompactExifLib,
        // ProperVersion) and engine namespaces that happen to ship public types in the API DLL.
        if (!engineInternal)
            q = q.Where(t => SignatureFormatter.RootNamespace(t).StartsWith("Vintagestory.API", StringComparison.Ordinal));

        var types = q.ToList();

        // Bucket by the ROOT namespace so nested public types land with their declaring type.
        var byNamespace = types
            .GroupBy(SignatureFormatter.RootNamespace)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        int documented = types.Count(t => _xml.Get(DocId.ForType(t)) != null);
        WriteIndex(outDir, label, types, byNamespace, documented, engineInternal);

        foreach (var g in byNamespace)
        {
            var sb = new StringBuilder(MarkdownWriter.Header($"API — {g.Key}", label, _genDate));
            if (engineInternal)
                sb.AppendLine("> **ENGINE-INTERNAL — unstable, no official docs.** These are public types of the")
                  .AppendLine("> engine assembly, not the modding contract; signatures only (no XML summaries).").AppendLine();
            sb.AppendLine($"[← index]({(engineInternal ? "INDEX.md" : "INDEX.md")}) · {g.Count()} public types.").AppendLine();

            foreach (var t in g.OrderBy(SignatureFormatter.QualifiedFriendlyName, StringComparer.Ordinal))
                WriteType(sb, t);

            MarkdownWriter.Write(outDir, NamespaceFile(g.Key), sb.ToString());
        }

        return (types.Count, byNamespace.Count, documented);
    }

    private void WriteIndex(string outDir, string label, List<TypeDefinition> types,
        List<IGrouping<string, TypeDefinition>> byNamespace, int documented, bool engineInternal)
    {
        var idx = new StringBuilder(MarkdownWriter.Header(
            engineInternal ? "Vintage Story engine internals — type reference" : "Vintage Story API — endpoints reference",
            label, _genDate));

        idx.AppendLine($"{(engineInternal ? "Public engine types of" : "Public modding surface of")} `{label}`. " +
                       $"{types.Count} public types across {byNamespace.Count} namespaces.");
        if (!engineInternal)
        {
            int pct = types.Count == 0 ? 0 : (int)Math.Round(100.0 * documented / types.Count);
            idx.AppendLine($"**Summary coverage:** {documented} of {types.Count} public types carry an upstream XML " +
                           $"summary ({pct}%); the rest are undocumented by VS itself (members may still inherit one).");
            idx.AppendLine();
            idx.AppendLine("Cross-cutting indexes: [events.md](events.md) · [enums.md](enums.md). Engine internals: [lib/INDEX.md](lib/INDEX.md).");
        }
        idx.AppendLine();
        idx.AppendLine("One file per namespace. A type's full source is at");
        idx.AppendLine($"`vs-game-reference/decompiled/{label.Split('.')[0]}/<namespace-as-folders>/<Type>.cs`.");
        idx.AppendLine();
        idx.AppendLine("| Namespace | Types | File |");
        idx.AppendLine("|---|--:|---|");
        foreach (var g in byNamespace)
        {
            string file = NamespaceFile(g.Key);
            idx.AppendLine($"| `{g.Key}` | {g.Count()} | [{file}]({file}) |");
        }
        MarkdownWriter.Write(outDir, "INDEX.md", idx.ToString());
    }

    private void WriteType(StringBuilder sb, TypeDefinition t)
    {
        sb.AppendLine($"## {SignatureFormatter.ObsoletePrefix(t)}{SignatureFormatter.QualifiedFriendlyName(t)}");
        sb.AppendLine();
        sb.AppendLine($"```csharp\n{SignatureFormatter.TypeDeclaration(t)}\n```");
        string? sum = Summary(t, DocId.ForType(t));
        if (sum != null) sb.AppendLine().AppendLine(sum);
        sb.AppendLine();

        if (t.IsEnum) { WriteEnum(sb, t); sb.AppendLine("---").AppendLine(); return; }

        // Constructors (instance, public/protected) — rendered explicitly (they are IsSpecialName).
        var ctors = t.Methods.Where(m => m.IsConstructor && !m.IsStatic && (m.IsPublic || m.IsFamily))
            .OrderBy(m => m.Parameters.Count).ToList();
        if (ctors.Count > 0)
        {
            sb.AppendLine("**Constructors**").AppendLine();
            foreach (var c in ctors) WriteMember(sb, c, SignatureFormatter.MethodSignature(c), Summary(c, DocId.ForMethod(c)));
            sb.AppendLine();
        }

        var fields = t.Fields.Where(f => (f.IsPublic || f.IsFamily) && !f.IsSpecialName)
            .OrderBy(f => f.Name, StringComparer.Ordinal).ToList();
        if (fields.Count > 0)
        {
            sb.AppendLine("**Fields**").AppendLine();
            foreach (var f in fields) WriteMember(sb, f, SignatureFormatter.FieldSignature(f), Summary(f, DocId.ForField(f)));
            sb.AppendLine();
        }

        var props = t.Properties.Where(IsVisibleProperty)
            .OrderBy(p => p.Name, StringComparer.Ordinal).ToList();
        if (props.Count > 0)
        {
            sb.AppendLine("**Properties**").AppendLine();
            foreach (var p in props) WriteMember(sb, p, SignatureFormatter.PropertySignature(p), Summary(p, DocId.ForProperty(p)));
            sb.AppendLine();
        }

        var events = t.Events.Where(e => e.AddMethod is { IsPublic: true } or { IsFamily: true })
            .OrderBy(e => e.Name, StringComparer.Ordinal).ToList();
        if (events.Count > 0)
        {
            sb.AppendLine("**Events**").AppendLine();
            foreach (var e in events) WriteMember(sb, e, SignatureFormatter.EventSignature(e), Summary(e, DocId.ForEvent(e)));
            sb.AppendLine();
        }

        var methods = t.Methods.Where(IsVisibleMethod)
            .OrderBy(m => m.Name, StringComparer.Ordinal).ToList();
        if (methods.Count > 0)
        {
            sb.AppendLine("**Methods**").AppendLine();
            foreach (var m in methods) WriteMember(sb, m, SignatureFormatter.MethodSignature(m), Summary(m, DocId.ForMethod(m)));
            sb.AppendLine();
        }

        sb.AppendLine("---").AppendLine();
    }

    private static void WriteEnum(StringBuilder sb, TypeDefinition t)
    {
        sb.AppendLine("**Values**").AppendLine();
        foreach (var f in t.Fields.Where(f => f.IsLiteral))
            sb.AppendLine($"- `{f.Name}`" + (f.Constant != null ? $" = {f.Constant}" : ""));
        sb.AppendLine();
    }

    /// <summary>Render a member bullet: obsolete prefix + signature + summary.</summary>
    private static void WriteMember(StringBuilder sb, ICustomAttributeProvider member, string signature, string? summary)
    {
        sb.Append("- ").Append(SignatureFormatter.ObsoletePrefix(member)).Append('`').Append(signature).Append('`');
        if (summary != null) sb.Append(" — ").Append(summary);
        sb.AppendLine();
    }

    // -- filters --------------------------------------------------------------

    public static IEnumerable<TypeDefinition> Flatten(TypeDefinition t)
    {
        yield return t;
        foreach (var n in t.NestedTypes)
            foreach (var x in Flatten(n))
                yield return x;
    }

    public static bool IsPublicSurface(TypeDefinition t) =>
        t.IsPublic || t.IsNestedPublic || t.IsNestedFamily || t.IsNestedFamilyOrAssembly;

    public static bool IsCompilerGenerated(TypeDefinition t) =>
        t.Name.StartsWith('<') || t.Name.StartsWith("__") ||
        t.CustomAttributes.Any(a => a.AttributeType.Name == "CompilerGeneratedAttribute");

    private static bool IsVisibleProperty(PropertyDefinition p)
    {
        var a = p.GetMethod ?? p.SetMethod;
        return a is { IsPublic: true } or { IsFamily: true };
    }

    private static bool IsVisibleMethod(MethodDefinition m)
    {
        // Constructors are rendered in their own subsection; everything special-name is excluded here.
        if (m.IsSpecialName || m.IsGetter || m.IsSetter || m.IsAddOn || m.IsRemoveOn) return false;
        if (m.Name.StartsWith('<')) return false;
        return m.IsPublic || m.IsFamily;
    }

    private static string NamespaceFile(string ns) => ns.Replace("(global)", "global") + ".md";
}
