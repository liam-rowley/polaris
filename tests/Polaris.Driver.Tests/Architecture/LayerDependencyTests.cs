using System.Collections.Generic;
using System.Reflection;
using NetArchTest.Rules;
using Xunit;

namespace Polaris.Driver.Tests.Architecture;

/// <summary>Layer direction: Autoline → Driver → Native (CLAUDE.md, SPEC §5).</summary>
public sealed class LayerDependencyTests
{
    [Fact]
    public void Driver_does_not_depend_on_Autoline()
    {
        AssertNoDependency(PolarisAssemblies.Driver, "Polaris.Autoline");
    }

    [Fact]
    public void Native_does_not_depend_on_any_higher_layer_or_tool()
    {
        AssertNoDependency(
            PolarisAssemblies.Native,
            "Polaris.Driver",
            "Polaris.Autoline",
            "Polaris.Registry",
            "Polaris.Probe",
            "Polaris.FakeDms");
    }

    [Fact]
    public void Nothing_outside_Native_references_Windows_Win32()
    {
        foreach (Assembly assembly in PolarisAssemblies.AllExceptNative)
        {
            AssertNoDependency(assembly, "Windows.Win32");
        }
    }

    private static void AssertNoDependency(Assembly assembly, params string[] forbiddenNamespaces)
    {
        NetArchTest.Rules.TestResult result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenNamespaces)
            .GetResult();

        IEnumerable<string> offenders = result.FailingTypeNames ?? [];
        Assert.True(
            result.IsSuccessful,
            $"{assembly.GetName().Name} must not depend on {string.Join(", ", forbiddenNamespaces)}. Offending types: {string.Join(", ", offenders)}");
    }
}
