using System;
using System.IO;
using VssCodex;
using Xunit;

namespace VssCodex.Tests;

public class InheritDocTests
{
    private static XmlDocIndex IndexWith(string member)
    {
        string path = Path.Combine(Path.GetTempPath(), $"vss-inh-{Guid.NewGuid():N}.xml");
        File.WriteAllText(path, $"<doc><members>{member}</members></doc>");
        return new XmlDocIndex(path);
    }

    [Fact]
    public void Inherits_summary_from_implemented_interface()
    {
        var idx = IndexWith(
            "<member name=\"M:VssCodex.Tests.Fixtures.IDocumented.Described\"><summary>does a thing</summary></member>");
        var resolver = new InheritDocResolver(idx);

        var derived = TestModule.Method("DerivedDoc", "Described");
        string? s = resolver.Resolve(derived);

        Assert.NotNull(s);
        Assert.Contains("does a thing", s);
        Assert.Contains("(inherited)", s);
    }

    [Fact]
    public void Returns_null_when_no_ancestor_documents_it()
    {
        var idx = IndexWith("<member name=\"T:Unrelated\"><summary>x</summary></member>");
        var resolver = new InheritDocResolver(idx);

        var m = TestModule.Method("Simple", "M");
        Assert.Null(resolver.Resolve(m));
    }
}
