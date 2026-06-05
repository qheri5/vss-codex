using VssCodex;
using Xunit;

namespace VssCodex.Tests;

public class HarmonyPatchabilityTests
{
    [Fact]
    public void Concrete_method_is_patchable()
    {
        var (ok, reason) = HarmonyTargetGenerator.Patchability(TestModule.Method("Simple", "M"));
        Assert.True(ok);
        Assert.Equal("", reason);
    }

    [Fact]
    public void Abstract_method_is_not_patchable()
    {
        var (ok, reason) = HarmonyTargetGenerator.Patchability(TestModule.Method("AbstractFixture", "DoAbstract"));
        Assert.False(ok);
        Assert.Contains("abstract", reason);
    }

    [Fact]
    public void Concrete_method_on_abstract_type_is_patchable()
    {
        var (ok, _) = HarmonyTargetGenerator.Patchability(TestModule.Method("AbstractFixture", "DoConcrete"));
        Assert.True(ok);
    }

    [Fact]
    public void Pinvoke_extern_method_is_not_patchable()
    {
        var (ok, reason) = HarmonyTargetGenerator.Patchability(TestModule.Method("NativeFixture", "GetCurrentProcessId"));
        Assert.False(ok);
        Assert.Contains("P/Invoke", reason);
    }
}
