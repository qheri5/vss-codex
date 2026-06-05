using System.Text;
using Mono.Cecil;

namespace VssCodex;

/// <summary>
/// Builds the XML-documentation member IDs (the strings used in the &lt;member name="..."&gt;
/// attributes of VintagestoryAPI.xml) from Cecil members, so we can look up the official
/// &lt;summary&gt; for each type/method/property/field/event.
///
/// Format reference: ECMA-334 / Microsoft "Processing the XML File" doc-comment ID strings.
///   Type:     T:Namespace.Outer.Nested
///   Method:   M:Namespace.Type.Method(ParamType,ParamType)  (#ctor for constructors, `n method arity)
///   Property: P:Namespace.Type.Name
///   Field:    F:Namespace.Type.Name
///   Event:    E:Namespace.Type.Name
/// Type-parameter refs use `N (declaring type) and ``N (method); arrays [], byref @, ptr *,
/// generic instances {Arg,Arg}.
/// </summary>
public static class DocId
{
    public static string ForType(TypeDefinition t) => "T:" + TypeFullName(t);

    public static string ForField(FieldDefinition f) => "F:" + TypeFullName(f.DeclaringType) + "." + f.Name;

    public static string ForEvent(EventDefinition e) => "E:" + TypeFullName(e.DeclaringType) + "." + e.Name;

    public static string ForProperty(PropertyDefinition p)
    {
        var sb = new StringBuilder("P:");
        sb.Append(TypeFullName(p.DeclaringType)).Append('.').Append(p.Name);
        if (p.HasParameters) // indexers
            AppendParameters(sb, p.Parameters, null);
        return sb.ToString();
    }

    public static string ForMethod(MethodDefinition m)
    {
        var sb = new StringBuilder("M:");
        sb.Append(TypeFullName(m.DeclaringType)).Append('.');
        sb.Append(m.Name.Replace('.', '#')); // .ctor -> #ctor, .cctor -> #cctor
        if (m.HasGenericParameters)
            sb.Append("``").Append(m.GenericParameters.Count);
        AppendParameters(sb, m.Parameters, m);
        // conversion operators encode return type: op_Implicit/op_Explicit -> ~ReturnType
        if (m.Name is "op_Implicit" or "op_Explicit")
            sb.Append('~').Append(TypeDocString(m.ReturnType, m));
        return sb.ToString();
    }

    private static void AppendParameters(StringBuilder sb, IList<ParameterDefinition> ps, MethodDefinition? owner)
    {
        if (ps.Count == 0) return;
        sb.Append('(');
        for (int i = 0; i < ps.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(TypeDocString(ps[i].ParameterType, owner));
        }
        sb.Append(')');
    }

    /// <summary>Full type name with nested types joined by '.' and arity backticks preserved.</summary>
    private static string TypeFullName(TypeReference t)
    {
        if (t.IsNested)
            return TypeFullName(t.DeclaringType) + "." + t.Name;
        string ns = t.Namespace;
        return string.IsNullOrEmpty(ns) ? t.Name : ns + "." + t.Name;
    }

    /// <summary>Doc-id encoding of a type *reference* as it appears inside a parameter list.</summary>
    private static string TypeDocString(TypeReference t, MethodDefinition? owner)
    {
        switch (t)
        {
            case ArrayType at:
            {
                string elem = TypeDocString(at.ElementType, owner);
                if (at.Rank == 1) return elem + "[]";
                // multi-dim: [lo:size,lo:size,...] — Cecil exposes dimensions; emit canonical "[0:,0:]"
                var dims = string.Join(",", Enumerable.Repeat("0:", at.Rank));
                return elem + "[" + dims + "]";
            }
            case ByReferenceType brt:
                return TypeDocString(brt.ElementType, owner) + "@";
            case PointerType pt:
                return TypeDocString(pt.ElementType, owner) + "*";
            case GenericParameter gp:
                return gp.Type == GenericParameterType.Method ? "``" + gp.Position : "`" + gp.Position;
            case GenericInstanceType git:
            {
                var sb = new StringBuilder();
                sb.Append(GenericBaseName(git.ElementType));
                sb.Append('{');
                for (int i = 0; i < git.GenericArguments.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(TypeDocString(git.GenericArguments[i], owner));
                }
                sb.Append('}');
                return sb.ToString();
            }
            default:
                return TypeFullName(t);
        }
    }

    /// <summary>Generic type name without the `n arity suffix (the args go in {} instead).</summary>
    private static string GenericBaseName(TypeReference t)
    {
        string full = TypeFullName(t);
        int tick = full.IndexOf('`');
        return tick < 0 ? full : full[..tick];
    }
}
