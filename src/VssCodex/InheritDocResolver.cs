using Mono.Cecil;

namespace VssCodex;

/// <summary>
/// When a member has no own XML &lt;summary&gt;, look for one on the base type / implemented
/// interface that declares the same member (the effect of &lt;inheritdoc/&gt;). Returns the
/// inherited text tagged "*(inherited)*", or null. All resolution is best-effort and guarded —
/// unresolvable references degrade to null.
/// </summary>
public sealed class InheritDocResolver
{
    private readonly XmlDocIndex _xml;
    // Ancestor chains are resolved once per declaring type and reused across that type's members
    // (resolution is otherwise O(members × ancestors)).
    private readonly Dictionary<string, List<TypeDefinition>> _ancestorCache = new();

    public InheritDocResolver(XmlDocIndex xml) => _xml = xml;

    public string? Resolve(IMemberDefinition member)
    {
        try
        {
            string? s = member switch
            {
                MethodDefinition m when !m.IsConstructor => Probe(m.DeclaringType, anc => MatchMethod(anc, m)),
                PropertyDefinition p => Probe(p.DeclaringType, anc => MatchProperty(anc, p)),
                TypeDefinition t => Probe(t, anc => _xml.Get(DocId.ForType(anc))),
                _ => null,
            };
            return s == null ? null : "*(inherited)* " + s;
        }
        catch { return null; }
    }

    private string? Probe(TypeDefinition start, Func<TypeDefinition, string?> match)
    {
        foreach (var anc in AncestorsCached(start))
        {
            var hit = match(anc);
            if (hit != null) return hit;
        }
        return null;
    }

    private List<TypeDefinition> AncestorsCached(TypeDefinition t)
    {
        if (_ancestorCache.TryGetValue(t.FullName, out var cached)) return cached;
        var list = Ancestors(t).ToList();
        _ancestorCache[t.FullName] = list;
        return list;
    }

    /// <summary>Base chain first, then (transitive) interfaces — prefer base-class docs over interface docs.</summary>
    private static IEnumerable<TypeDefinition> Ancestors(TypeDefinition t)
    {
        var seen = new HashSet<string>();
        var queue = new Queue<TypeReference>();
        if (t.BaseType != null) queue.Enqueue(t.BaseType);
        foreach (var i in t.Interfaces) queue.Enqueue(i.InterfaceType);

        while (queue.Count > 0)
        {
            var td = SafeResolve(queue.Dequeue());
            if (td == null || !seen.Add(td.FullName)) continue;
            yield return td;
            if (td.BaseType != null) queue.Enqueue(td.BaseType);
            foreach (var i in td.Interfaces) queue.Enqueue(i.InterfaceType);
        }
    }

    private string? MatchMethod(TypeDefinition anc, MethodDefinition m)
    {
        MethodDefinition? singleByName = null;
        int countByName = 0;
        foreach (var cand in anc.Methods)
        {
            if (cand.Name != m.Name) continue;
            countByName++; singleByName = cand;
            if (cand.Parameters.Count == m.Parameters.Count && ParamsMatch(cand, m))
            {
                var s = _xml.Get(DocId.ForMethod(cand));
                if (s != null) return s;
            }
        }
        // fallback: a uniquely-named method (no overload ambiguity) AND the same arity, so we don't
        // attach a differently-shaped base member's summary to this one.
        if (countByName == 1 && singleByName != null && singleByName.Parameters.Count == m.Parameters.Count)
            return _xml.Get(DocId.ForMethod(singleByName));
        return null;
    }

    private string? MatchProperty(TypeDefinition anc, PropertyDefinition p)
    {
        foreach (var cand in anc.Properties)
            if (cand.Name == p.Name)
            {
                var s = _xml.Get(DocId.ForProperty(cand));
                if (s != null) return s;
            }
        return null;
    }

    private static bool ParamsMatch(MethodDefinition a, MethodDefinition b)
    {
        for (int i = 0; i < a.Parameters.Count; i++)
            if (a.Parameters[i].ParameterType.FullName != b.Parameters[i].ParameterType.FullName)
                return false;
        return true;
    }

    private static TypeDefinition? SafeResolve(TypeReference tr)
    {
        try { return tr.Resolve(); } catch { return null; }
    }
}
