using System;
using System.IO;
using System.Linq;
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

    [Fact]
    public void Exact_overload_inherits_its_own_base_summary()
    {
        var idx = IndexWith(
            "<member name=\"M:VssCodex.Tests.Fixtures.OverloadBase.Op(System.Int32)\"><summary>one arg</summary></member>");
        var resolver = new InheritDocResolver(idx);

        var oneArg = TestModule.Type("OverloadDerived").Methods.First(m => m.Name == "Op" && m.Parameters.Count == 1);
        var s = resolver.Resolve(oneArg);
        Assert.NotNull(s);
        Assert.Contains("one arg", s);
    }

    [Fact]
    public void Does_not_borrow_a_different_arity_base_overload_summary()
    {
        // The base declares only Op(int); the derived Op(int,int) has no matching base overload, so it
        // must NOT inherit Op(int)'s summary (regression: the single-name fallback ignored arity).
        var idx = IndexWith(
            "<member name=\"M:VssCodex.Tests.Fixtures.OverloadBase.Op(System.Int32)\"><summary>one arg</summary></member>");
        var resolver = new InheritDocResolver(idx);

        var twoArg = TestModule.Type("OverloadDerived").Methods.First(m => m.Name == "Op" && m.Parameters.Count == 2);
        Assert.Null(resolver.Resolve(twoArg));
    }
}
