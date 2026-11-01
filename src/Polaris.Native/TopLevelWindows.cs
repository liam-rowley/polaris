using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Windows.Win32;

namespace Polaris.Native;

/// <summary>Enumerates top-level windows on the current desktop.</summary>
public static class TopLevelWindows
{
    /// <summary>Returns a snapshot of every top-level window, in Z order.</summary>
    /// <returns>The handles found. The list is a copy and never changes after it is returned.</returns>
    /// <exception cref="Win32Exception"><c>EnumWindows</c> failed.</exception>
    public static IReadOnlyList<WindowHandle> Enumerate()
    {
        var handles = new List<WindowHandle>();
        bool ok = PInvoke.EnumWindows(
            (hwnd, _) =>
            {
                handles.Add(new WindowHandle((nint)hwnd));
                return true;
            },
            default);

        if (!ok)
        {
            throw new Win32Exception(Marshal.GetLastPInvokeError());
        }

        return handles;
    }
}
