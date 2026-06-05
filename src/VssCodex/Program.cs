using Mono.Cecil;
using VssCodex;

// VssCodex — emit the API (Doc A) and Harmony (Doc B) markdown references from the local VS binaries,
// plus cross-cutting indexes (events/enums), an engine-internal type surface (VintagestoryLib), and a
// version-diff CHANGELOG.
//   Usage: VssCodex --install "<VS install dir>" --out "<generated docs dir>"
//   Defaults: --install %APPDATA%\Vintagestory
// The --out dir MUST be the gitignored vs-game-reference/docs/generated tree (the PowerShell wrapper
// enforces this). Output is proprietary; never commit it.

string install = Environment.ExpandEnvironmentVariables(@"%APPDATA%\Vintagestory");
string? outDir = null;

for (int i = 0; i < args.Length - 1; i++)
{
    if (args[i] is "--install" or "-i") install = args[++i];
    else if (args[i] is "--out" or "-o") outDir = args[++i];
}

if (outDir is null)
{
    Console.Error.WriteLine("error: --out <dir> is required (the gitignored docs/generated tree).");
    return 1;
}
if (!Directory.Exists(install))
{
    Console.Error.WriteLine($"error: VS install dir not found: {install}");
    return 1;
}

try
{

string genDate = DateTime.Now.ToString("yyyy-MM-dd");
string apiDir = Path.Combine(outDir, "api");
string harmonyDir = Path.Combine(outDir, "harmony");

// Clean generated output, but preserve the hand-written curated guide + the version snapshot.
CleanGenerated(apiDir, harmonyDir);

using var ctx = new CecilContext(install);

// --- Doc A: API reference (VintagestoryAPI.dll + VintagestoryAPI.xml) ---
var xml = new XmlDocIndex(Path.Combine(install, "VintagestoryAPI.xml"));
Console.WriteLine($"XML docs: {(xml.Loaded ? $"{xml.Count} members" : "NOT FOUND")}");
var inherit = new InheritDocResolver(xml);

var apiModule = ctx.ReadModule("VintagestoryAPI.dll");
// The assembly version is unreliable across builds (older VS stamps 1.0.0.0). Use the real game
// version from the API's GameVersion.ShortGameVersion const; fall back to the assembly version.
string version = apiModule.GetType("Vintagestory.API.Config.GameVersion")?.Fields
        .FirstOrDefault(f => f.Name == "ShortGameVersion" && f.IsLiteral)?.Constant as string
    ?? apiModule.Assembly.Name.Version.ToString();

var (apiTypes, apiNs, apiDocumented) = new ApiReferenceGenerator(xml, inherit, genDate).Generate(apiModule, apiDir);
Console.WriteLine($"API   : {apiTypes} public types in {apiNs} namespaces -> {apiDir}");

var (evCount, enCount) = new EventsEnumsGenerator(xml, inherit, genDate).Generate(apiModule, apiDir);
Console.WriteLine($"Index : {evCount} events, {enCount} enums -> events.md / enums.md");

// --- Doc B: Harmony-patchable catalog (engine internals + first-party mods) ---
string[] harmonyAssemblies =
{
    "VintagestoryLib.dll", "VintagestoryServer.dll", "Vintagestory.dll",
    "VSEssentials.dll", "VSSurvivalMod.dll", "VSCreativeMod.dll",
};

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
        Console.WriteLine($"Harmony: {s.Name,-20} {s.Patchable}/{s.Total} patchable -> {s.File}");
    }
    catch (Exception ex)
    {
        // Expected for server-only installs (client assemblies absent) — informational, not an error.
        Console.WriteLine($"  skip {dll}: {ex.Message}");
    }
}
hgen.WriteIndex(stats, harmonyDir);

// --- Engine-internal type surface: VintagestoryLib public types -> api/lib/ (signatures only) ---
int libTypes = 0;
if (libModule != null)
{
    // inherit:null keeps this fast (no per-member cross-assembly resolution); engine internals are signature-first.
    int libNs;
    (libTypes, libNs, _) = new ApiReferenceGenerator(xml, null, genDate)
        .Generate(libModule, Path.Combine(apiDir, "lib"), engineInternal: true);
    Console.WriteLine($"Lib   : {libTypes} public engine types in {libNs} namespaces -> api/lib/");
}

// --- Version-diff CHANGELOG (snapshot now, diff vs previous run of a different version) ---
var snapshot = SymbolSnapshot.Build(version, apiModule, harmonyModules);
string? changelog = new ChangelogGenerator(genDate).Process(snapshot, outDir);
Console.WriteLine(changelog != null
    ? $"Diff  : wrote {changelog}"
    : $"Diff  : snapshot saved for v{version} (no prior version to diff)");

// --- build-info.json: handoff to the formatter (skill template + summary) ---
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
info.Save(outDir);

Console.WriteLine($"Done. Generated docs under: {outDir}");
return 0;

}
catch (FileNotFoundException ex)
{
    Console.Error.WriteLine($"error: required assembly not found - {ex.Message}");
    Console.Error.WriteLine("       check that --install points at a Vintage Story install/extract with the VS-authored DLLs.");
    return 2;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"error: generation failed - {ex.GetType().Name}: {ex.Message}");
    return 2;
}

static void CleanGenerated(string apiDir, string harmonyDir)
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
