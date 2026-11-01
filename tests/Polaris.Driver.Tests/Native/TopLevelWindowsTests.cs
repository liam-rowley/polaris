using Polaris.Native;
using Xunit;

namespace Polaris.Driver.Tests.Native;

public sealed class TopLevelWindowsTests
{
    [Fact]
    public void Enumerate_returns_at_least_one_window_from_CsWin32_EnumWindows()
    {
        var windows = TopLevelWindows.Enumerate();

        Assert.NotEmpty(windows);
        Assert.DoesNotContain(default, windows);
    }
}
