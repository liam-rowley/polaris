using System.Collections.Generic;
using System.Reflection;

namespace Polaris.Driver.Tests.Architecture;

/// <summary>
/// The Polaris assemblies the architecture rules apply to. They are loaded by name rather than
/// through a marker type, so the rules still bind while a project is empty (M0). Loading throws
/// if an assembly is missing, so a rule can never pass just because its target wasn't found.
/// </summary>
internal static class PolarisAssemblies
{
    public static Assembly Native => Assembly.Load("Polaris.Native");

    public static Assembly Driver => Assembly.Load("Polaris.Driver");

    public static Assembly Registry => Assembly.Load("Polaris.Registry");

    public static Assembly Probe => Assembly.Load("Polaris.Probe");

    public static Assembly FakeDms => Assembly.Load("Polaris.FakeDms");

    public static Assembly Tests => typeof(PolarisAssemblies).Assembly;

    /// <summary>Gets every Polaris assembly except <c>Polaris.Native</c>, including this test assembly.</summary>
    public static IEnumerable<Assembly> AllExceptNative => [Driver, Registry, Probe, FakeDms, Tests];
}
