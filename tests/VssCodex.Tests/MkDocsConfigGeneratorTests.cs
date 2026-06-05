using System.IO;
using VssCodex;
using Xunit;

namespace VssCodex.Tests;

public class MkDocsConfigGeneratorTests
{
    // Build a small docs/ tree mirroring the real layout and return its path.
    private static string MakeDocs(bool withLib = true)
    {
        string docs = Path.Combine(Directory.CreateTempSubdirectory().FullName, "docs");
        void W(string rel, string body = "# x\n")
        {
            string full = Path.Combine(docs, rel.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, body);
        }

        W("README.md");
        W("entity-simulation.md");
        W("generated/api/INDEX.md");
        W("generated/api/Vintagestory.API.Server.md");
        W("generated/api/Vintagestory.API.Common.md");
        W("generated/api/events.md");
        W("generated/api/enums.md");
        if (withLib)
        {
            W("generated/api/lib/INDEX.md");
            W("generated/api/lib/Vintagestory.Common.md");
        }
        W("generated/harmony/INDEX.md");
        W("generated/harmony/high-value-targets.md");
        W("generated/harmony/patchable-Vintagestory.md");
        return docs;
    }

    private static readonly BuildInfo Info = new() { VsVersion = "1.22.3" };

    [Fact]
    public void Emits_material_theme_search_and_portable_links()
    {
        string yaml = MkDocsConfigGenerator.BuildYaml(MakeDocs(), Info);
        Assert.Contains("name: material", yaml);
        Assert.Contains("- search", yaml);
        Assert.Contains("use_directory_urls: false", yaml);
        Assert.Contains("docs_dir: docs", yaml);
        Assert.Contains("site_dir: site", yaml);
    }

    [Fact]
    public void Site_name_includes_vs_version()
    {
        string yaml = MkDocsConfigGenerator.BuildYaml(MakeDocs(), Info);
        Assert.Contains("1.22.3", yaml);
    }

    [Fact]
    public void Nav_is_ordered_home_then_api_then_harmony()
    {
        string yaml = MkDocsConfigGenerator.BuildYaml(MakeDocs(), Info);
        int home = yaml.IndexOf("README.md");
        int api = yaml.IndexOf("'API':");
        int harmony = yaml.IndexOf("'Harmony':");
        Assert.True(home >= 0 && api >= 0 && harmony >= 0);
        Assert.True(home < api, "Home must come before the API section");
        Assert.True(api < harmony, "API must come before Harmony");
    }

    [Fact]
    public void Nav_lists_api_pages_and_indexes()
    {
        string yaml = MkDocsConfigGenerator.BuildYaml(MakeDocs(), Info);
        Assert.Contains("generated/api/INDEX.md", yaml);
        Assert.Contains("generated/api/Vintagestory.API.Server.md", yaml);
        Assert.Contains("generated/api/events.md", yaml);
        Assert.Contains("generated/harmony/patchable-Vintagestory.md", yaml);
    }

    [Fact]
    public void Absent_lib_is_not_referenced()
    {
        string yaml = MkDocsConfigGenerator.BuildYaml(MakeDocs(withLib: false), Info);
        Assert.DoesNotContain("/lib/", yaml);
        Assert.DoesNotContain("Engine internals", yaml);
    }

    [Theory]
    [InlineData("README.md", "Home")]
    [InlineData("generated/api/INDEX.md", "Overview")]
    [InlineData("generated/api/events.md", "Events")]
    [InlineData("generated/api/Vintagestory.API.Server.md", "Vintagestory.API.Server")]
    [InlineData("generated/harmony/patchable-VSEssentials.md", "Patchable: VSEssentials")]
    [InlineData("generated/CHANGELOG-1.0-to-1.1.md", "1.0 -> 1.1")]
    public void TitleFor_maps_filenames_to_labels(string rel, string expected) =>
        Assert.Equal(expected, MkDocsConfigGenerator.TitleFor(rel));

    [Fact]
    public void Changelog_files_get_a_version_diffs_section()
    {
        string docs = MakeDocs();
        File.WriteAllText(Path.Combine(docs, "generated", "CHANGELOG-1.0-to-1.1.md"), "# diff\n");

        string yaml = MkDocsConfigGenerator.BuildYaml(docs, Info);
        Assert.Contains("Version diffs", yaml);
        Assert.Contains("generated/CHANGELOG-1.0-to-1.1.md", yaml);
    }
}
