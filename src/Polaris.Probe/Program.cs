using System;

namespace Polaris.Probe;

/// <summary>Entry point for the <c>probe</c> console tool.</summary>
internal static class Program
{
    private static int Main(string[] args)
    {
        // list / snapshot / try-* / watch arrive in M1 (docs/MILESTONES.md, SPEC §5.5).
        Console.Error.WriteLine("probe: no commands yet; list, snapshot, try-read, try-write, try-click and watch arrive in M1.");
        return args is ["--help"] ? 0 : 1;
    }
}
