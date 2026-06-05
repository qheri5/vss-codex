using System.IO;
using VssCodex;
using Xunit;

namespace VssCodex.Tests;

// Exercise the markdown generators end-to-end against this test assembly (read back via Cecil), with
// no XML docs and no VS binaries. engineInternal mode drops the Vintagestory.API namespace filter, so
// the fixtures are picked up.
public class GeneratorTests
{
    private static XmlDocIndex NoXml() => new(null); // not loaded -> no summaries

    [Fact]
    public void ApiReference_engineInternal_emits_index_and_type_pages()
    {
        string dir = Directory.CreateTempSubdirectory().FullName;
        var (types, namespaces, documented) =
            new ApiReferenceGenerator(NoXml(), null, "2026-01-01").Generate(TestModule.Self, dir, engineInternal: true);

        Assert.True(types > 0);
        Assert.True(namespaces >= 1);
        Assert.Equal(0, documented); // no XML loaded

        Assert.True(File.Exists(Path.Combine(dir, "INDEX.md")));
        Assert.Contains("VssCodex.Tests.Fixtures", File.ReadAllText(Path.Combine(dir, "INDEX.md")));

        string ns = Path.Combine(dir, "VssCodex.Tests.Fixtures.md");
        Assert.True(File.Exists(ns));
        string md = File.ReadAllText(ns);
        Assert.Contains("## Simple", md);
        Assert.Contains("EnumFixture", md);
    }

    [Fact]
    public void ApiReference_normal_mode_filters_to_api_namespace()
    {
        string dir = Directory.CreateTempSubdirectory().FullName;
        // The fixtures are not under Vintagestory.API, so normal mode yields nothing.
        var (types, _, _) =
            new ApiReferenceGenerator(NoXml(), null, "2026-01-01").Generate(TestModule.Self, dir, engineInternal: false);
        Assert.Equal(0, types);
    }

    [Fact]
    public void ApiReference_emits_view_source_link_only_when_the_cs_exists()
    {
        string root = Directory.CreateTempSubdirectory().FullName;
        string dir = Path.Combine(root, "out");
        string decompiled = Path.Combine(root, "decompiled");
        // The generator derives the assembly folder as label.Split('.')[0] -> "VssCodex" for this test asm.
        string csDir = Path.Combine(decompiled, "VssCodex", "VssCodex.Tests.Fixtures");
        Directory.CreateDirectory(csDir);
        File.WriteAllText(Path.Combine(csDir, "Simple.cs"), "// decompiled");

        new ApiReferenceGenerator(NoXml(), null, "2026-01-01")
            .Generate(TestModule.Self, dir, engineInternal: true, decompiledRoot: decompiled);

        string md = File.ReadAllText(Path.Combine(dir, "VssCodex.Tests.Fixtures.md"));
        Assert.Contains("[view decompiled source](/decompiled/VssCodex/VssCodex.Tests.Fixtures/Simple.cs)", md);
        // a type whose .cs we did not create gets no link
        Assert.DoesNotContain("/decompiled/VssCodex/VssCodex.Tests.Fixtures/Outer.cs", md);
    }

    [Fact]
    public void ApiReference_engineInternal_does_not_flag_signature_only_types()
    {
        // Engine assemblies have no XML at all and carry a global "signatures only" banner, so the
        // per-type "No official summary" badge would be pure noise across hundreds of types.
        string dir = Directory.CreateTempSubdirectory().FullName;
        new ApiReferenceGenerator(NoXml(), null, "2026-01-01").Generate(TestModule.Self, dir, engineInternal: true);
        string md = File.ReadAllText(Path.Combine(dir, "VssCodex.Tests.Fixtures.md"));
        Assert.DoesNotContain("No official summary", md);
    }

    [Fact]
    public void ApiReference_index_explains_the_signature_only_flag_in_normal_mode()
    {
        // Normal mode filters to Vintagestory.API.* (zero test types), but the INDEX coverage note is
        // still written and must tell the reader what the signature-only flag means.
        string dir = Directory.CreateTempSubdirectory().FullName;
        new ApiReferenceGenerator(NoXml(), null, "2026-01-01").Generate(TestModule.Self, dir, engineInternal: false);
        string idx = File.ReadAllText(Path.Combine(dir, "INDEX.md"));
        Assert.Contains("signature-only", idx);
    }

    [Fact]
    public void ApiReference_small_namespace_gets_no_overview_list()
    {
        // The per-namespace "Types in this namespace" overview only fires for large namespaces; the
        // small fixtures namespace must not get one (it would be noise).
        string dir = Directory.CreateTempSubdirectory().FullName;
        new ApiReferenceGenerator(NoXml(), null, "2026-01-01").Generate(TestModule.Self, dir, engineInternal: true);
        string md = File.ReadAllText(Path.Combine(dir, "VssCodex.Tests.Fixtures.md"));
        Assert.DoesNotContain("Types in this namespace", md);
    }

    [Fact]
    public void EventsEnums_runs_and_writes_pages()
    {
        string dir = Directory.CreateTempSubdirectory().FullName;
        var (events, enums) = new EventsEnumsGenerator(NoXml(), null, "2026-01-01").Generate(TestModule.Self, dir);
        Assert.Equal(0, events); // normal-mode API filter -> no test types match
        Assert.Equal(0, enums);
        Assert.True(File.Exists(Path.Combine(dir, "events.md")));
        Assert.True(File.Exists(Path.Combine(dir, "enums.md")));
    }
}
