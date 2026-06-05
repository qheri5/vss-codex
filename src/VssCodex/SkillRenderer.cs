using System.Text;

namespace VssCodex;

/// <summary>
/// Render the vss skill from the template (shipped beside the tool): fill the build-info placeholders
/// and inject the absolute reference path, then copy the references/ + examples/ verbatim. The skill
/// is emitted under out/.claude/skills/vss/ so it can be copied into a Claude Code project as-is.
/// </summary>
public static class SkillRenderer
{
    public static void Render(string contentDir, BuildInfo info, string refDir, string skillOut)
    {
        string skillSrc = Path.Combine(contentDir, "skill");
        string tpl = File.ReadAllText(Path.Combine(skillSrc, "SKILL.md.template"));

        var map = new Dictionary<string, string>
        {
            ["VS_VERSION"]     = info.VsVersion,                 ["GENERATED_ON"]  = info.GeneratedOn,
            ["API_TYPES"]      = info.ApiTypes.ToString(),       ["API_NAMESPACES"] = info.ApiNamespaces.ToString(),
            ["COVERAGE_PCT"]   = info.CoveragePct.ToString(),    ["EVENTS"]        = info.Events.ToString(),
            ["ENUMS"]          = info.Enums.ToString(),          ["LIB_TYPES"]     = info.LibTypes.ToString(),
            ["REFERENCE_PATH"] = Path.GetFullPath(refDir),       // absolute, so the copied skill still resolves
        };
        foreach (var kv in map) tpl = tpl.Replace("{{" + kv.Key + "}}", kv.Value);

        Directory.CreateDirectory(skillOut);
        // No BOM (a leading BOM can break YAML frontmatter detection).
        File.WriteAllText(Path.Combine(skillOut, "SKILL.md"), tpl, new UTF8Encoding(false));
        CopyDir(Path.Combine(skillSrc, "references"), Path.Combine(skillOut, "references"));
        CopyDir(Path.Combine(skillSrc, "examples"),   Path.Combine(skillOut, "examples"));
        Console.WriteLine($"  skill rendered -> {skillOut}");
    }

    private static void CopyDir(string src, string dst)
    {
        if (!Directory.Exists(src)) return;
        Directory.CreateDirectory(dst);
        foreach (var f in Directory.GetFiles(src))
            File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), overwrite: true);
    }
}
