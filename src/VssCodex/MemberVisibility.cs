using Mono.Cecil;

namespace VssCodex;

/// <summary>
/// Single source of truth for "is this member part of the visible surface", shared by the API doc
/// generator and the changelog snapshot so a member can never appear in one but be missing from the
/// other. Note the two method tests: the docs render "normal" methods only (accessors/ctors handled
/// separately), while the snapshot tracks every accessible method incl. accessors.
/// </summary>
public static class MemberVisibility
{
    public static bool IsVisibleField(FieldDefinition f) =>
        (f.IsPublic || f.IsFamily) && !f.IsSpecialName;

    public static bool IsVisibleProperty(PropertyDefinition p) =>
        (p.GetMethod ?? p.SetMethod) is { IsPublic: true } or { IsFamily: true };

    public static bool IsVisibleEvent(EventDefinition e) =>
        e.AddMethod is { IsPublic: true } or { IsFamily: true };

    /// <summary>Instance constructor exposed to mods (the docs render these in their own subsection).</summary>
    public static bool IsVisibleConstructor(MethodDefinition m) =>
        m.IsConstructor && !m.IsStatic && (m.IsPublic || m.IsFamily);

    /// <summary>A "normal" callable method for the docs: public/protected, excluding accessors/ctors/operators.</summary>
    public static bool IsVisibleMethod(MethodDefinition m)
    {
        if (m.IsSpecialName || m.IsGetter || m.IsSetter || m.IsAddOn || m.IsRemoveOn) return false;
        if (m.Name.StartsWith('<')) return false;
        return m.IsPublic || m.IsFamily;
    }

    /// <summary>Raw accessibility (public/protected) — the snapshot tracks every accessible method.</summary>
    public static bool IsAccessibleMethod(MethodDefinition m) => m.IsPublic || m.IsFamily;
}
