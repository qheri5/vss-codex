using VssCodex;
using Xunit;

namespace VssCodex.Tests;

public class DocIdTests
{
    private const string T = "VssCodex.Tests.Fixtures.";

    [Fact]
    public void Type_id() =>
        Assert.Equal($"T:{T}Simple", DocId.ForType(TestModule.Type("Simple")));

    [Fact]
    public void Nested_type_id_uses_dot() =>
        Assert.Equal($"T:{T}Outer.Inner", DocId.ForType(TestModule.Nested("Outer", "Inner")));

    [Fact]
    public void Method_simple_param() =>
        Assert.Equal($"M:{T}Simple.M(System.Int32)", DocId.ForMethod(TestModule.Method("Simple", "M")));

    [Fact]
    public void Method_array_param() =>
        Assert.Equal($"M:{T}Simple.Arr(System.Int32[])", DocId.ForMethod(TestModule.Method("Simple", "Arr")));

    [Fact]
    public void Method_byref_param_uses_at() =>
        Assert.Equal($"M:{T}Simple.Ref(System.Int32@)", DocId.ForMethod(TestModule.Method("Simple", "Ref")));

    [Fact]
    public void Method_generic_instance_param_uses_braces() =>
        Assert.Equal($"M:{T}Simple.Gen(System.Collections.Generic.List{{System.String}})",
            DocId.ForMethod(TestModule.Method("Simple", "Gen")));

    [Fact]
    public void Generic_method_uses_double_backtick_arity_and_param() =>
        Assert.Equal($"M:{T}Simple.Generic``1(``0)", DocId.ForMethod(TestModule.Method("Simple", "Generic")));

    [Fact]
    public void Ctor_parameterless() =>
        Assert.Equal($"M:{T}Simple.#ctor", DocId.ForMethod(TestModule.Ctor("Simple", 0)));

    [Fact]
    public void Ctor_with_param() =>
        Assert.Equal($"M:{T}Simple.#ctor(System.Int32)", DocId.ForMethod(TestModule.Ctor("Simple", 1)));

    [Fact]
    public void Field_id() =>
        Assert.Equal($"F:{T}Simple.Field", DocId.ForField(TestModule.Field("Simple", "Field")));

    [Fact]
    public void Property_id() =>
        Assert.Equal($"P:{T}Simple.Name", DocId.ForProperty(TestModule.Property("Simple", "Name")));

    [Fact]
    public void Event_id() =>
        Assert.Equal($"E:{T}Simple.Changed", DocId.ForEvent(TestModule.Event("Simple", "Changed")));
}
