using VssCodex;

// vss-codex - build the Vintage Story modding reference from the game binaries (cross-platform).
//   dotnet run --project src/VssCodex -- [options]
// OUTPUT is derived from proprietary binaries and is written only into the gitignored out/ tree.

var o = new Options();
for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--install" or "-i" when i + 1 < args.Length: o.Install = args[++i]; break;
        case "--zip" or "-z" when i + 1 < args.Length: o.Zip = args[++i]; break;
        case "--out" or "-o" when i + 1 < args.Length: o.Out = args[++i]; break;
        case "--skip-decompile" or "-s": o.SkipDecompile = true; break;
        case "--help" or "-h": PrintHelp(); return 0;
        default:
            Console.Error.WriteLine($"unknown or incomplete argument: {args[i]}");
            PrintHelp();
            return 1;
    }
}

return Pipeline.Run(o);

static void PrintHelp()
{
    Console.WriteLine("""
        vss-codex - build the Vintage Story modding reference from the game binaries.

          dotnet run --project src/VssCodex -- [options]

          --install, -i <dir>   VS install dir (default: %APPDATA%\Vintagestory on Windows)
          --zip,     -z <file>  a VS server/client .zip or .tar.gz to extract and use
          --out,     -o <dir>   output dir (default: ./out)
          --skip-decompile, -s  reuse the existing decompiled tree (faster doc/skill iteration)
          --help,    -h         this help

        Output (gitignored): <out>/reference/ (the knowledge base) and <out>/.claude/skills/vss/.
        Requires the .NET SDK and ilspycmd (auto-installed). Runs on Windows, Linux, and macOS.
        """);
}
