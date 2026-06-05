namespace VssCodex;

/// <summary>Parsed command-line options for the tool.</summary>
public sealed class Options
{
    public string? Install;
    public string? Zip;
    public string? Out;
    public bool SkipDecompile;
    public bool NoSite;

    /// <summary>
    /// Parse argv. Returns the options plus, when the program should exit immediately, the exit code
    /// (0 for <c>--help</c>, 1 for a bad argument); otherwise the code is null and the run proceeds.
    /// </summary>
    public static (Options options, int? exitCode) Parse(string[] args)
    {
        var o = new Options();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--install" or "-i" when i + 1 < args.Length: o.Install = args[++i]; break;
                case "--zip" or "-z" when i + 1 < args.Length: o.Zip = args[++i]; break;
                case "--out" or "-o" when i + 1 < args.Length: o.Out = args[++i]; break;
                case "--skip-decompile" or "-s": o.SkipDecompile = true; break;
                case "--no-site": o.NoSite = true; break;
                case "--help" or "-h": return (o, 0);
                default:
                    Console.Error.WriteLine($"unknown or incomplete argument: {args[i]}");
                    return (o, 1);
            }
        }

        if (o.Zip != null && o.Install != null)
            Console.Error.WriteLine("warning: both --zip and --install given; using --zip.");

        return (o, null);
    }
}
