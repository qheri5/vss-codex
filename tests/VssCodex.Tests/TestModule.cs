using System.Linq;
using Mono.Cecil;
using VssCodex.Tests.Fixtures;

namespace VssCodex.Tests;

/// <summary>Reads this test assembly back with Mono.Cecil so tests can run the real Cecil-based logic.</summary>
internal static class TestModule
{
    public static readonly ModuleDefinition Self =
        ModuleDefinition.ReadModule(typeof(Simple).Assembly.Location);

    private const string Ns = "VssCodex.Tests.Fixtures.";

    public static TypeDefinition Type(string name) => Self.GetType(Ns + name)!;

    public static TypeDefinition Nested(string outer, string inner) =>
        Type(outer).NestedTypes.First(t => t.Name == inner);

    public static MethodDefinition Method(string type, string method) =>
        Type(type).Methods.First(m => m.Name == method && !m.IsConstructor);

    public static MethodDefinition Ctor(string type, int paramCount) =>
        Type(type).Methods.First(m => m.IsConstructor && m.Parameters.Count == paramCount);

    public static PropertyDefinition Property(string type, string name) =>
        Type(type).Properties.First(p => p.Name == name);

    public static FieldDefinition Field(string type, string name) =>
        Type(type).Fields.First(f => f.Name == name);

    public static EventDefinition Event(string type, string name) =>
        Type(type).Events.First(e => e.Name == name);
}
