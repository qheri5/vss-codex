using Mono.Cecil;

namespace VssCodex;

/// <summary>
/// Shared base for the markdown doc generators: holds the XML-summary index, the optional inherited-doc
/// resolver, and the generation date, and resolves a member's summary (own, else inherited, else null).
/// </summary>
public abstract class DocGenerator
{
    protected readonly XmlDocIndex _xml;
    protected readonly InheritDocResolver? _inherit;
    protected readonly string _genDate;

    protected DocGenerator(XmlDocIndex xml, InheritDocResolver? inherit, string genDate)
    {
        _xml = xml;
        _inherit = inherit;
        _genDate = genDate;
    }

    /// <summary>Official summary, falling back to an inherited one (tagged) when absent.</summary>
    protected string? Summary(IMemberDefinition member, string docId) => _xml.Get(docId) ?? _inherit?.Resolve(member);
}
