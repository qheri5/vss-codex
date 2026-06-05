using VssCodex;
using Xunit;

namespace VssCodex.Tests;

public class ProcessUtilTests
{
    [Fact]
    public void Run_returns_minus_one_when_the_executable_is_missing()
    {
        // A launch failure is caught and surfaced as -1 (logged to stderr), never an escaping exception.
        int rc = ProcessUtil.Run("vss-codex-no-such-exe-zzzzz", [], 5000);
        Assert.Equal(-1, rc);
    }

    [Fact]
    public void Run_returns_zero_for_a_successful_command()
    {
        // `dotnet` is always on PATH where these tests run (dev + CI). Quiet mode drains its output.
        int rc = ProcessUtil.Run("dotnet", ["--version"], 60_000, quiet: true);
        Assert.Equal(0, rc);
    }
}
