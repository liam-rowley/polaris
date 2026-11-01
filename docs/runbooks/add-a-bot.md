# Runbook: add a bot

1. **List the screens the bot touches.** Each must exist in the registry; if not, do `add-a-screen.md` first. A bot PR never adds a screen.
2. **Write the bot as a sequence of page-object calls.** `session.NavigateTo("vehicle-stock.edit")`, `screen.EnterRegNumber(reg)`, `var outcome = screen.Save()`. Branch on returned outcomes. *Check:* the bot project contains no `Locator`, no `WaitUntil`, no sleeps — the banned-API analyzer and an architecture test enforce this.
3. **Handle interrupts by type.** Catch `RecordLockedException` etc. where the bot can do something useful; let everything else propagate to Hal.
4. **Run against the fake** with a scripted scenario first, then against Autoline on a test record.
5. **Register with Hal** per Hal's own runbook (launch/attach via `AutolineSession`, logger + diagnostics sinks).
6. **Observe for two weeks.** Every failure bundle gets looked at; registry fixes go through `add-a-screen.md` step 2 onward.
