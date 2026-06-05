using System.Text;
using Mono.Cecil;

namespace VssCodex;

/// <summary>
/// Cross-cutting indexes over the API surface: events.md (the event-driven modding surface — events
/// + delegate types) and enums.md (all public enums + their values). Both pull official summaries
/// (with inheritance) like the main API doc. VS modding is event-driven and enums are scattered
/// one-per-file, so these two consolidated pages are the most frequent lookups.
/// </summary>
public sealed class EventsEnumsGenerator
{
    private readonly XmlDocIndex _xml;
    private readonly InheritDocResolver? _inherit;
    private readonly string _genDate;

    public EventsEnumsGenerator(XmlDocIndex xml, InheritDocResolver? inherit, string genDate)
    {
        _xml = xml;
        _inherit = inherit;
        _genDate = genDate;
    }

    private string? Summary(IMemberDefinition m, string id) => _xml.Get(id) ?? _inherit?.Resolve(m);

    public (int events, int enums) Generate(ModuleDefinition module, string outDir)
    {
        string label = MarkdownWriter.AssemblyLabel(module);
        var apiTypes = module.Types.SelectMany(ApiReferenceGenerator.Flatten)
            .Where(ApiReferenceGenerator.IsPublicSurface)
            .Where(t => !ApiReferenceGenerator.IsCompilerGenerated(t))
            .Where(t => SignatureFormatter.RootNamespace(t).StartsWith("Vintagestory.API", StringComparison.Ordinal))
            .ToList();

        int events = WriteEvents(apiTypes, label, outDir);
        int enums = WriteEnums(apiTypes, label, outDir);
        return (events, enums);
    }

    private int WriteEvents(List<TypeDefinition> apiTypes, string label, string outDir)
    {
        var sb = new StringBuilder(MarkdownWriter.Header("API — events & delegates", label, _genDate));
        sb.AppendLine("[← API index](INDEX.md)").AppendLine();
        sb.AppendLine("The event-driven modding surface. Subscribe in `StartServerSide`/`StartClientSide`.").AppendLine();

        // Events grouped by declaring type (only types that actually expose events).
        int count = 0;
        var withEvents = apiTypes
            .Select(t => (t, evs: t.Events.Where(e => e.AddMethod is { IsPublic: true } or { IsFamily: true }).ToList()))
            .Where(x => x.evs.Count > 0)
            .OrderBy(x => SignatureFormatter.RootNamespace(x.t) + "." + x.t.Name, StringComparer.Ordinal)
            .ToList();

        sb.AppendLine("## Events").AppendLine();
        foreach (var (t, evs) in withEvents)
        {
            sb.AppendLine($"### {SignatureFormatter.QualifiedFriendlyName(t)} — `{SignatureFormatter.RootNamespace(t)}`").AppendLine();
            foreach (var e in evs.OrderBy(e => e.Name, StringComparer.Ordinal))
            {
                sb.Append("- ").Append(SignatureFormatter.ObsoletePrefix(e))
                  .Append('`').Append(SignatureFormatter.EventSignature(e)).Append('`');
                var s = Summary(e, DocId.ForEvent(e));
                if (s != null) sb.Append(" — ").Append(s);
                sb.AppendLine();
                count++;
            }
            sb.AppendLine();
        }

        // Delegate types (the handler signatures events/callbacks use).
        var delegates = apiTypes.Where(IsDelegate)
            .OrderBy(t => SignatureFormatter.RootNamespace(t) + "." + t.Name, StringComparer.Ordinal).ToList();
        sb.AppendLine("## Delegate types").AppendLine();
        foreach (var d in delegates)
        {
            var invoke = d.Methods.FirstOrDefault(m => m.Name == "Invoke");
            string sig = invoke != null ? SignatureFormatter.MethodSignature(invoke, withModifiers: false) : "Invoke(...)";
            sb.Append("- `").Append(SignatureFormatter.QualifiedFriendlyName(d)).Append("` : `").Append(sig).Append('`');
            var s = Summary(d, DocId.ForType(d));
            if (s != null) sb.Append(" — ").Append(s);
            sb.AppendLine();
        }

        MarkdownWriter.Write(outDir, "events.md", sb.ToString());
        return count;
    }

    private int WriteEnums(List<TypeDefinition> apiTypes, string label, string outDir)
    {
        var enums = apiTypes.Where(t => t.IsEnum)
            .GroupBy(SignatureFormatter.RootNamespace)
            .OrderBy(g => g.Key, StringComparer.Ordinal).ToList();

        var sb = new StringBuilder(MarkdownWriter.Header("API — enums", label, _genDate));
        sb.AppendLine("[← API index](INDEX.md)").AppendLine();

        int count = 0;
        foreach (var g in enums)
        {
            sb.AppendLine($"## `{g.Key}`").AppendLine();
            foreach (var e in g.OrderBy(SignatureFormatter.QualifiedFriendlyName, StringComparer.Ordinal))
            {
                sb.Append("### ").Append(SignatureFormatter.ObsoletePrefix(e)).AppendLine(SignatureFormatter.QualifiedFriendlyName(e));
                var s = Summary(e, DocId.ForType(e));
                if (s != null) sb.AppendLine().AppendLine(s);
                sb.AppendLine();
                sb.AppendLine(string.Join("  ", e.Fields.Where(f => f.IsLiteral)
                    .Select(f => $"`{f.Name}`" + (f.Constant != null ? $"={f.Constant}" : ""))));
                sb.AppendLine();
                count++;
            }
        }

        MarkdownWriter.Write(outDir, "enums.md", sb.ToString());
        return count;
    }

    private static bool IsDelegate(TypeDefinition t) =>
        t.BaseType is { FullName: "System.MulticastDelegate" or "System.Delegate" };
}
