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
