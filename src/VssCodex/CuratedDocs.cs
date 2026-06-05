namespace VssCodex;

/// <summary>Install the committable curated docs (shipped beside the tool) into the reference tree.</summary>
public static class CuratedDocs
{
    public static void Install(string contentDir, string refDir)
    {
        string src = Path.Combine(contentDir, "docs-src");
        string docs = Path.Combine(refDir, "docs");
        string harmony = Path.Combine(docs, "generated", "harmony");
        Directory.CreateDirectory(docs);
        Directory.CreateDirectory(harmony);

        Copy(Path.Combine(src, "ref-README.md"),         Path.Combine(refDir, "README.md"));
        Copy(Path.Combine(src, "kb-README.md"),          Path.Combine(docs, "README.md"));
        Copy(Path.Combine(src, "entity-simulation.md"),  Path.Combine(docs, "entity-simulation.md"));
        Copy(Path.Combine(src, "high-value-targets.md"), Path.Combine(harmony, "high-value-targets.md"));
        Console.WriteLine($"  curated docs installed -> {docs}");
    }

    private static void Copy(string src, string dst)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
        File.Copy(src, dst, overwrite: true);
    }
}
