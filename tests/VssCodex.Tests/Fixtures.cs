using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace VssCodex.Tests.Fixtures;

// Types exercised by the tests. They are read back via Mono.Cecil from this assembly, so the tests
// run against real .NET metadata without needing any Vintage Story binary.

public class Simple
{
    public Simple() { }
    public Simple(int x) { _ = x; }

    public int Field;
    public string Name { get; set; } = "";
    public int? NullableProp { get; set; }
#pragma warning disable CS0067 // read via Cecil metadata, never invoked
    public event Action? Changed;
#pragma warning restore CS0067

    public void M(int x) { _ = x; }
    public void Arr(int[] a) { _ = a; }
    public void Ref(out int x) { x = 0; }
    public void Gen(List<string> items) { _ = items; }
    public void Generic<T>(T item) { _ = item; }
    public virtual void V() { }

    [Obsolete("use V instead")]
    public void Old() { }

    protected void Prot() { }
    private void Priv() { }
}

public class Outer
{
    public class Inner { }
}

// Delegate fixtures for the events-doc handler rendering (resolvable within this module).
public delegate bool SampleDelegate(int code, string name);
public delegate void VoidDelegate(string msg);
public delegate T GenDelegate<T>(T input, int n);

public class GenEventHost
{
#pragma warning disable CS0067 // read via Cecil metadata, never invoked
    public event GenDelegate<string>? OnGen; // closed generic instance: handler should read (string input, int n)
#pragma warning restore CS0067
}

public abstract class AbstractFixture
{
    public abstract void DoAbstract();
    public void DoConcrete() { }
}

public static class NativeFixture
{
    [DllImport("kernel32")]
    public static extern int GetCurrentProcessId();
}

// Base/derived pair for the inherited-doc test.
public interface IDocumented
{
    void Described();
}

public class DerivedDoc : IDocumented
{
    public virtual void Described() { }
}

// -- type-declaration / modifier matrix --------------------------------------

public enum EnumFixture { A, B = 5 }

public struct StructFixture { public int X; }

public sealed class SealedFixture { }

public class Combined : AbstractFixture, IDocumented
{
    public override void DoAbstract() { }
    public void Described() { }
}

public class BaseV { public virtual void Vm() { } }

public sealed class SealedOverrideFixture : BaseV { public sealed override void Vm() { } }

public class Members
{
    public const int K = 3;
    public static readonly int RO = 4;
#pragma warning disable CS0067 // read via Cecil metadata, never invoked
    public static event Action? Ping;
#pragma warning restore CS0067
}

// Overload pair for the inherited-doc disambiguation test: the base has only the 1-arg Op, the
// derived adds a 2-arg Op that has NO matching base overload.
public class OverloadBase { public virtual void Op(int a) { _ = a; } }

public class OverloadDerived : OverloadBase
{
    public override void Op(int a) { _ = a; }
    public void Op(int a, int b) { _ = a; _ = b; }
}
