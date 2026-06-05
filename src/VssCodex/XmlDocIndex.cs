using System.Text;
using System.Xml;

namespace VssCodex;

/// <summary>
/// Loads an IntelliSense XML doc file (e.g. VintagestoryAPI.xml) and exposes the &lt;summary&gt;
/// for a given doc-comment member id (see <see cref="DocId"/>). Missing files / missing members
/// degrade gracefully to null so the generator never hard-fails on doc gaps.
/// </summary>
public sealed class XmlDocIndex
{
    private readonly Dictionary<string, string> _summaries = new(StringComparer.Ordinal);
    public int Count => _summaries.Count;
    public bool Loaded { get; }

    public XmlDocIndex(string? xmlPath)
    {
        if (string.IsNullOrEmpty(xmlPath) || !File.Exists(xmlPath)) return;

        var doc = new XmlDocument();
        doc.Load(xmlPath);
        var members = doc.SelectNodes("/doc/members/member");
        if (members == null) return;

        foreach (XmlNode member in members)
        {
            string? name = member.Attributes?["name"]?.Value;
            if (name == null) continue;
            var summaryNode = member.SelectSingleNode("summary");
            if (summaryNode == null) continue;
            string text = Flatten(summaryNode);
            if (text.Length > 0) _summaries[name] = text;
        }
        Loaded = true;
    }

    /// <summary>Summary for a doc id, or null. Newlines collapsed to single spaces.</summary>
    public string? Get(string docId) => _summaries.TryGetValue(docId, out var s) ? s : null;

    /// <summary>Render a &lt;summary&gt; node to a single-line plain string (cref/paramref → readable names).</summary>
    private static string Flatten(XmlNode node)
    {
        var sb = new StringBuilder();
        Walk(node, sb);
        // collapse all whitespace runs to single spaces
        return string.Join(' ', sb.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static void Walk(XmlNode node, StringBuilder sb)
    {
        foreach (XmlNode child in node.ChildNodes)
        {
            switch (child.NodeType)
            {
                case XmlNodeType.Text:
                case XmlNodeType.CDATA:
                    sb.Append(child.Value);
                    break;
                case XmlNodeType.Element:
                    string el = child.Name;
                    if (el is "see" or "seealso")
                    {
                        string cref = child.Attributes?["cref"]?.Value ?? child.Attributes?["langword"]?.Value ?? "";
                        sb.Append(' ').Append(ShortCref(cref)).Append(' ');
                    }
                    else if (el is "paramref" or "typeparamref")
                    {
                        sb.Append(' ').Append(child.Attributes?["name"]?.Value ?? "").Append(' ');
                    }
                    else
                    {
                        Walk(child, sb); // <c>, <para>, <list>, etc. — keep inner text
                    }
                    break;
            }
        }
    }

    /// <summary>"T:Vintagestory.API.Common.Entity" → "Entity"; "M:...Foo(...)" → "Foo".</summary>
    private static string ShortCref(string cref)
    {
        if (cref.Length > 2 && cref[1] == ':') cref = cref[2..];
        int paren = cref.IndexOf('(');
        if (paren >= 0) cref = cref[..paren];
        int dot = cref.LastIndexOf('.');
        return dot >= 0 ? cref[(dot + 1)..] : cref;
    }
}
