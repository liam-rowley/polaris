# Polaris.FakeDms — behaviour switches

Target for driver development and CI. Registers real custom window classes (ADR-0005). Every switch is a named test scenario in `Polaris.Driver.Tests`.

Switches are set at launch (`--switch name=value`) and can be changed at runtime through the named pipe `\\.\pipe\polaris-fakedms-<pid>` (JSON line `{"set":{"name":"value"}}`), so a test can flip behaviour between steps.

| Switch | Values | Default | Simulates |
|---|---|---|---|
| `validationTrigger` | `keystroke` \| `focusLeave` \| `save` | `focusLeave` | when Autoline validates a field |
| `buttonsHonourBmClick` | `true` \| `false` | `true` | a control that needs posted mouse messages |
| `editsHonourSetText` | `true` \| `false` | `true` | a control that needs typed characters |
| `windowOpenDelayMs` | int | `0` | slow screens |
| `interruptPopupAfterMs` | int \| `off` | `off` | an unexpected popup mid-flow |
| `gridStrategy` | `childHwnds` \| `msaa` \| `clipboardOnly` \| `none` | TBD after M1 | which grid access paths exist |
| `hung` | `true` \| `false` | `false` | app stops pumping messages |
| `controlIds` | `zero` \| `stable` \| `sequential` | TBD after M1 | what `GetDlgCtrlID` returns |

Screens: `Login`, `MainMenu`, `VehicleStockList`, `VehicleStockEdit`, `ConfirmDialog`, `SlowScreen`, `InterruptPopup`. Class names and title formats are calibrated to probe captures under `registry/autoline/`; the calibration test asserts every curated `screen.json` resolves against the fake.
