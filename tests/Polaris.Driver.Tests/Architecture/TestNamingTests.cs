using System.Linq;
using System.Reflection;
using Xunit;

namespace Polaris.Driver.Tests.Architecture;

/// <summary>Test names describe a scenario: <c>Subject_Action_Outcome</c> (CONVENTIONS.md).</summary>
public sealed class TestNamingTests
{
    [Fact]
    public void Every_test_method_name_contains_an_underscore()
    {
        string[] offenders = PolarisAssemblies.Tests.GetTypes()
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(m => m.IsDefined(typeof(FactAttribute), inherit: true))
            .Where(m => !m.Name.Contains('_', System.StringComparison.Ordinal))
            .Select(m => $"{m.DeclaringType?.Name}.{m.Name}")
            .ToArray();

        Assert.True(offenders.Length == 0, $"Test names must describe a scenario (Subject_Action_Outcome): {string.Join(", ", offenders)}");
    }
}
