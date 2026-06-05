namespace VssCodex;

/// <summary>
/// Build the browsable, searchable site with the real MkDocs + Material (Python) - invoked as a
/// subprocess, exactly like ilspycmd. We never reimplement MkDocs: we only generate its config
/// (<see cref="MkDocsConfigGenerator"/>) and run <c>mkdocs build</c> against the generated docs.
///
/// MkDocs is an optional, external prerequisite, so every failure here is NON-fatal: if Python or the
/// venv can't be set up, we print a short note and return - the markdown knowledge base and skill (the
/// core deliverables) are already done. Browsing uses a local server (Material's search fetches its
/// index, which the browser blocks on file://), so we print the serve command on success.
/// </summary>
public static class MkDocsSite
{
    private const int VenvTimeoutMs = 5 * 60 * 1000;
    private const int PipTimeoutMs = 10 * 60 * 1000;
    private const int BuildTimeoutMs = 5 * 60 * 1000;
    // Pin the theme so a future breaking mkdocs-material release can't silently break the site; the
    // cached venv's readiness sentinel is keyed on this version, so bumping it re-installs.
    private const string MkDocsMaterial = "mkdocs-material==9.7.6";
    private const string MkDocsMaterialVersion = "9.7.6";

    /// <summary>Returns true only if the site was actually built (false on any graceful skip/failure).</summary>
    public static bool Build(string refDir, BuildInfo info)
    {
        try
        {
            if (FindPython() is not (string python, string[] pyPrefix))
            {
                Note("Python 3 not found on PATH - skipping the browsable site. "
                   + "Install Python 3 (or pass --no-site to silence). The markdown knowledge base is unaffected.");
                return false;
            }

            string? mkdocs = EnsureVenvMkdocs(python, pyPrefix);
            if (mkdocs is null)
            {
                Note("could not set up the mkdocs-material venv - skipping the site. "
                   + "You can build it yourself: pip install mkdocs-material, then mkdocs build in the reference dir.");
                return false;
            }

            string docsDir = Path.Combine(refDir, "docs");
            string cfg = Path.Combine(refDir, "mkdocs.yml");
            File.WriteAllText(cfg, MkDocsConfigGenerator.BuildYaml(docsDir, info));

            string siteDir = Path.Combine(refDir, "site");
            Console.WriteLine("  building the Material site (mkdocs build) ...");
            int exit = ProcessUtil.Run(mkdocs, ["build", "-f", cfg, "-d", siteDir, "--clean"], BuildTimeoutMs, refDir);
            if (exit != 0) { Note($"mkdocs build exited {exit} - the site may be incomplete."); return false; }

            WriteServeScripts(siteDir);
            Console.WriteLine($"  site built -> {siteDir}");
            Console.WriteLine($"  browse it:  double-click {(OperatingSystem.IsWindows() ? "serve-docs.cmd" : "serve-docs.sh")} in that folder");
            Console.WriteLine($"             (opens http://localhost:8000 - the search needs the local server, it can't run from file://)");
            return true;
        }
        catch (Exception ex)
        {
            Note($"site build skipped: {ex.Message}");
            return false;
        }
    }

    /// <summary>A cached per-user venv with mkdocs-material; created + populated once, reused after.</summary>
    private static string? EnsureVenvMkdocs(string python, string[] pyPrefix)
    {
        string venvDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "vss-codex", "mkdocs-venv");
        string binDir = Path.Combine(venvDir, OperatingSystem.IsWindows() ? "Scripts" : "bin");
        string mkdocs = Path.Combine(binDir, OperatingSystem.IsWindows() ? "mkdocs.exe" : "mkdocs");
        // Sentinel keyed on the pinned version: a version bump means a different sentinel, so the venv
        // is re-installed instead of reused stale.
        string sentinel = Path.Combine(venvDir, $".vss-ready-{MkDocsMaterialVersion}");

        if (File.Exists(mkdocs) && File.Exists(sentinel)) return mkdocs;

        // Decide on the venv's pip executable, not just the directory: a previous attempt that failed
        // mid-creation (e.g. Ubuntu's python3-venv not yet installed) can leave a partial venv dir, which
        // must be discarded and rebuilt rather than reused - otherwise the site would be broken forever.
        string pip = Path.Combine(binDir, OperatingSystem.IsWindows() ? "pip.exe" : "pip");
        if (!File.Exists(pip))
        {
            if (Directory.Exists(venvDir)) { try { Directory.Delete(venvDir, recursive: true); } catch { } }
            Console.WriteLine("  creating a local mkdocs-material venv (first run, one-off) ...");
            if (ProcessUtil.Run(python, [.. pyPrefix, "-m", "venv", venvDir], VenvTimeoutMs) != 0) return null;
        }

        Console.WriteLine($"  installing {MkDocsMaterial} into the venv ...");
        if (ProcessUtil.Run(pip, ["install", "--upgrade", MkDocsMaterial], PipTimeoutMs) != 0) return null;

        if (!File.Exists(mkdocs)) return null;
        File.WriteAllText(sentinel, "ok");
        return mkdocs;
    }

    /// <summary>Resolve a Python 3 interpreter: (exe path, leading args). Null if none on PATH.</summary>
    private static (string exe, string[] prefix)? FindPython()
    {
        (string name, string[] prefix)[] cands = OperatingSystem.IsWindows()
            ? [("python", []), ("python3", []), ("py", ["-3"])]
            : [("python3", []), ("python", [])];

        foreach (var (name, prefix) in cands)
        {
            string exe = OperatingSystem.IsWindows() ? name + ".exe" : name;
            if (ProcessUtil.FindOnPath(exe) is string p) return (p, prefix);
        }
        return null;
    }

    // One-double-click launchers so a human can browse the served site (Material's search needs HTTP,
    // not file://). Both are written regardless of OS; the user runs the one for their platform.
    private static void WriteServeScripts(string siteDir)
    {
        try
        {
            File.WriteAllText(Path.Combine(siteDir, "serve-docs.cmd"),
                "@echo off\r\n" +
                "rem Serve the offline docs and open them - Material's search needs HTTP, not file://.\r\n" +
                "cd /d \"%~dp0\"\r\n" +
                "start \"\" http://localhost:8000/\r\n" +
                "python -m http.server 8000\r\n");

            string sh =
                "#!/usr/bin/env sh\n" +
                "# Serve the offline docs and open them - Material's search needs HTTP, not file://.\n" +
                "cd \"$(dirname \"$0\")\"\n" +
                "( sleep 1; (xdg-open http://localhost:8000/ 2>/dev/null || open http://localhost:8000/ 2>/dev/null) ) &\n" +
                "python3 -m http.server 8000\n";
            string shPath = Path.Combine(siteDir, "serve-docs.sh");
            File.WriteAllText(shPath, sh);
            if (!OperatingSystem.IsWindows())
                try { File.SetUnixFileMode(shPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                                                  | UnixFileMode.GroupRead | UnixFileMode.GroupExecute
                                                  | UnixFileMode.OtherRead | UnixFileMode.OtherExecute); }
                catch { }
        }
        catch { /* best-effort convenience scripts */ }
    }

    private static void Note(string msg) => Console.WriteLine($"  note: {msg}");
}
