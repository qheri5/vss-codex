using VssCodex;

// vss-codex - build the Vintage Story modding reference from the game binaries (cross-platform).
//   dotnet run --project src/VssCodex -- [options]
// OUTPUT is derived from proprietary binaries and is written only into the gitignored out/ tree.

var (o, exitCode) = Options.Parse(args);
if (exitCode is int code) { PrintHelp(); ConsoleExit.PauseIfLaunchedByDoubleClick(); return code; }

int rc = Pipeline.Run(o);
ConsoleExit.PauseIfLaunchedByDoubleClick();
return rc;

static void PrintHelp()
{
    Console.WriteLine("""
        vss-codex - build the Vintage Story modding reference from the game binaries.

          dotnet run --project src/VssCodex -- [options]

          --install, -i <dir>   VS install dir (default: %APPDATA%\Vintagestory on Windows)
          --zip,     -z <file>  a VS server/client .zip or .tar.gz to extract and use
          --out,     -o <dir>   output dir (default: ./out)
          --skip-decompile, -s  reuse the existing decompiled tree (faster doc/skill iteration)
          --no-site             skip building the browsable MkDocs site (the markdown is still produced)
          --help,    -h         this help

        Output (gitignored): <out>/reference/ (the knowledge base) and <out>/.claude/skills/vss/.
        Requires the .NET SDK and ilspycmd (auto-installed). Runs on Windows, Linux, and macOS.
        The browsable site additionally needs Python 3 + mkdocs-material (auto-installed into a cached
        venv on first run); it is skipped gracefully if Python is unavailable, or with --no-site.
        """);
}
