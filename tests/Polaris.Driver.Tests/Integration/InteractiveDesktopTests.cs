using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows.Forms;
using Xunit;

namespace Polaris.Driver.Tests.Integration;

/// <summary>
/// Throwaway M0 probe (SPEC §16 Q8): does this machine (CI runner or dev box) give tests an interactive
/// desktop where hardware input reaches a window? Replace it with real FakeDms integration tests in M2.
/// </summary>
public sealed class InteractiveDesktopTests
{
    private const string NotForegroundMessage =
        "The probe window could not become the foreground window (foreground lock), so no input was sent. "
        + "Expected on a dev box when another app has focus; on CI it means input-based tests cannot run there.";

    private const string NoKeyDownMessage =
        "The window was foreground but KeyDown never fired: SendInput did not reach it (no interactive desktop?).";

    private static readonly TimeSpan KeyDownDeadline = TimeSpan.FromSeconds(10);

    private enum ProbeResult
    {
        KeyDownFired,
        KeyDownNeverFired,
        NotForeground,
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void SendInput_keystroke_reaches_a_foreground_WinForms_window()
    {
        ProbeResult result = ProbeResult.KeyDownNeverFired;
        ExceptionDispatchInfo? failure = null;

        var uiThread = new Thread(() =>
        {
            try
            {
                result = ShowFormAndSendKeystroke();
            }
#pragma warning disable CA1031 // Marshal any UI-thread failure back to the test thread, which rethrows it.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                failure = ExceptionDispatchInfo.Capture(ex);
            }
        });
        uiThread.SetApartmentState(ApartmentState.STA);
        uiThread.Start();

        Assert.True(uiThread.Join(KeyDownDeadline * 3), "UI thread did not finish; the desktop may be locked or non-interactive.");
        failure?.Throw();
        Assert.False(result == ProbeResult.NotForeground, NotForegroundMessage);
        Assert.True(result == ProbeResult.KeyDownFired, NoKeyDownMessage);
    }

    private static ProbeResult ShowFormAndSendKeystroke()
    {
        var result = ProbeResult.KeyDownNeverFired;
        using var form = new Form
        {
            Text = "Polaris M0 desktop probe",
            StartPosition = FormStartPosition.CenterScreen,
            TopMost = true,
            KeyPreview = true,
        };
        using var deadline = new System.Windows.Forms.Timer { Interval = (int)KeyDownDeadline.TotalMilliseconds };

        deadline.Tick += (_, _) => form.Close();
        form.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.A)
            {
                result = ProbeResult.KeyDownFired;
                form.Close();
            }
        };
        form.Shown += (_, _) =>
        {
            form.Activate();

            // Form.ActiveForm is backed by GetForegroundWindow. Never send hardware input unless our window
            // is in front; otherwise the keystroke lands in whatever the developer is typing into.
            if (Form.ActiveForm != form)
            {
                result = ProbeResult.NotForeground;
                form.Close();
                return;
            }

            deadline.Start();

            // WinForms SendKeys is implemented with SendInput on modern .NET, so this is hardware-level
            // input, without adding a P/Invoke to the test project.
            SendKeys.SendWait("a");
        };

        Application.Run(form);
        return result;
    }
}
