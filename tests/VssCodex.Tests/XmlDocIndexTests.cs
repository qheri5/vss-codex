using System.IO;
using VssCodex;
using Xunit;

namespace VssCodex.Tests;

public class XmlDocIndexTests
{
    private static string WriteXml(string body)
    {
        string path = Path.Combine(Path.GetTempPath(), $"vss-xml-{System.Guid.NewGuid():N}.xml");
        File.WriteAllText(path, $"<doc><members>{body}</members></doc>");
        return path;
    }

    [Fact]
    public void Missing_file_degrades_to_not_loaded()
    {
        var idx = new XmlDocIndex(Path.Combine(Path.GetTempPath(), "does-not-exist.xml"));
        Assert.False(idx.Loaded);
        Assert.Null(idx.Get("T:Anything"));
    }

    [Fact]
    public void Null_path_degrades()
    {
        var idx = new XmlDocIndex(null);
        Assert.False(idx.Loaded);
    }

    [Fact]
    public void Reads_and_flattens_summary()
    {
        var idx = new XmlDocIndex(WriteXml(
            "<member name=\"T:Foo\"><summary>Hello   world</summary></member>"));
        Assert.True(idx.Loaded);
        Assert.Equal("Hello world", idx.Get("T:Foo"));
    }

    [Fact]
    public void Shortens_see_cref_to_simple_name()
    {
        var idx = new XmlDocIndex(WriteXml(
            "<member name=\"M:Foo.Bar\"><summary>see <see cref=\"T:My.Space.Thing\"/> here</summary></member>"));
        Assert.Equal("see Thing here", idx.Get("M:Foo.Bar"));
    }

    [Fact]
    public void Unknown_member_returns_null()
    {
        var idx = new XmlDocIndex(WriteXml(
            "<member name=\"T:Foo\"><summary>x</summary></member>"));
        Assert.Null(idx.Get("T:Missing"));
    }

    [Fact]
    public void Flattens_paramref_typeparamref_c_and_para()
    {
        var idx = new XmlDocIndex(WriteXml(
            "<member name=\"M:Foo.Bar\"><summary>uses <paramref name=\"x\"/> and <typeparamref name=\"T\"/> " +
            "with <c>code</c> in <para>a para</para></summary></member>"));
        Assert.Equal("uses x and T with code in a para", idx.Get("M:Foo.Bar"));
    }
}
