using System.IO.Compression;
using System.Formats.Tar;

namespace VssCodex;

/// <summary>
/// Converter mode: extract a VS server/client archive (.zip / .tar.gz) and auto-locate the binaries.
/// Pure .NET (ZipFile + GZipStream + TarFile) - no external tools, works on Windows/Linux/macOS.
/// </summary>
public static class ArchiveExtractor
{
    public static string ResolveInstall(string archive)
    {
        if (!File.Exists(archive)) throw new FileNotFoundException($"archive not found: {archive}");

        string name = Path.GetFileNameWithoutExtension(archive);
        if (name.EndsWith(".tar", StringComparison.OrdinalIgnoreCase)) name = name[..^4];
        string dest = Path.Combine(Path.GetTempPath(), "vss-codex-extract", name);
        if (Directory.Exists(dest)) Directory.Delete(dest, recursive: true);
        Directory.CreateDirectory(dest);

        Console.WriteLine($"  extracting {Path.GetFileName(archive)} ...");
        string lower = archive.ToLowerInvariant();
        if (lower.EndsWith(".zip"))
        {
            ZipFile.ExtractToDirectory(archive, dest);
        }
        else if (lower.EndsWith(".tar.gz") || lower.EndsWith(".tgz"))
        {
            using var fs = File.OpenRead(archive);
            using var gz = new GZipStream(fs, CompressionMode.Decompress);
            TarFile.ExtractToDirectory(gz, dest, overwriteFiles: true);
        }
        else
        {
            throw new Exception($"unsupported archive type (expected .zip or .tar.gz): {archive}");
        }

        // The folder that holds VintagestoryAPI.dll (shallowest match).
        var api = new DirectoryInfo(dest).GetFiles("VintagestoryAPI.dll", SearchOption.AllDirectories)
            .OrderBy(f => f.FullName.Count(c => c is '/' or '\\'))
            .FirstOrDefault()
            ?? throw new Exception($"VintagestoryAPI.dll not found inside {archive} - is this a VS server/client archive?");

        Console.WriteLine($"  binaries -> {api.DirectoryName}");
        return api.DirectoryName!;
    }
}
