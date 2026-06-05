using System.Diagnostics;
using System.Text;

namespace VssCodex;

/// <summary>Cross-platform helpers shared by the steps that shell out to external tools.</summary>
public static class ProcessUtil
{
    /// <summary>First directory on PATH that contains <paramref name="exe"/>, or null if none.</summary>
    public static string? FindOnPath(string exe)
    {
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir)) continue;
            try { string c = Path.Combine(dir, exe); if (File.Exists(c)) return c; } catch { }
        }
        return null;
    }

    /// <summary>
    /// Run a subprocess with a hard timeout. Returns the exit code, -2 on timeout (process tree killed),
    /// or -1 on a launch/exception (logged to stderr). <paramref name="quiet"/> redirects and drains the
    /// child's output (so it can't block and isn't echoed); otherwise the child inherits the console so
    /// long first-run installs show progress.
    /// </summary>
    public static int Run(string exe, IReadOnlyList<string> args, int timeoutMs, string? workingDir = null, bool quiet = false)
    {
        var psi = new ProcessStartInfo(exe) { UseShellExecute = false };
        if (workingDir != null) psi.WorkingDirectory = workingDir;
        if (quiet) { psi.RedirectStandardOutput = true; psi.RedirectStandardError = true; }
        foreach (var a in args) psi.ArgumentList.Add(a);
        try
        {
            using var p = new Process { StartInfo = psi };
            if (quiet)
            {
                p.OutputDataReceived += static (_, _) => { };  // drain so the child can't block on a full pipe
                p.ErrorDataReceived += static (_, _) => { };
            }
            p.Start();
            if (quiet) { p.BeginOutputReadLine(); p.BeginErrorReadLine(); }
            if (!p.WaitForExit(timeoutMs)) { try { p.Kill(entireProcessTree: true); } catch { } return -2; }
            if (quiet) p.WaitForExit(); // flush the async output readers
            return p.ExitCode;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"  process error ({Path.GetFileName(exe)}): {ex.Message}");
            return -1;
        }
    }

    /// <summary>
    /// Like <see cref="Run"/> but captures the child's combined stdout+stderr instead of echoing it, and
    /// returns it alongside the exit code. Lets a caller stay quiet on success and surface the output
    /// only on failure. Same -2 (timeout) / -1 (launch error) sentinels.
    /// </summary>
    public static (int exit, string output) RunCaptured(string exe, IReadOnlyList<string> args, int timeoutMs, string? workingDir = null)
    {
        var psi = new ProcessStartInfo(exe)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        if (workingDir != null) psi.WorkingDirectory = workingDir;
        foreach (var a in args) psi.ArgumentList.Add(a);

        var sb = new StringBuilder();
        void Collect(object _, DataReceivedEventArgs e) { if (e.Data != null) lock (sb) sb.AppendLine(e.Data); }
        try
        {
            using var p = new Process { StartInfo = psi };
            p.OutputDataReceived += Collect;
            p.ErrorDataReceived += Collect;
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            if (!p.WaitForExit(timeoutMs)) { try { p.Kill(entireProcessTree: true); } catch { } return (-2, sb.ToString()); }
            p.WaitForExit(); // flush the async readers
            return (p.ExitCode, sb.ToString());
        }
        catch (Exception ex)
        {
            return (-1, sb.ToString() + ex.Message);
        }
    }
}
