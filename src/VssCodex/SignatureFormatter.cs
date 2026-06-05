using System.Text;
using Mono.Cecil;

namespace VssCodex;

/// <summary>
/// Renders Cecil members as readable, C#-like signatures for the markdown docs.
/// (Distinct from <see cref="DocId"/>, which renders the machine doc-comment ids.)
/// </summary>
public static class SignatureFormatter
{
    private static readonly Dictionary<string, string> Primitives = new(StringComparer.Ordinal)
    {
        ["System.Void"] = "void", ["System.Boolean"] = "bool", ["System.Byte"] = "byte",
        ["System.SByte"] = "sbyte", ["System.Char"] = "char", ["System.Int16"] = "short",
        ["System.UInt16"] = "ushort", ["System.Int32"] = "int", ["System.UInt32"] = "uint",
        ["System.Int64"] = "long", ["System.UInt64"] = "ulong", ["System.Single"] = "float",
        ["System.Double"] = "double", ["System.Decimal"] = "decimal", ["System.String"] = "string",
        ["System.Object"] = "object", ["System.IntPtr"] = "nint", ["System.UIntPtr"] = "nuint",
    };

    /// <summary>Readable type name: int, List&lt;string&gt;, Vec3d[], etc.</summary>
    public static string TypeName(TypeReference t)
    {
        switch (t)
        {
            case ArrayType at:
                return TypeName(at.ElementType) + "[" + new string(',', at.Rank - 1) + "]";
            case ByReferenceType brt:
                return TypeName(brt.ElementType); // ref/out shown at the parameter level
            case PointerType pt:
                return TypeName(pt.ElementType) + "*";
            case GenericParameter gp:
                return gp.Name;
            case GenericInstanceType git:
            {
                // Nullable<T> -> T?
                if (git.ElementType.FullName == "System.Nullable`1")
                    return TypeName(git.GenericArguments[0]) + "?";
                var sb = new StringBuilder(StripArity(BareName(git.ElementType)));
                sb.Append('<');
                for (int i = 0; i < git.GenericArguments.Count; i++)
                {
                    if (i > 0) sb.Append(", ");
                    sb.Append(TypeName(git.GenericArguments[i]));
                }
                sb.Append('>');
                return sb.ToString();
            }
            default:
                if (Primitives.TryGetValue(t.FullName, out var p)) return p;
                return StripArity(BareName(t));
        }
    }

    public static string MethodSignature(MethodDefinition m, bool withModifiers = true)
    {
        var sb = new StringBuilder();
        if (withModifiers) sb.Append(Modifiers(m));

        bool isCtor = m.IsConstructor;
        if (!isCtor) sb.Append(TypeName(m.ReturnType)).Append(' ');
        sb.Append(isCtor ? StripArity(m.DeclaringType.Name) : m.Name);

        if (m.HasGenericParameters)
            sb.Append('<').Append(string.Join(", ", m.GenericParameters.Select(g => g.Name))).Append('>');

        sb.Append('(');
        for (int i = 0; i < m.Parameters.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(Parameter(m.Parameters[i]));
        }
        sb.Append(')');
        return sb.ToString();
    }

    public static string PropertySignature(PropertyDefinition p)
    {
        var sb = new StringBuilder();
        var accessor = p.GetMethod ?? p.SetMethod!;
        if (accessor.IsStatic) sb.Append("static ");
        sb.Append(TypeName(p.PropertyType)).Append(' ').Append(p.Name);
        if (p.HasParameters) // indexer
            sb.Append("[").Append(string.Join(", ", p.Parameters.Select(Parameter))).Append("]");
        sb.Append(" { ");
        if (p.GetMethod is { IsPublic: true } or { IsFamily: true }) sb.Append("get; ");
        if (p.SetMethod is { IsPublic: true } or { IsFamily: true }) sb.Append("set; ");
        sb.Append('}');
        return sb.ToString();
    }

    public static string FieldSignature(FieldDefinition f)
    {
        var sb = new StringBuilder();
        if (f.IsLiteral) sb.Append("const ");
        else
        {
            if (f.IsStatic) sb.Append("static ");
            if (f.IsInitOnly) sb.Append("readonly ");
        }
        sb.Append(TypeName(f.FieldType)).Append(' ').Append(f.Name);
        return sb.ToString();
    }

    public static string EventSignature(EventDefinition e)
    {
        var add = e.AddMethod;
        string mod = add is { IsStatic: true } ? "static " : "";
        return $"{mod}event {TypeName(e.EventType)} {e.Name}";
    }

    private static string Parameter(ParameterDefinition pd)
    {
        var sb = new StringBuilder();
        if (pd.ParameterType is ByReferenceType)
            sb.Append(pd.IsOut ? "out " : (pd.IsIn ? "in " : "ref "));
        if (pd.HasCustomAttributes && pd.CustomAttributes.Any(a => a.AttributeType.Name == "ParamArrayAttribute"))
            sb.Append("params ");
        sb.Append(TypeName(pd.ParameterType)).Append(' ').Append(pd.Name);
        if (pd.HasConstant) sb.Append(" = ").Append(ConstantText(pd.Constant));
        return sb.ToString();
    }

