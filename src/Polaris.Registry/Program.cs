using System;

namespace Polaris.Registry;

/// <summary>Entry point for the <c>polaris-registry</c> CLI.</summary>
internal static class Program
{
    private static int Main()
    {
        // validate / generate / diff arrive in M4 (docs/MILESTONES.md).
        Console.Error.WriteLine("polaris-registry: no commands yet; validate, generate and diff arrive in M4.");
        return 1;
    }
}
