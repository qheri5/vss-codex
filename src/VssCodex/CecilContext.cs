using Mono.Cecil;

namespace VssCodex;

/// <summary>
/// Wraps a Mono.Cecil reader configured to resolve cross-assembly references against the
/// Vintage Story install (root + Lib + Mods). VintagestoryLib references VintagestoryAPI,
/// the mods reference both, etc. — without these search dirs, .Resolve() fails.
/// </summary>
public sealed class CecilContext : IDisposable
{
    private readonly DefaultAssemblyResolver _resolver = new();
    private readonly List<ModuleDefinition> _modules = new();
    public string InstallDir { get; }

    public CecilContext(string installDir)
    {
        InstallDir = installDir;
        _resolver.AddSearchDirectory(installDir);
        _resolver.AddSearchDirectory(Path.Combine(installDir, "Lib"));
        _resolver.AddSearchDirectory(Path.Combine(installDir, "Mods"));
    }

    /// <summary>Read a module by file name (e.g. "VintagestoryAPI.dll"), searching root then Mods.</summary>
    public ModuleDefinition ReadModule(string fileName)
    {
        string path = Path.Combine(InstallDir, fileName);
        if (!File.Exists(path)) path = Path.Combine(InstallDir, "Mods", fileName);
        if (!File.Exists(path)) throw new FileNotFoundException($"Assembly not found: {fileName}", path);

        var module = ModuleDefinition.ReadModule(path, new ReaderParameters
        {
            AssemblyResolver = _resolver,
            ReadingMode = ReadingMode.Deferred,
        });
        _modules.Add(module);
        return module;
    }

    public void Dispose()
    {
        foreach (var m in _modules) m.Dispose();
        _resolver.Dispose();
    }
}