    private static string ConstantText(object? c) => c switch
    {
        null => "null",
        string s => "\"" + s + "\"",
        bool b => b ? "true" : "false",
        _ => c.ToString() ?? "null",
    };

    /// <summary>Modifiers for a method: access + static/virtual/abstract/override/sealed.</summary>
    public static string Modifiers(MethodDefinition m)
    {
        var sb = new StringBuilder();
        if (m.IsPublic) sb.Append("public ");
        else if (m.IsFamily) sb.Append("protected ");
        else if (m.IsFamilyOrAssembly) sb.Append("protected internal ");
        else if (m.IsAssembly) sb.Append("internal ");
        else if (m.IsPrivate) sb.Append("private ");

        if (m.IsStatic) sb.Append("static ");
        if (m.IsAbstract) sb.Append("abstract ");
        else if (m.IsVirtual && m.IsReuseSlot) sb.Append("override ");
        else if (m.IsVirtual && !m.IsFinal) sb.Append("virtual ");
        if (m.IsFinal && m.IsVirtual && m.IsReuseSlot) sb.Append("sealed ");
        return sb.ToString();
    }

    /// <summary>Type declaration line: "public abstract class Foo : Bar, IBaz".</summary>
    public static string TypeDeclaration(TypeDefinition t)
    {
        var sb = new StringBuilder();
        if (t.IsPublic || t.IsNestedPublic) sb.Append("public ");
        else if (t.IsNestedFamily) sb.Append("protected ");
        else sb.Append("internal ");

        string kind;
        if (t.IsInterface) kind = "interface";
        else if (t.IsEnum) kind = "enum";
        else if (t.IsValueType) kind = "struct";
        else
        {
            if (t.IsSealed && t.IsAbstract) sb.Append("static ");
            else if (t.IsAbstract) sb.Append("abstract ");
            else if (t.IsSealed) sb.Append("sealed ");
            kind = "class";
        }
        sb.Append(kind).Append(' ').Append(FriendlyTypeName(t));

        // base type + interfaces
        var bases = new List<string>();
        if (t.BaseType != null && t.BaseType.FullName is not "System.Object" and not "System.ValueType"
            and not "System.Enum" && !t.IsEnum)
            bases.Add(TypeName(t.BaseType));
        foreach (var i in t.Interfaces) bases.Add(TypeName(i.InterfaceType));
        if (bases.Count > 0) sb.Append(" : ").Append(string.Join(", ", bases));
        return sb.ToString();
    }

    /// <summary>Type name with generic params shown as &lt;T, U&gt;.</summary>
    public static string FriendlyTypeName(TypeDefinition t)
    {
        string name = StripArity(t.Name);
        if (t.HasGenericParameters)
            name += "<" + string.Join(", ", t.GenericParameters.Select(g => g.Name)) + ">";
        return name;
    }

    /// <summary>Declaring-type-qualified name: nested "NormalizedSimplexNoise.ColumnNoise".</summary>
    public static string QualifiedFriendlyName(TypeDefinition t)
    {
        string name = FriendlyTypeName(t);
        for (var d = t.DeclaringType; d != null; d = d.DeclaringType)
            name = StripArity(d.Name) + "." + name;
        return name;
    }

    /// <summary>Namespace of the outermost declaring type (nested types report empty Namespace).</summary>
    public static string RootNamespace(TypeDefinition t)
    {
        var r = t;
        while (r.DeclaringType != null) r = r.DeclaringType;
        return string.IsNullOrEmpty(r.Namespace) ? "(global)" : r.Namespace;
    }

    /// <summary>[Obsolete] detection — returns (true, message) if the member is deprecated.</summary>
    public static (bool yes, string? message) Obsolete(ICustomAttributeProvider m)
    {
        if (!m.HasCustomAttributes) return (false, null);
        foreach (var a in m.CustomAttributes)
            if (a.AttributeType.Name == "ObsoleteAttribute")
                return (true, a.ConstructorArguments.Count > 0 ? a.ConstructorArguments[0].Value as string : null);
        return (false, null);
    }

    /// <summary>Inline "⚠️ [Obsolete: msg] " prefix for a deprecated member, or "".</summary>
    public static string ObsoletePrefix(ICustomAttributeProvider m)
    {
        var (yes, msg) = Obsolete(m);
        if (!yes) return "";
        return string.IsNullOrEmpty(msg) ? "⚠️ `[Obsolete]` " : $"⚠️ `[Obsolete: {msg}]` ";
    }

    private static string BareName(TypeReference t) => t.IsNested ? t.Name : t.FullName;

    private static string StripArity(string name)
    {
        int tick = name.IndexOf('`');
        return tick < 0 ? name : name[..tick];
    }
}
