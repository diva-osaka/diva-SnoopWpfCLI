# SnoopWpfCLI

> Playwright-CLI for WPF -- Inspect and interact with running WPF applications from the command line.

[English](README.md) | [日本語](README.ja.md)

## Features

- **list-processes** -- Discover running WPF processes on the system
- **ping** -- Inject the inspector DLL and verify communication
- **get-tree** -- Retrieve the full visual tree of a WPF window (JSON or human-readable tree)
- **get-subtree** -- Retrieve a subtree rooted at a specific element
- **get-element** -- Get detailed information about a single element
- **invoke** -- Execute UI Automation actions (click buttons, set text, toggle checkboxes, etc.)
- **screenshot** -- Capture a screenshot of the WPF window (save to file or output as base64)

## Prerequisites

- **.NET 10.0 SDK** (or later)
- **Windows 10 / 11**

## Installation

```bash
git clone --recursive https://github.com/diva-osaka/diva-SnoopWpfCLI.git
cd diva-SnoopWpfCLI
dotnet publish src/App/App.csproj -c Release -o ./publish
```

The `./publish` folder contains `snoopwpfcli.exe` and all required DLLs. Add this folder to your `PATH` to use `snoopwpfcli` from anywhere.

> The `--recursive` flag is required to fetch the [SnoopWPF](https://github.com/snoopwpf/snoopwpf) submodule used for DLL injection.

## Quick Start

This walkthrough uses the included **TestApp** to demonstrate the full workflow.

### 1. Build and launch the TestApp

```bash
dotnet build tests/TestApp/TestApp.csproj
dotnet run --project tests/TestApp/TestApp.csproj
```

The TestApp window opens with tabs for Basic Controls, Selection Controls, Nested Structure, and Template Test.

### 2. List WPF processes

```bash
dotnet run --project src/App/App.csproj -- list-processes
```

```json
{
  "success": true,
  "count": 1,
  "processes": [
    {
      "processId": 12345,
      "processName": "TestApp",
      "mainWindowTitle": "SnoopWpfCLI Test App",
      "isWpfApplication": true,
      "hasMainWindow": true
    }
  ]
}
```

Note the `processId` value -- you will use it for all subsequent commands.

### 3. Ping (inject the inspector DLL)

```bash
dotnet run --project src/App/App.csproj -- ping --pid 12345
```

```json
{
  "success": true,
  "processId": 12345,
  "message": "Ping successful",
  "wasAlreadyInjected": false
}
```

### 4. Get the full visual tree (JSON)

```bash
dotnet run --project src/App/App.csproj -- get-tree --pid 12345
```

The response contains the entire visual tree as a nested JSON structure inside `visualTreeJson`:

```json
{
  "success": true,
  "processId": 12345,
  "processName": "TestApp",
  "windowTitle": "SnoopWpfCLI Test App",
  "visualTreeJson": "{ ... nested visual tree ... }"
}
```

### 5. Get the visual tree (human-readable tree)

```bash
dotnet run --project src/App/App.csproj -- get-tree --pid 12345 --format tree
```

```
Window "SnoopWpfCLI Test App"
└─ Grid
   ├─ Border
   │  └─ StackPanel
   │     ├─ TextBlock "SnoopWpfCLI Test Application"  [HeaderTitle]
   │     └─ TextBlock  [ProcessInfoText]
   ├─ TabControl
   │  ├─ TabItem "Basic Controls"  [BasicControlsTab]
   │  │  └─ ScrollViewer
   │  │     └─ StackPanel
   │  │        ├─ GroupBox "Text Input"
   │  │        │  ├─ TextBox  [InputTextBox]
   │  │        │  └─ TextBox  [MirrorTextBox]
   │  │        ├─ GroupBox "Buttons"
   │  │        │  ├─ Button "Click Me"  [CountButton]
   │  │        │  └─ Button "Custom Template Button"  [CustomStyledButton]
   │  │        ├─ GroupBox "Toggle Controls"
   │  │        │  ├─ CheckBox "Bound CheckBox"  [BoundCheckBox]
   │  │        │  ├─ CheckBox "Three-State CheckBox"  [ThreeStateCheckBox]
   │  │        │  └─ ToggleButton "Toggle Button"  [TestToggleButton]
   │  │        └─ GroupBox "Range Controls"
   │  │           ├─ Slider  [TestSlider]
   │  │           └─ ProgressBar  [TestProgressBar]
   │  ├─ TabItem "Selection Controls"  [SelectionTab]
   │  ├─ TabItem "Nested Structure"  [NestedTab]
   │  └─ TabItem "Template Test"  [TemplateTab]
   └─ StatusBar
      └─ TextBlock  [StatusText]
```

### 6. Get a specific element

Find an element from the tree output (e.g., the `CountButton` with type `System.Windows.Controls.Button` and hashcode `56789`), then query its details:

```bash
dotnet run --project src/App/App.csproj -- get-element --pid 12345 \
    --type System.Windows.Controls.Button --hash 56789
```

```json
{
  "success": true,
  "processId": 12345,
  "type": "System.Windows.Controls.Button",
  "hashcode": 56789,
  "message": "Element retrieved successfully",
  "element": {
    "type": "System.Windows.Controls.Button",
    "hashcode": 56789,
    "name": "CountButton",
    "content": "Click Me",
    "automationPatterns": ["Invoke"]
  }
}
```

### 7. Invoke an action (click a button)

```bash
dotnet run --project src/App/App.csproj -- invoke --pid 12345 \
    --type System.Windows.Controls.Button --hash 56789 \
    --action Invoke_Invoke
```

```json
{
  "success": true,
  "processId": 12345,
  "type": "System.Windows.Controls.Button",
  "hashcode": 56789,
  "action": "Invoke_Invoke",
  "message": "Action invoked successfully"
}
```

The click counter in the TestApp increments.

### 8. Take a screenshot

```bash
dotnet run --project src/App/App.csproj -- screenshot --pid 12345 \
    --output screenshot.png
```

```json
{
  "success": true,
  "processId": 12345,
  "processName": "TestApp",
  "message": "Screenshot saved to screenshot.png",
  "windowTitle": "SnoopWpfCLI Test App",
  "width": 900,
  "height": 700,
  "filePath": "C:\\...\\screenshot.png",
  "format": "PNG"
}
```

## Command Reference

### list-processes

List running WPF processes.

```bash
snoopwpfcli list-processes [--json] [--format json|tree] [--verbose]
```

| Option | Default | Description |
|--------|---------|-------------|
| `--json` | `true` | Output as JSON |
| `--format` | `json` | Output format: `json` or `tree` |
| `--verbose` | `false` | Enable verbose output |

### ping

Inject the inspector DLL into a WPF process and verify communication.

```bash
snoopwpfcli ping --pid <PID> [--verbose]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | Yes | Target process ID |
| `--verbose` | No | Enable verbose output |

### get-tree

Retrieve the full visual tree of the target WPF window.

```bash
snoopwpfcli get-tree --pid <PID> [--format tree] [--verbose]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | Yes | Target process ID |
| `--format tree` | No | Output as human-readable tree instead of JSON |
| `--verbose` | No | Enable verbose output |

### get-subtree

Retrieve the subtree rooted at a specific element.

```bash
snoopwpfcli get-subtree --pid <PID> --type <TYPE> --hash <HASHCODE> [--format tree] [--verbose]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | Yes | Target process ID |
| `--type` | Yes | Fully-qualified element type (e.g. `System.Windows.Controls.Button`) |
| `--hash` | Yes | Element hashcode |
| `--format tree` | No | Output as human-readable tree |
| `--verbose` | No | Enable verbose output |

### get-element

Get detailed information about a single element.

```bash
snoopwpfcli get-element --pid <PID> --type <TYPE> --hash <HASHCODE> [--verbose]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | Yes | Target process ID |
| `--type` | Yes | Fully-qualified element type |
| `--hash` | Yes | Element hashcode |
| `--verbose` | No | Enable verbose output |

### invoke

Execute a UI Automation action on an element.

```bash
snoopwpfcli invoke --pid <PID> --type <TYPE> --hash <HASHCODE> --action <ACTION> [--params <JSON>] [--verbose]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | Yes | Target process ID |
| `--type` | Yes | Fully-qualified element type |
| `--hash` | Yes | Element hashcode |
| `--action` | Yes | Automation peer action name |
| `--params` | No | Additional parameters as JSON string |
| `--verbose` | No | Enable verbose output |

**Supported actions:**

| Action | Description |
|--------|-------------|
| `Invoke_Invoke` | Click a button |
| `Value_Get` | Get the current text value |
| `Value_Set` | Set a text value (requires `--params '{"value":"..."}'`) |
| `Toggle_Toggle` | Toggle a checkbox or toggle button |
| `Toggle_Status` | Get the current toggle state |
| `SelectionItem_Select` | Select an item |
| `SelectionItem_AddToSelection` | Add to current selection |
| `SelectionItem_RemoveFromSelection` | Remove from selection |
| `SelectionItem_Status` | Get selection state |
| `ExpandCollapse_Expand` | Expand a node |
| `ExpandCollapse_Collapse` | Collapse a node |
| `ExpandCollapse_Toggle` | Toggle expand/collapse |
| `ExpandCollapse_Status` | Get expand/collapse state |
| `RangeValue_Get` | Get the current range value |
| `RangeValue_Set` | Set a range value (requires `--params '{"value":...}'`) |
| `Scroll_Status` | Get scroll position |
| `Scroll_Scroll` | Scroll by amount |
| `Scroll_SetPosition` | Set absolute scroll position |

### screenshot

Capture a screenshot of the WPF window.

```bash
snoopwpfcli screenshot --pid <PID> [--output <PATH>] [--verbose]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | Yes | Target process ID |
| `--output` | No | Save as PNG file. If omitted, outputs base64 JSON. |
| `--verbose` | No | Enable verbose output |

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | General error |
| 2 | Process not found |
| 3 | Injection failed |
| 4 | Timeout |

## Architecture

SnoopWpfCLI uses **DLL injection** and **Named Pipes** to communicate with target WPF processes.

```
                          Named Pipe (IPC)
  +-----------+          +------------------+          +------------------+
  |           |  inject  |                  |  query/  |                  |
  |  CLI App  | -------> | WpfInspector.dll | <------> |  Target WPF App  |
  |           |          | (injected DLL)   |  respond |  (e.g. TestApp)  |
  +-----------+          +------------------+          +------------------+
       |
       v
  System.CommandLine         Snoop.InjectorLauncher
  (CLI framework)            (from snoopwpf submodule)
```

1. The CLI uses **Snoop.InjectorLauncher** (from the snoopwpf submodule) to inject `WpfInspector.dll` into the target process.
2. Once injected, the DLL starts a **Named Pipe server** inside the target process.
3. The CLI communicates with the injected DLL over Named Pipes to query the visual tree, invoke actions, and capture screenshots.

## Development

### Build

```bash
dotnet build src/SnoopWpfCLI.slnx
```

### Run

```bash
dotnet run --project src/App/App.csproj --framework net10.0-windows -- <command> [options]
```

### Run Tests

```bash
dotnet test src/App.Tests/App.Tests.csproj
```

### Project Structure

```
SnoopWpfCLI/
├── snoopwpf/                    # Git submodule (SnoopWPF injector)
├── src/
│   ├── App/                     # CLI application
│   │   ├── Commands/            # Subcommand definitions (System.CommandLine)
│   │   ├── Services/            # InjectionService, WpfProcessService
│   │   ├── Models/              # Data models
│   │   └── Formatters/          # Output formatters (JSON / tree)
│   ├── WpfInspector/            # Injected DLL (visual tree inspection)
│   └── App.Tests/               # Unit tests (xUnit)
├── tests/
│   └── TestApp/                 # Sample WPF app for testing
└── docs/
    ├── plans/                   # Design documents
    ├── specs/                   # Specifications
    └── references/              # Reference materials
```

## License

MIT

## Acknowledgements

- [SnoopWPF](https://github.com/snoopwpf/snoopwpf) -- The WPF inspection tool that provides the DLL injection foundation
- [SnoopWpfMcp](https://github.com/aoyagi/SnoopWpfMcp) -- The MCP server implementation this CLI is derived from
