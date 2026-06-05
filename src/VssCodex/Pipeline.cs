using System.Diagnostics;

namespace VssCodex;

/// <summary>
/// The orchestrator: resolve the install, decompile, generate, and install the docs + render the
/// skill - all in one cross-platform process. Output goes to out/.
/// </summary>
public static class Pipeline
{
    public static int Run(Options o)
    {
        try
        {
            string contentDir = Path.Combine(AppContext.BaseDirectory, "content");
            string install = o.Zip != null ? ArchiveExtractor.ResolveInstall(o.Zip) : (o.Install ?? DefaultInstall());
            if (!Directory.Exists(install)) throw new DirectoryNotFoundException($"VS install dir not found: {install}");

            string outDir = Path.GetFullPath(o.Out ?? Path.Combine(Directory.GetCurrentDirectory(), "out"));
            string refDir = Path.Combine(outDir, "reference");
            string genRoot = Path.Combine(refDir, "docs", "generated");
            string skillOut = Path.Combine(outDir, ".claude", "skills", "vss");

            int total = (o.SkipDecompile ? 2 : 3) + (o.NoSite ? 0 : 1), step = 0;
            var runTimer = Stopwatch.StartNew();

            void DoStep(string msg, Action body)
            {
                step++;
                Console.WriteLine();
                var sw = Stopwatch.StartNew();
                body();
                Console.WriteLine($"    [OK] step {step}/{total} done in {sw.Elapsed.TotalSeconds:n1}s ({msg})");
            }

            Banner("vss-codex  -  building the Vintage Story knowledge base");
            Console.WriteLine($"  install : {install}");
            Console.WriteLine($"  output  : {outDir}");
            Console.WriteLine($"  steps   : {total}{(o.SkipDecompile ? " (decompile skipped)" : "")}");

            if (o.SkipDecompile)
                Console.WriteLine("\n  (decompile skipped: --skip-decompile, reusing existing decompiled tree)");
            else
                DoStep("decompile the VS-authored assemblies (ilspycmd)", () => Decompiler.Run(install, refDir));

            BuildInfo info = null!;
            DoStep("generate docs: API + events/enums + lib + Harmony + CHANGELOG (Mono.Cecil)",
                () => info = Generator.Run(install, genRoot));
            DoStep("install curated docs + render the vss skill", () =>
            {
                CuratedDocs.Install(contentDir, refDir);
                SkillRenderer.Render(contentDir, info, refDir, skillOut);
            });

            // The browsable site is additive and depends only on the markdown above; MkDocsSite swallows
            // its own errors (missing Python etc.), so a failure here never fails the overall run.
            bool siteBuilt = false;
            if (!o.NoSite)
                DoStep("build the searchable Material site (MkDocs)", () => siteBuilt = MkDocsSite.Build(refDir, info));

            Banner($"DONE  -  VS {info.VsVersion}  -  total {runTimer.Elapsed.TotalSeconds:n1}s");
            Console.WriteLine($"  API     : {info.ApiTypes} types / {info.ApiNamespaces} ns, {info.CoveragePct}% documented");
            Console.WriteLine($"  Indexes : {info.Events} events, {info.Enums} enums, {info.LibTypes} engine types");
            Console.WriteLine($"  Knowledge base -> {refDir}");
            Console.WriteLine($"  Skill          -> {skillOut}  (copy into a project's .claude/skills/ to use it)");
            // Only advertise the site (and a path to cd into) when it was actually produced; a graceful
            // skip already printed its own note above.
            if (siteBuilt)
                Console.WriteLine($"  Site           -> {Path.Combine(refDir, "site")}  (serve it: python -m http.server in that dir)");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine();
            Console.Error.WriteLine("##############################################################");
            Console.Error.WriteLine($"#  vss-codex FAILED: {ex.Message}");
            Console.Error.WriteLine("##############################################################");
            return 1;
        }
    }

    private static void Banner(string text)
    {
        Console.WriteLine();
        Console.WriteLine("##############################################################");
        Console.WriteLine($"#  {text}");
        Console.WriteLine("##############################################################");
    }

    // Cross-platform default: the VINTAGE_STORY env var (the VS modding convention) works on any OS;
    // otherwise the Windows %APPDATA% location; otherwise the user must pass --install/--zip.
    private static string DefaultInstall()
    {
        string? env = Environment.GetEnvironmentVariable("VINTAGE_STORY");
        if (!string.IsNullOrWhiteSpace(env)) return env;
        if (OperatingSystem.IsWindows())
            return Environment.ExpandEnvironmentVariables(@"%APPDATA%\Vintagestory");
        throw new Exception("no default install on this OS - set VINTAGE_STORY, or pass --install <dir> or --zip <archive>");
    }
}
