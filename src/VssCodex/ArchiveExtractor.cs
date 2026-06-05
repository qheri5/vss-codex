using System.IO.Compression;
using System.Formats.Tar;

namespace VssCodex;

/// <summary>
/// Converter mode: extract a VS server/client archive (.zip / .tar.gz) and auto-locate the binaries.
/// Pure .NET (ZipArchive + GZipStream + TarReader) - no external tools, works on Windows/Linux/macOS.
/// Extraction is entry-by-entry and hardened: every entry must resolve to a path under the destination
/// (zip-slip / tar-slip guard) and symlink/hardlink entries are skipped (a link-escape vector).
/// </summary>
public static class ArchiveExtractor
{
    public static string ResolveInstall(string archive)
    {
        if (!File.Exists(archive)) throw new FileNotFoundException($"archive not found: {archive}");

        // A unique per-run dir: a deterministic name keyed on the archive would let concurrent or stale
        // runs delete each other's tree mid-use.
        string dest = Path.Combine(Path.GetTempPath(), "vss-codex-extract", Guid.NewGuid().ToString("N")[..12]);
        Directory.CreateDirectory(dest);

        Console.WriteLine($"  extracting {Path.GetFileName(archive)} ...");
        Extract(archive, dest);

        // The folder that holds VintagestoryAPI.dll (shallowest match).
        var api = new DirectoryInfo(dest).GetFiles("VintagestoryAPI.dll", SearchOption.AllDirectories)
            .OrderBy(f => f.FullName.Count(c => c is '/' or '\\'))
            .FirstOrDefault()
            ?? throw new Exception($"VintagestoryAPI.dll not found inside {archive} - is this a VS server/client archive?");

        Console.WriteLine($"  binaries -> {api.DirectoryName}");
        return api.DirectoryName!;
    }

    /// <summary>Extract a .zip or .tar.gz into <paramref name="dest"/> with the safety guards above.</summary>
    public static void Extract(string archive, string dest)
    {
        string lower = archive.ToLowerInvariant();
        if (lower.EndsWith(".zip"))
            ExtractZip(archive, dest);
        else if (lower.EndsWith(".tar.gz") || lower.EndsWith(".tgz"))
            ExtractTarGz(archive, dest);
        else
            throw new Exception($"unsupported archive type (expected .zip or .tar.gz): {archive}");
    }

    private static void ExtractZip(string archive, string dest)
    {
        using var zip = ZipFile.OpenRead(archive);
        foreach (var entry in zip.Entries)
        {
            if (SafeTarget(dest, entry.FullName) is not string target) continue; // escapes dest -> skip
            if (entry.FullName.EndsWith('/') || entry.FullName.EndsWith('\\') || entry.Name.Length == 0)
            {
                Directory.CreateDirectory(target);
                continue;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, overwrite: true);
        }
    }

    private static void ExtractTarGz(string archive, string dest)
    {
        using var fs = File.OpenRead(archive);
        using var gz = new GZipStream(fs, CompressionMode.Decompress);
        using var tar = new TarReader(gz);
        while (tar.GetNextEntry() is { } entry)
        {
            // Skip link entries outright: a symlink/hardlink can point outside dest and let a later
            // entry write through it.
            if (entry.EntryType is TarEntryType.SymbolicLink or TarEntryType.HardLink) continue;
            if (SafeTarget(dest, entry.Name) is not string target) continue;

            if (entry.EntryType is TarEntryType.Directory)
            {
                Directory.CreateDirectory(target);
            }
            else if (entry.EntryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                entry.ExtractToFile(target, overwrite: true);
            }
            // other types (devices, fifos) are ignored
        }
    }

    /// <summary>
    /// Resolve an archive entry name to an absolute path under <paramref name="dest"/>, or null if it
    /// would escape (absolute path or `..` traversal). This is the zip-slip / tar-slip guard.
    /// </summary>
    private static string? SafeTarget(string dest, string entryName)
    {
        string rel = entryName.Replace('\\', '/').TrimStart('/');
        if (rel.Length == 0) return null;
        string root = Path.GetFullPath(dest);
        string full = Path.GetFullPath(Path.Combine(root, rel));
        string prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        return full.StartsWith(prefix, StringComparison.Ordinal) ? full : null;
    }
}
