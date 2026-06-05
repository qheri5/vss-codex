using System.Diagnostics;

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
            int exit = RunProcess(mkdocs, ["build", "-f", cfg, "-d", siteDir, "--clean"], BuildTimeoutMs, refDir);
            if (exit != 0) { Note($"mkdocs build exited {exit} - the site may be incomplete."); return false; }

            Console.WriteLine($"  site built -> {siteDir}");
            Console.WriteLine($"  browse offline:  cd \"{siteDir}\"  then  python -m http.server   (open http://localhost:8000)");
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
        string sentinel = Path.Combine(venvDir, ".vss-ready");

        if (File.Exists(mkdocs) && File.Exists(sentinel)) return mkdocs;

        // Decide on the venv's pip executable, not just the directory: a previous attempt that failed
        // mid-creation (e.g. Ubuntu's python3-venv not yet installed) can leave a partial venv dir, which
        // must be discarded and rebuilt rather than reused - otherwise the site would be broken forever.
        string pip = Path.Combine(binDir, OperatingSystem.IsWindows() ? "pip.exe" : "pip");
        if (!File.Exists(pip))
        {
            if (Directory.Exists(venvDir)) { try { Directory.Delete(venvDir, recursive: true); } catch { } }
            Console.WriteLine("  creating a local mkdocs-material venv (first run, one-off) ...");
            if (RunProcess(python, [.. pyPrefix, "-m", "venv", venvDir], VenvTimeoutMs) != 0) return null;
        }

        Console.WriteLine("  installing mkdocs-material into the venv ...");
        if (RunProcess(pip, ["install", "--upgrade", "mkdocs-material"], PipTimeoutMs) != 0) return null;

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

    // Inherit the console (no redirect) so first-run pip/venv progress is visible; bounded by a timeout.
    private static int RunProcess(string exe, string[] args, int timeoutMs, string? workingDir = null)
    {
        var psi = new ProcessStartInfo(exe) { UseShellExecute = false };
        if (workingDir != null) psi.WorkingDirectory = workingDir;
        foreach (var a in args) psi.ArgumentList.Add(a);
        try
        {
            using var p = Process.Start(psi);
            if (p is null) return -1;
            if (!p.WaitForExit(timeoutMs)) { try { p.Kill(entireProcessTree: true); } catch { } return -2; }
            return p.ExitCode;
        }
        catch { return -1; }
    }

    private static void Note(string msg) => Console.WriteLine($"  note: {msg}");
}
