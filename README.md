# MATRIC Integration API Demo

This repository demonstrates MATRIC's existing UDP integration API in two Windows console applications:

- [Raw demo](demos/raw): constructs and sends commands without the MATRIC SDK.
- [C# SDK demo](demos/net_wrapper): uses the bundled `MatricIntegration.dll`.

Integrations can update the appearance and state of controls, switch decks and pages, publish server variables, and receive control interactions and variable changes. For example, a flight simulator can display fuel levels on a tablet and react to its controls.

## Status and compatibility

This reference describes the current MATRIC implementation. Existing UDP command names and message envelopes are preserved. A deployed MATRIC release may predate recent client-side fixes, including clearing text/images and applying alignment/padding; check the release you target.

Both demos use the existing UDP protocol and run their animations on the foreground thread while receive callbacks process events. PIN authorization remains unchanged; see the setup instructions below.

## Build and run

Requirements:

- Windows with MATRIC Desktop and a connected MATRIC client.
- Visual Studio or Visual Studio Build Tools with .NET desktop development tools and the .NET Framework 4.8 targeting pack.
- NuGet package restore for the dependencies declared in each project's `packages.config`.

Open `IntegrationDemo.sln` in Visual Studio, restore NuGet packages, and build either project. Alternatively, from a Developer Command Prompt with `nuget` and `msbuild` available:

```bat
nuget restore IntegrationDemo.sln
msbuild IntegrationDemo.sln /p:Configuration=Release /p:Platform="Any CPU"
```

The executables are `demos/raw/bin/Release/IntegrationDemo.exe` and `demos/net_wrapper/bin/Release/IntegrationDemo.exe`.

Before running:

1. In MATRIC Settings > API Integration, enable third-party integration. Choose whether to require an integration PIN.
2. Check the Integration API port under the advanced network settings. The current server and bundled SDK default is **5300**. Saved custom settings remain valid. Save and restart MATRIC after changing network ports.
3. Both demos default to **5300**. If your server uses a different port, pass `--port=<port>` when running the executable. Shared defaults are in [DemoCommon.cs](DemoCommon.cs).
4. Import [IntegrationDemo.deck](IntegrationDemo.deck) by double-clicking it, then connect your phone/tablet to MATRIC.
5. Run one demo at a time. Both use application name `MATRIC Integration Demo` and default receive port **50301**. If DBMM or another application owns that receive port, use `--listen-port=<port>`. Keep the chosen receive port stable between runs: the current MATRIC server does not refresh same-named registrations when the port changes. Restarting MATRIC clears those registrations.

When PIN authorization is disabled, no authorization popup or PIN entry is required. When it is enabled, an unrecognized app name/PIN triggers MATRIC's approval dialog. After approval, use the displayed PIN with that same app name in subsequent commands.

**PIN authorization behavior is unchanged:** neither demo reads a PIN interactively; both use `CONST.PIN`, currently `0450`. When MATRIC requires a PIN and the configured value is not authorized, the first request triggers MATRIC's approval dialog. Record the displayed PIN, stop the demo, update `CONST.PIN`, rebuild, and restart. The demos do not implement an interactive authorization workflow.

Both demos start immediately, request connected devices with a bounded timeout, and print their current IDs and names. By default the first device is selected; use `--client-id=<id>` to choose another. Every device-targeted demo command uses that selected ID. Variable writes are server-wide by design.

```bat
demos\raw\bin\Release\IntegrationDemo.exe --port=5300 --listen-port=50301 --client-id=tablet-id --timeout-ms=10000
demos\net_wrapper\bin\Release\IntegrationDemo.exe --port=5300 --listen-port=50301 --client-id=tablet-id --timeout-ms=10000
```

The animation finishes before the program exits. Press **Ctrl+C** to cancel discovery, readiness polling, or animation delays; sockets are then disposed. Exit codes are 0 for completion, 1 for an error/timeout, and 130 for cancellation. Do not run both example commands simultaneously.

### Deck readiness and compatibility

After sending `SETDECK` once, the demos poll `GETCONNECTEDCLIENTS` until the selected device reports the expected `currentDeckId`, using the same timeout as discovery. Client-list callbacks only publish snapshots; they never start another animation. Only the read-only client query is retried automatically.

For older servers or devices without usable deck-membership reports, explicitly choose a timing fallback:

```bat
demos\raw\bin\Release\IntegrationDemo.exe --legacy-deck-delay-ms=1500
```

This skips membership polling and waits the configured number of milliseconds. It is a timing assumption, not confirmation. Without this option, unavailable or unconfirmed deck membership times out rather than silently proceeding. Neither membership nor the fallback acknowledges complete image/applet rendering or a particular reload of an already active deck. Later page-change and animation delays are pacing only; the existing protocol has no page-ready acknowledgement.

| Option | Default | Meaning |
|---|---|---|
| `--port=<n>` | 5300 | MATRIC Integration API port, 1-65535. |
| `--listen-port=<n>` | 50301 | Stable local UDP receive port, 1-65535. |
| `--client-id=<id>` | First connected device | Explicit target device. An empty ID is rejected. |
| `--timeout-ms=<n>` | 10000 | Positive discovery/deck-membership timeout. |
| `--legacy-deck-delay-ms=<n>` | Not enabled | Explicit nonnegative timing fallback for older servers/devices. |

### Regression checks

Restore dependencies and build the demos before running the checks. From PowerShell:

```powershell
./tests/run-checks.ps1
```

These private loopback checks cover escaped JSON, independent normal/pressed images, genuine null values, object-valued states, UTF-8 replies, recovery after malformed packets, socket disposal, target selection, deck polling, timeouts, and cancellation.

With Python 3 available, run the executable-level checks:

```bat
python tests/mock-server-check.py
```

These launch both built demos with separate fake UDP responders and verify selected-device targeting, delayed deck-membership confirmation, event processing during animation, distinct typed variables, and clean completion. They do not connect to a running MATRIC instance or change saved user settings.

## Transport and authorization

Send one UTF-8 JSON command per UDP datagram to `127.0.0.1` on MATRIC's configured Integration API port. This API is local to the MATRIC Windows PC.

Use the **same bound UDP socket** to send commands and receive replies/events. MATRIC replies to the sender's address and port; there is no receive-port field in a command. `50301` is the examples' receive port, not a required protocol port. MATRIC's reply/event source port is not necessarily its command-listener port.

An accepted command registers the application's endpoint for subsequent control-interaction and variable-change broadcasts. `CONNECT` is useful for registration even when PIN authorization is disabled. There is no explicit disconnect command or subscription filter in this protocol. The current server identifies registrations by app name and IP, not port: restarting an app on another receive port does not currently refresh the old registration. Keep the endpoint stable, and do not run same-named instances concurrently.

UDP has no delivery acknowledgement or application-level sequencing here. A successful send does not prove that MATRIC received a command or that a device applied it. Commands other than `GETCONNECTEDCLIENTS` have no ordinary success response; failures do not have a structured error response. Do not treat a sleep as a deck-loaded acknowledgement, or blindly retry operations that could have side effects.

### Common command fields

| Field | Type | Meaning |
|---|---|---|
| `command` | string | One of the commands documented below. |
| `appName` | string | Application identity and display name; keep it consistent between authorization and subsequent requests. |
| `appPIN` | string or null | PIN issued for this app; required only when MATRIC requires integration PINs. |
| `clientId` | string or null | Device ID for client-targeted commands. Null or omission broadcasts those commands to all connected clients. Server commands ignore it. An empty string is not a broadcast target. |

Examples use illustrative PIN `0186` and device ID `tablet-id`; replace them with your actual values. JSON field names are matched case-insensitively by the current server, but use the documented spelling consistently. Use valid JSON, including quoted property names, and serialize values rather than concatenating strings.

## Commands

### CONNECT

With PINs enabled, an unrecognized app name/PIN prompts for user approval and the request is not executed further. Enter the approved PIN into your integration and send a new request. An already authorized request does not prompt again. With PINs disabled, this registers the endpoint without a prompt. There is no CONNECT response containing a PIN or success acknowledgement.

```json
{
  "command": "CONNECT",
  "appName": "MATRIC Integration Demo"
}
```

### GETCONNECTEDCLIENTS

Requests the currently connected devices:

```json
{
  "command": "GETCONNECTEDCLIENTS",
  "appName": "MATRIC Integration Demo",
  "appPIN": "0186"
}
```

The response is an envelope whose `MessageData` is a **JSON string**, not an embedded array:

```json
{
  "MessageType": "ClientList",
  "MessageData": "[{\"clientName\":\"My tablet\",\"IP\":\"192.168.1.20\",\"clientId\":\"tablet-id\",\"matricVersion\":2918,\"currentDeckId\":\"7f18056c-7a2a-46ac-9956-662e7d0b78ec\",\"LastContact\":\"2026-11-01T10:00:00+01:00\"}]"
}
```

Parse the outer JSON, then parse `MessageData` to obtain the array. The array is empty when no devices are connected. Read `clientId` and `clientName`, not `Id`, `Name` or `Hash`. `matricVersion` is device compatibility metadata; the example value is illustrative.

`currentDeckId` is the deck last reported by that device connection. It is null before a valid report, when no deck is loaded, or when an older device does not report deck membership. It resets on reconnect. Older servers omit the property; older SDKs ignore it. After SETDECK, poll GETCONNECTEDCLIENTS with a timeout for the target client to report the desired ID. This confirms reported membership, not image/applet rendering completion or completion of a particular reload of the same deck. The server does not set this field merely because it received SETDECK.

### SETDECK

Loads a deck on the target device. `pageId` optionally selects a page in that deck; omit it or use JSON null when no page is specified. Deck and page IDs must exist in the installed deck library.

```json
{
  "command": "SETDECK",
  "appName": "MATRIC Integration Demo",
  "appPIN": "0186",
  "clientId": "tablet-id",
  "deckId": "7f18056c-7a2a-46ac-9956-662e7d0b78ec",
  "pageId": "00d9c649-f2df-405f-abfd-06d38f8626be"
}
```

### SETACTIVEPAGE

Selects a page in the device's currently loaded deck:

```json
{
  "command": "SETACTIVEPAGE",
  "appName": "MATRIC Integration Demo",
  "appPIN": "0186",
  "clientId": "tablet-id",
  "pageId": "16856457-d2f8-405e-927e-749b1dc7d7ec"
}
```

### SETBUTTONPROPS

Updates one control's visual properties. Supply `buttonId` or `buttonName`; a nonempty ID takes precedence. Name lookup selects the first matching control in the loaded deck. These updates do not change saved deck definitions or action assignments.

```json
{
  "command": "SETBUTTONPROPS",
  "appName": "MATRIC Integration Demo",
  "appPIN": "0186",
  "clientId": "tablet-id",
  "buttonName": "BTN_GREEN",
  "data": {
    "text": "Ready",
    "backgroundcolorOff": "green",
    "textcolorOff": "white",
    "fontSize": "large",
    "halign": "center",
    "valign": "mid",
    "padding": "5"
  }
}
```

| Property | Values |
|---|---|
| `text` | Label text; an empty string clears it. |
| `imageOff`, `imageOn` | Normal/pressed image resource names or supported image data. An empty string removes the image. Resource filenames must exist in the loaded deck. |
| `textcolorOff`, `textcolorOn` | Normal/pressed text colors. |
| `backgroundcolorOff`, `backgroundcolorOn` | Normal/pressed background colors. |
| `fontSize` | `small`, `medium`, `large`, `xlarge`, `xxlarge`, `xxxlarge` map to 10, 15, 30, 40, 60, 80 percent of grid cell size; numeric strings such as `"25"` also work. |
| `halign` | `left`, `center`, `right`. |
| `valign` | `top`, `mid`, `bottom`. |
| `padding` | Numeric string representing percent of grid cell size. Current client code clamps finite values to 0-50; invalid values are ignored. |

Omitted or null properties leave existing values unchanged. Empty strings explicitly clear text and images; do not assume that empty strings reset every other property.

### SETBUTTONPROPSEX

Batch visual updates with the same properties as `SETBUTTONPROPS`. Properties and selectors are directly inside each `data` entry. A nonempty `buttonId` takes precedence; `buttonName` updates **all** matching controls across pages of the loaded deck.

```json
{
  "command": "SETBUTTONPROPSEX",
  "appName": "MATRIC Integration Demo",
  "appPIN": "0186",
  "clientId": "tablet-id",
  "data": [
    { "buttonName": "BTN_RED", "text": "Stop", "backgroundcolorOff": "red" },
    { "buttonName": "BTN_GREEN", "text": "Go", "backgroundcolorOff": "green" }
  ]
}
```

### SETBUTTONSVISUALSTATE

Legacy button-only visual state command. `on` selects the pressed appearance and `off` selects the normal appearance. It does not execute the button's actions. For new integrations, `SETCONTROLSSTATE` also supports other control types.

```json
{
  "command": "SETBUTTONSVISUALSTATE",
  "appName": "MATRIC Integration Demo",
  "appPIN": "0186",
  "clientId": "tablet-id",
  "data": [
    { "buttonName": "BTN_RED", "state": "on" },
    { "buttonName": "BTN_GREEN", "state": "off" }
  ]
}
```

### SETCONTROLSSTATE

Updates control state without executing its configured input actions. Supply `controlId` or `controlName` for each item. An ID takes precedence; names can match multiple controls across pages. State is assigned as an object, so provide the fields needed for that control.

```json
{
  "command": "SETCONTROLSSTATE",
  "appName": "MATRIC Integration Demo",
  "appPIN": "0186",
  "clientId": "tablet-id",
  "data": [
    { "controlName": "BTN_GREEN", "state": { "pressed": true } },
    { "controlName": "SHIELDS_SLIDER", "state": { "value": 50 } },
    { "controlName": "MULTI_POS_5", "state": { "position": 4 } }
  ]
}
```

| Control | State example | Meaning |
|---|---|---|
| Button | `{"pressed":true}` | Pressed/normal appearance. |
| Slider | `{"value":50}` | Slider position, normally 0-100. |
| Rotary | `{"pressed":true,"value":50}` | Pressed state and value, normally 0-100. |
| Multi-position switch | `{"position":4}` | **Zero-based** position: 0-4 for five positions. The current renderer caps values above the final position. |
| Rotary encoder | `{"pressed":true}` | Pressed/normal state. |

Some controls defer external state changes while the user is interacting with them. Do not assume immediate visual application during an active touch/drag.

### SETVARIABLES

Adds or replaces server variables. This is a server operation; `clientId` does not scope the variables to one device. Variable names must be unique within the server's shared variable store.

```json
{
  "command": "SETVARIABLES",
  "appName": "MATRIC Integration Demo",
  "appPIN": "0186",
  "Variables": [
    { "Name": "demo_string", "Value": "Hello", "VariableType": "STRING" },
    { "Name": "demo_bool", "Value": true, "VariableType": "BOOL" },
    { "Name": "demo_number", "Value": 42, "VariableType": "NUMBER" },
    { "Name": "demo_json", "Value": { "fuel": 42 }, "VariableType": "JSONOBJECT" }
  ]
}
```

The current server accepts `Variables` or `variables`; use `Variables` for compatibility with older implementations that matched that key exactly. Supported type labels are `STRING`, `NUMBER`, `BOOL`, and `JSONOBJECT`.

Definitions also support `CalculateValueFunction`, `IsPersistent`, and `IsUserEditable`. They default to null, false, and false. A write replaces the stored definition rather than merging value-only fields, so include any metadata you need to retain. `CalculateValueFunction` is a JavaScript function body evaluated with `value` and `variables` arguments. `IsPersistent` requests persistence through MATRIC's variable/settings handling; `IsUserEditable` marks the variable as editable in the UI.

Variable changes are buffered/coalesced by name on a nominal 60 ms timer. They represent state, not a lossless queue of every write. Multiple writes can collapse into one notification. There is no variable snapshot request in the existing integration command set.

## Replies and events

All replies/events use the same envelope, encoded as UTF-8 JSON with a trailing newline:

```json
{
  "MessageType": "VariablesChange",
  "MessageData": "{\"ChangedVariables\":[\"demo_number\"],\"Variables\":{\"demo_number\":{\"Name\":\"demo_number\",\"Value\":42,\"VariableType\":\"NUMBER\",\"CalculateValueFunction\":null,\"IsPersistent\":false,\"IsUserEditable\":false}}}"
}
```

Decode the outer JSON and then decode the JSON string in `MessageData`.

| `MessageType` | Decoded `MessageData` |
|---|---|
| `ClientList` | Array of connected-device descriptions; reply to `GETCONNECTEDCLIENTS`. |
| `ControlInteraction` | Client control message containing `MessageType`, `ClientInfo`, and `MessageData` with the control event. |
| `VariablesChange` | Object with `ChangedVariables` (names) and `Variables` (a dictionary of changed definitions/values). |

`VariablesChange.Variables` contains the changes in that notification, not the entire server store. An integration joining later does not automatically receive all existing values. Notifications include writes from integrations themselves; avoid echo loops when reacting to changes.

For `ControlInteraction`, the decoded payload has this shape:

```json
{
  "MessageType": "controlevent",
  "ClientInfo": { "clientId": "tablet-id", "clientName": "My tablet", "IP": "192.168.1.20", "matricVersion": 2918 },
  "MessageData": {
    "DeckId": "7f18056c-7a2a-46ac-9956-662e7d0b78ec",
    "PageId": "00d9c649-f2df-405f-abfd-06d38f8626be",
    "ControlId": "70158257-0124-4e3d-bc26-728a2d417109",
    "ControlName": "BTN_RED",
    "EventName": "press",
    "Data": { "pressed": true }
  }
}
```

Event names and `Data` depend on control type. Use the supplied device, deck, page and control identity when routing events. Existing event broadcasts are not filtered by your last command's target device. Treat these as notifications of user interaction, not command acknowledgements.

## C# SDK

The bundled SDK is in [demos/net_wrapper/lib](demos/net_wrapper/lib). It was refreshed from MATRIC's `Integration/MatricIntegrationLib` source with `ClientInfo.CurrentDeckId` support, built as `Release|AnyCPU` for .NET Framework 4.8. Its assembly version remains `2.8.1.0`; that version alone does not identify the refreshed binary.

The DLL's SHA-256 is `C92A367B54DE08865260AA9ECB8C1988837E9B3BBB75332847AA75028530400D`. The adjacent PDB matches this build. Both demo projects and the regression checks use Newtonsoft.Json 13.0.4 through NuGet, compatible with their .NET Framework 4.8 target.

The SDK exposes `GetConnectedClients`, `SetDeck`, `SetActivePage`, `SetButtonProperties`, `SetButtonPropertiesEx`, `SetButtonsVisualState`, `SetControlsState`, and `SetVariables`. Subscribe to `OnConnectedClientsReceived`, `OnControlInteraction`, `OnVariablesChanged`, and `OnError` before issuing requests. `OnControlInteraction` supplies the inner payload as a JSON string; the other data callbacks provide typed objects.

Pass the configured API port explicitly if it differs from the SDK default. The optional constructor receive port defaults to 50301. A method's null `clientId` first falls back to the SDK object's `ClientId` property; only if that is also null do client-targeted commands broadcast. The demo does not set that property.

Receive callbacks run on a background thread. Keep handlers short, marshal UI work to the UI thread, and keep the receiver alive while a separate task performs animations or other long operations. Dispose the SDK instance when finished; its current `Stop()` method only suppresses outgoing sends. Send failures are not all delivered through `OnError`, so that event is not proof that every command succeeded.
