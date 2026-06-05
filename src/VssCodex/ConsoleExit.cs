using System.Runtime.InteropServices;

namespace VssCodex;

/// <summary>
/// When the tool was launched by double-clicking the .exe on Windows, its console window closes the
/// instant the process exits - so the user never sees the output. Detect that case and wait for Enter.
/// No-op when run from a shell, when output is piped/redirected (scripts, CI), or on non-Windows.
/// </summary>
public static class ConsoleExit
{
    public static void PauseIfLaunchedByDoubleClick()
    {
        if (!OperatingSystem.IsWindows()) return;
        try
        {
            if (Console.IsOutputRedirected || Console.IsInputRedirected) return;
            // If our process is the ONLY one attached to the console, it owns a window that was created
            // just for it (a double-click) and will vanish on exit. Launched from a shell, the shell is
            // also attached, so the count is >= 2 and we don't pause.
            uint[] buffer = new uint[4];
            if (GetConsoleProcessList(buffer, (uint)buffer.Length) <= 1)
            {
                Console.WriteLine();
                Console.Write("Press Enter to exit . . . ");
                Console.ReadLine();
            }
        }
        catch { /* never let the exit prompt itself fail the run */ }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetConsoleProcessList(uint[] processList, uint count);
}
