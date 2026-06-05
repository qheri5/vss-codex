using VssCodex;
using Xunit;

namespace VssCodex.Tests;

// MemberVisibility is the single source of truth shared by the docs and the changelog snapshot, so
// guard its predicates directly (not just indirectly through the generators).
public class MemberVisibilityTests
{
    [Fact]
    public void IsVisibleMethod_includes_public_and_protected_excludes_private()
    {
        Assert.True(MemberVisibility.IsVisibleMethod(TestModule.Method("Simple", "M")));
        Assert.True(MemberVisibility.IsVisibleMethod(TestModule.Method("Simple", "Prot")));
        Assert.False(MemberVisibility.IsVisibleMethod(TestModule.Method("Simple", "Priv")));
    }

    [Fact]
    public void IsVisibleMethod_excludes_property_accessors()
    {
        var getter = TestModule.Property("Simple", "Name").GetMethod!;
        Assert.False(MemberVisibility.IsVisibleMethod(getter));
    }

    [Fact]
    public void IsAccessibleMethod_is_raw_public_or_protected() // the snapshot tracks accessors too
    {
        Assert.True(MemberVisibility.IsAccessibleMethod(TestModule.Property("Simple", "Name").GetMethod!));
        Assert.True(MemberVisibility.IsAccessibleMethod(TestModule.Method("Simple", "Prot")));
        Assert.False(MemberVisibility.IsAccessibleMethod(TestModule.Method("Simple", "Priv")));
    }

    [Fact]
    public void Field_property_event_visibility()
    {
        Assert.True(MemberVisibility.IsVisibleField(TestModule.Field("Simple", "Field")));
        Assert.True(MemberVisibility.IsVisibleProperty(TestModule.Property("Simple", "Name")));
        Assert.True(MemberVisibility.IsVisibleEvent(TestModule.Event("Simple", "Changed")));
    }

    [Fact]
    public void IsVisibleConstructor_matches_public_instance_ctor() =>
        Assert.True(MemberVisibility.IsVisibleConstructor(TestModule.Ctor("Simple", 0)));
}
