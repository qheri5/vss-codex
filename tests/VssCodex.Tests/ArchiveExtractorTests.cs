using System;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Text;
using VssCodex;
using Xunit;

namespace VssCodex.Tests;

public class ArchiveExtractorTests
{
    private static string TempDir()
    {
        string d = Path.Combine(Path.GetTempPath(), "vss-arc-" + Guid.NewGuid().ToString("N")[..10]);
        Directory.CreateDirectory(d);
        return d;
    }

    private static string MakeZip(string dir, params (string name, string content)[] entries)
    {
        string path = Path.Combine(dir, "a.zip");
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            var e = zip.CreateEntry(name);
            using var w = new StreamWriter(e.Open());
            w.Write(content);
        }
        return path;
    }

    [Fact]
    public void ResolveInstall_finds_shallowest_api_dll()
    {
        string dir = TempDir();
        // A decoy deeper copy plus the real shallow one; the shallowest must win.
        string zip = MakeZip(dir,
            ("server/VintagestoryAPI.dll", "x"),
            ("server/deep/a/VintagestoryAPI.dll", "x"));

        string install = ArchiveExtractor.ResolveInstall(zip);
        Assert.True(File.Exists(Path.Combine(install, "VintagestoryAPI.dll")));
        Assert.EndsWith("server", install);
    }

    [Fact]
    public void Extract_tar_gz_happy_path()
    {
        string dir = TempDir();
        string tar = Path.Combine(dir, "a.tar.gz");
        using (var fs = File.Create(tar))
        using (var gz = new GZipStream(fs, CompressionMode.Compress))
        using (var w = new TarWriter(gz))
        {
            byte[] bytes = Encoding.UTF8.GetBytes("x");
            var entry = new PaxTarEntry(TarEntryType.RegularFile, "srv/VintagestoryAPI.dll")
            { DataStream = new MemoryStream(bytes) };
            w.WriteEntry(entry);
        }

        string install = ArchiveExtractor.ResolveInstall(tar);
        Assert.True(File.Exists(Path.Combine(install, "VintagestoryAPI.dll")));
    }

    [Fact]
    public void Zip_slip_entry_is_not_written_outside_dest()
    {
        string dir = TempDir();
        string dest = Path.Combine(dir, "out");
        string zip = MakeZip(dir,
            ("../escape.txt", "pwned"),
            ("ok/inside.txt", "fine"));

        ArchiveExtractor.Extract(zip, dest);

        // The traversal entry must NOT have landed next to dest.
        Assert.False(File.Exists(Path.Combine(dir, "escape.txt")));
        Assert.True(File.Exists(Path.Combine(dest, "ok", "inside.txt")));
    }

    [Fact]
    public void Missing_archive_throws_file_not_found() =>
        Assert.Throws<FileNotFoundException>(() =>
            ArchiveExtractor.ResolveInstall(Path.Combine(Path.GetTempPath(), "nope-" + Guid.NewGuid().ToString("N") + ".zip")));

    [Fact]
    public void Unsupported_extension_throws()
    {
        string dir = TempDir();
        string f = Path.Combine(dir, "a.rar");
        File.WriteAllText(f, "x");
        Assert.Throws<Exception>(() => ArchiveExtractor.Extract(f, Path.Combine(dir, "out")));
    }

    [Fact]
    public void No_api_dll_inside_throws()
    {
        string dir = TempDir();
        string zip = MakeZip(dir, ("server/readme.txt", "no dll here"));
        Assert.Throws<Exception>(() => ArchiveExtractor.ResolveInstall(zip));
    }
}
