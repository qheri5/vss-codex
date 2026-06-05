using System.Diagnostics;

namespace VssCodex;

/// <summary>
/// Decompile the VS-authored assemblies with ilspycmd, invoked as a subprocess (cross-platform).
/// ilspycmd can hit a native stack overflow on a few pathological types; the crash is isolated to the
/// child and it still emits most files. Our docs read the DLLs via Mono.Cecil (not these .cs), so a
/// partial decompile only affects human browsing - we tolerate a non-zero exit and keep going.
/// </summary>
public static class Decompiler
{
    private static readonly string[] CoreAssemblies =
        { "VintagestoryAPI", "VintagestoryLib", "VintagestoryServer", "Vintagestory", "VSCrashReporter", "VSCrashReporterLib", "ModMaker" };
    private static readonly string[] ModAssemblies = { "VSEssentials", "VSSurvivalMod", "VSCreativeMod" };

    public static void Run(string install, string refDir)
    {
        string ilspy = EnsureIlspycmd();
        string dec = Path.Combine(refDir, "decompiled");
        Directory.CreateDirectory(dec);

        var partial = new List<string>();
        int total = CoreAssemblies.Length + ModAssemblies.Length, idx = 0;

        void One(string name, string dll)
        {
            idx++;
            if (!File.Exists(dll)) { Console.WriteLine($"  [{idx}/{total}] missing {name}.dll - skipping"); return; }
            string target = Path.Combine(dec, name);
            if (Directory.Exists(target)) Directory.Delete(target, recursive: true);

            var sw = Stopwatch.StartNew();
            int exit = RunIlspy(ilspy, target, dll);
            sw.Stop();
            int files = Directory.Exists(target) ? Directory.GetFiles(target, "*.cs", SearchOption.AllDirectories).Length : 0;
            if (exit != 0) { partial.Add(name); Console.WriteLine($"  [{idx}/{total}] {name}: PARTIAL ({files} files, exit {exit}) {sw.Elapsed.TotalSeconds:n1}s"); }
            else Console.WriteLine($"  [{idx}/{total}] {name}: {files} files, {sw.Elapsed.TotalSeconds:n1}s");
        }

        foreach (var a in CoreAssemblies) One(a, Path.Combine(install, $"{a}.dll"));
        foreach (var a in ModAssemblies) One(a, Path.Combine(install, "Mods", $"{a}.dll"));
        if (partial.Count > 0) Console.WriteLine($"  note: partial decompile for: {string.Join(", ", partial)}");
        Console.WriteLine($"  decompiled -> {dec}");
    }

    private static int RunIlspy(string ilspy, string target, string dll)
    {
        var psi = new ProcessStartInfo(ilspy)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        psi.ArgumentList.Add("-p");
        psi.ArgumentList.Add("-o");
        psi.ArgumentList.Add(target);
        psi.ArgumentList.Add(dll);
        try
        {
            using var p = new Process { StartInfo = psi };
            p.OutputDataReceived += static (_, _) => { };  // drain so the child can't block
            p.ErrorDataReceived += static (_, _) => { };
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();
            return p.ExitCode;
        }
        catch { return -1; }
    }

    /// <summary>Find ilspycmd on PATH or in the per-user dotnet-tools dir; auto-install it if missing.</summary>
    private static string EnsureIlspycmd()
    {
        string exe = OperatingSystem.IsWindows() ? "ilspycmd.exe" : "ilspycmd";
        if (FindOnPath(exe) is string onPath) return onPath;

        string toolsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".dotnet", "tools");
        string toolPath = Path.Combine(toolsDir, exe);
        if (File.Exists(toolPath)) return toolPath;

        Console.WriteLine("  installing ilspycmd (global dotnet tool) ...");
        var psi = new ProcessStartInfo("dotnet") { UseShellExecute = false };
        psi.ArgumentList.Add("tool"); psi.ArgumentList.Add("install");
        psi.ArgumentList.Add("--global"); psi.ArgumentList.Add("ilspycmd");
        try { using var p = Process.Start(psi); p!.WaitForExit(); } catch { }

        if (File.Exists(toolPath)) return toolPath;
        if (FindOnPath(exe) is string p2) return p2;
        throw new Exception("ilspycmd not found and auto-install failed; install it with: dotnet tool install --global ilspycmd");
    }

    private static string? FindOnPath(string exe)
    {
        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            if (string.IsNullOrWhiteSpace(dir)) continue;
            try { string c = Path.Combine(dir, exe); if (File.Exists(c)) return c; } catch { }
        }
        return null;
    }
}
