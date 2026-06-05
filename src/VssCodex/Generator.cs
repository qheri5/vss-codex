using Mono.Cecil;

namespace VssCodex;

/// <summary>
/// The generation step: reflect over the VS binaries with Mono.Cecil and emit the API reference,
/// events/enums indexes, the engine-internal lib/ surface, the Harmony patchability catalog, the
/// version-diff CHANGELOG, and build-info.json. Returns the BuildInfo for the skill render + summary.
/// Pure compute over the binaries — same inputs, same outputs.
/// </summary>
public static class Generator
{
    /// <param name="install">VS install dir holding the DLLs + VintagestoryAPI.xml.</param>
    /// <param name="genRoot">Output root: the reference's docs/generated dir.</param>
    public static BuildInfo Run(string install, string genRoot)
    {
        string genDate = DateTime.Now.ToString("yyyy-MM-dd");
        string apiDir = Path.Combine(genRoot, "api");
        string harmonyDir = Path.Combine(genRoot, "harmony");
        // The decompiled .cs tree lives at <reference>/decompiled (genRoot = <reference>/docs/generated);
        // the API pages link into it for "view source".
        string decompiledRoot = Path.Combine(Directory.GetParent(genRoot)!.Parent!.FullName, "decompiled");

        // Clean generated output, but preserve the hand-written curated guide + the version snapshot.
        CleanGenerated(apiDir, harmonyDir);

        using var ctx = new CecilContext(install);

        var xml = new XmlDocIndex(Path.Combine(install, "VintagestoryAPI.xml"));
        Console.WriteLine($"  XML docs: {(xml.Loaded ? $"{xml.Count} members" : "NOT FOUND")}");
        var inherit = new InheritDocResolver(xml);

        var apiModule = ctx.ReadModule("VintagestoryAPI.dll");
        // The assembly version is unreliable across builds (older VS stamps 1.0.0.0). Use the real
        // game version from the API's GameVersion.ShortGameVersion const; fall back to the asm version.
        string? shortVersion = apiModule.GetType("Vintagestory.API.Config.GameVersion")?.Fields
                .FirstOrDefault(f => f.Name == "ShortGameVersion" && f.IsLiteral)?.Constant as string;
        string version = shortVersion ?? apiModule.Assembly.Name.Version?.ToString() ?? "0.0.0";
        if (shortVersion == null)
            Console.WriteLine($"  warning: GameVersion.ShortGameVersion not found; falling back to assembly version {version}");

        var (apiTypes, apiNs, apiDocumented) = new ApiReferenceGenerator(xml, inherit, genDate).Generate(apiModule, apiDir, decompiledRoot: decompiledRoot);
        Console.WriteLine($"  API   : {apiTypes} public types in {apiNs} namespaces");

        var (evCount, enCount) = new EventsEnumsGenerator(xml, inherit, genDate).Generate(apiModule, apiDir);
        Console.WriteLine($"  Index : {evCount} events, {enCount} enums");

        string[] harmonyAssemblies =
        [
            "VintagestoryLib.dll", "VintagestoryServer.dll", "Vintagestory.dll",
            "VSEssentials.dll", "VSSurvivalMod.dll", "VSCreativeMod.dll",
        ];

        var hgen = new HarmonyTargetGenerator(genDate);
        var stats = new List<HarmonyTargetGenerator.AssemblyStats>();
        var harmonyModules = new List<ModuleDefinition>();
        ModuleDefinition? libModule = null;
        foreach (var dll in harmonyAssemblies)
        {
            try
            {
                var module = ctx.ReadModule(dll);
                harmonyModules.Add(module);
                if (dll == "VintagestoryLib.dll") libModule = module;
                var s = hgen.Generate(module, harmonyDir);
                stats.Add(s);
                Console.WriteLine($"  Harmony: {s.Name,-20} {s.Patchable}/{s.Total} patchable");
            }
            catch (Exception ex)
            {
                // Expected for server-only installs (client assemblies absent) - informational.
                Console.WriteLine($"  skip {dll}: {ex.Message}");
            }
        }
        hgen.WriteIndex(stats, harmonyDir);

        int libTypes = 0;
        if (libModule != null)
        {
            // inherit:null keeps this fast; engine internals are signature-first.
            (libTypes, int libNs, _) = new ApiReferenceGenerator(xml, null, genDate)
                .Generate(libModule, Path.Combine(apiDir, "lib"), engineInternal: true, decompiledRoot: decompiledRoot);
            Console.WriteLine($"  Lib   : {libTypes} public engine types in {libNs} namespaces");
        }

        var snapshot = SymbolSnapshot.Build(version, apiModule, harmonyModules);
        string? changelog = new ChangelogGenerator(genDate).Process(snapshot, genRoot);
        Console.WriteLine(changelog != null
            ? $"  Diff  : wrote {changelog}"
            : $"  Diff  : snapshot saved for v{version} (no prior version to diff)");

        var info = new BuildInfo
        {
            VsVersion = version,
            GeneratedOn = genDate,
            ApiTypes = apiTypes,
            ApiNamespaces = apiNs,
            ApiTypesDocumented = apiDocumented,
            CoveragePct = apiTypes == 0 ? 0 : (int)Math.Round(100.0 * apiDocumented / apiTypes),
            Events = evCount,
            Enums = enCount,
            LibTypes = libTypes,
            Harmony = stats.ToDictionary(s => s.Name, s => $"{s.Patchable}/{s.Total}"),
        };
        info.Save(genRoot);
        return info;
    }

    private static void CleanGenerated(string apiDir, string harmonyDir)
    {
        if (Directory.Exists(apiDir)) Directory.Delete(apiDir, recursive: true);
        if (Directory.Exists(harmonyDir))
        {
            // delete only generated files; KEEP the hand-written high-value-targets.md
            foreach (var f in Directory.GetFiles(harmonyDir))
            {
                string name = Path.GetFileName(f);
                if (name == "INDEX.md" || name.StartsWith("patchable-", StringComparison.Ordinal))
                    File.Delete(f);
            }
        }
    }
}
