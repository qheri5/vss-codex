namespace VssCodex;

/// <summary>Small cross-platform helpers shared by the steps that shell out to external tools.</summary>
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
}
