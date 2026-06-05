using Mono.Cecil;

namespace VssCodex;

/// <summary>
/// The public API "surface" selection, shared by the doc generators and the changelog snapshot so they
/// can never drift apart. "Surface" = public/protected types with compiler junk dropped; in normal mode
/// restricted to the Vintagestory.API.* namespaces, in engineInternal mode any public engine type.
/// </summary>
public static class ApiSurface
{
    public const string ApiNamespace = "Vintagestory.API";

    /// <summary>A type followed by all of its (transitively) nested types.</summary>
    public static IEnumerable<TypeDefinition> Flatten(TypeDefinition t)
    {
        yield return t;
        foreach (var n in t.NestedTypes)
            foreach (var x in Flatten(n))
                yield return x;
    }

    public static bool IsPublicSurface(TypeDefinition t) =>
        t.IsPublic || t.IsNestedPublic || t.IsNestedFamily || t.IsNestedFamilyOrAssembly;

    public static bool IsCompilerGenerated(TypeDefinition t) =>
        t.Name.StartsWith('<') || t.Name.StartsWith("__") ||
        t.CustomAttributes.Any(a => a.AttributeType.Name == "CompilerGeneratedAttribute");

    /// <summary>
    /// The public-surface types of a module. Normal mode keeps only the Vintagestory.API.* contract;
    /// engineInternal mode keeps every public type (used for the lib/ engine-internal reference).
    /// </summary>
    public static IEnumerable<TypeDefinition> PublicTypes(ModuleDefinition module, bool engineInternal = false)
    {
        var q = module.Types
            .SelectMany(Flatten)
            .Where(IsPublicSurface)
            .Where(t => !IsCompilerGenerated(t));
        // Normal mode: only the real public modding contract. Drops bundled OSS (CompactExifLib,
        // ProperVersion) and engine namespaces that happen to ship public types in the API DLL.
        if (!engineInternal)
            q = q.Where(t => SignatureFormatter.RootNamespace(t).StartsWith(ApiNamespace, StringComparison.Ordinal));
        return q;
    }
}
