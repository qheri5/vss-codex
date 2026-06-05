using VssCodex;
using Xunit;

namespace VssCodex.Tests;

public class SignatureFormatterTests
{
    [Fact]
    public void Primitive_and_string_names()
    {
        var m = TestModule.Method("Simple", "M");
        Assert.Equal("public void M(int x)", SignatureFormatter.MethodSignature(m));
    }

    [Fact]
    public void Out_parameter_modifier()
    {
        var m = TestModule.Method("Simple", "Ref");
        Assert.Equal("public void Ref(out int x)", SignatureFormatter.MethodSignature(m));
    }

    [Fact]
    public void Generic_instance_param_uses_angle_brackets()
    {
        // Generic args render with full type names (same convention as the generated API docs).
        var m = TestModule.Method("Simple", "Gen");
        Assert.Equal("public void Gen(System.Collections.Generic.List<string> items)",
            SignatureFormatter.MethodSignature(m));
    }

    [Fact]
    public void Generic_method_shows_type_params()
    {
        var m = TestModule.Method("Simple", "Generic");
        Assert.Equal("public void Generic<T>(T item)", SignatureFormatter.MethodSignature(m));
    }

    [Fact]
    public void Virtual_modifier()
    {
        var m = TestModule.Method("Simple", "V");
        Assert.Equal("public virtual void V()", SignatureFormatter.MethodSignature(m));
    }

    [Fact]
    public void Nullable_property_renders_question_mark()
    {
        var p = TestModule.Property("Simple", "NullableProp");
        Assert.Contains("int? NullableProp", SignatureFormatter.PropertySignature(p));
    }

    [Fact]
    public void Nested_type_qualified_name()
    {
        var inner = TestModule.Nested("Outer", "Inner");
        Assert.Equal("Outer.Inner", SignatureFormatter.QualifiedFriendlyName(inner));
    }

    [Fact]
    public void Root_namespace_of_nested_type()
    {
        var inner = TestModule.Nested("Outer", "Inner");
        Assert.Equal("VssCodex.Tests.Fixtures", SignatureFormatter.RootNamespace(inner));
    }

    [Fact]
    public void Obsolete_detected_with_message()
    {
        var m = TestModule.Method("Simple", "Old");
        var (yes, msg) = SignatureFormatter.Obsolete(m);
        Assert.True(yes);
        Assert.Equal("use V instead", msg);
        Assert.Contains("[Obsolete: use V instead]", SignatureFormatter.ObsoletePrefix(m));
    }

    [Fact]
    public void Non_obsolete_has_empty_prefix()
    {
        var m = TestModule.Method("Simple", "M");
        Assert.Equal("", SignatureFormatter.ObsoletePrefix(m));
    }

    [Fact]
    public void Protected_modifier()
    {
        var m = TestModule.Method("Simple", "Prot");
        Assert.StartsWith("protected ", SignatureFormatter.MethodSignature(m));
    }
}
