using VssCodex;
using Xunit;

namespace VssCodex.Tests;

public class MarkdownWriterTests
{
    [Fact]
    public void EscapePipes_escapes_table_cell_separators() =>
        Assert.Equal(@"a \| b \| c", MarkdownWriter.EscapePipes("a | b | c"));

    [Fact]
    public void EscapePipes_leaves_pipe_free_text_untouched() =>
        Assert.Equal("no pipes here", MarkdownWriter.EscapePipes("no pipes here"));
}
