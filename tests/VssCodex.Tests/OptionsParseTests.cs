using VssCodex;
using Xunit;

namespace VssCodex.Tests;

public class OptionsParseTests
{
    [Fact]
    public void Parses_all_flags_and_aliases()
    {
        var (o, exit) = Options.Parse(["--install", "/vs", "-z", "a.zip", "--out", "/out", "-s", "--no-site"]);
        Assert.Null(exit);
        Assert.Equal("/vs", o.Install);
        Assert.Equal("a.zip", o.Zip);
        Assert.Equal("/out", o.Out);
        Assert.True(o.SkipDecompile);
        Assert.True(o.NoSite);
    }

    [Fact]
    public void Short_aliases_work()
    {
        var (o, exit) = Options.Parse(["-i", "/vs", "-o", "/out"]);
        Assert.Null(exit);
        Assert.Equal("/vs", o.Install);
        Assert.Equal("/out", o.Out);
    }

    [Fact]
    public void Help_returns_exit_zero()
    {
        var (_, exit) = Options.Parse(["--help"]);
        Assert.Equal(0, exit);
    }

    [Fact]
    public void Unknown_arg_returns_exit_one()
    {
        var (_, exit) = Options.Parse(["--bogus"]);
        Assert.Equal(1, exit);
    }

    [Fact]
    public void Missing_value_is_unknown_and_returns_one()
    {
        // "-i" with no following value falls through to the default (unknown/incomplete) branch.
        var (_, exit) = Options.Parse(["-i"]);
        Assert.Equal(1, exit);
    }

    [Fact]
    public void No_args_proceeds_with_defaults()
    {
        var (o, exit) = Options.Parse([]);
        Assert.Null(exit);
        Assert.Null(o.Install);
        Assert.False(o.NoSite);
    }

    [Fact]
    public void Both_zip_and_install_still_parse() // a warning is emitted; both are populated, run proceeds
    {
        var (o, exit) = Options.Parse(["--zip", "a.zip", "--install", "/vs"]);
        Assert.Null(exit);
        Assert.Equal("a.zip", o.Zip);
        Assert.Equal("/vs", o.Install);
    }
}
