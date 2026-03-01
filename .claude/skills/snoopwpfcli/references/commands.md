# SnoopWpfCLI Command Reference

## Commands

### list-processes

```bash
snoopwpfcli list-processes [--json] [--format json|tree] [--verbose]
```

| Option | Default | Description |
|--------|---------|-------------|
| `--json` | `true` | Output as JSON |
| `--format` | `json` | Output format: `json` or `tree` |
| `--verbose` | `false` | Enable verbose output |

### ping

```bash
snoopwpfcli ping --pid <PID> [--verbose]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | Yes | Target process ID |
| `--verbose` | No | Enable verbose output |

### get-tree

```bash
snoopwpfcli get-tree --pid <PID> [--format tree] [--verbose]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | Yes | Target process ID |
| `--format tree` | No | Human-readable tree instead of JSON |
| `--verbose` | No | Enable verbose output |

### get-subtree

```bash
snoopwpfcli get-subtree --pid <PID> --type <TYPE> --hash <HASHCODE> [--format tree] [--verbose]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | Yes | Target process ID |
| `--type` | Yes | Fully-qualified element type (e.g. `System.Windows.Controls.Button`) |
| `--hash` | Yes | Element hashcode |
| `--format tree` | No | Human-readable tree |
| `--verbose` | No | Enable verbose output |

### get-element

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

#### Supported Actions

| Action | Description | Params |
|--------|-------------|--------|
| `Invoke_Invoke` | Click a button | none |
| `Value_Get` | Get current text value | none |
| `Value_Set` | Set text value | `{"value":"..."}` |
| `Toggle_Toggle` | Toggle checkbox/toggle button | none |
| `Toggle_Status` | Get current toggle state | none |
| `SelectionItem_Select` | Select an item | none |
| `SelectionItem_AddToSelection` | Add to current selection | none |
| `SelectionItem_RemoveFromSelection` | Remove from selection | none |
| `SelectionItem_Status` | Get selection state | none |
| `ExpandCollapse_Expand` | Expand a node | none |
| `ExpandCollapse_Collapse` | Collapse a node | none |
| `ExpandCollapse_Toggle` | Toggle expand/collapse | none |
| `ExpandCollapse_Status` | Get expand/collapse state | none |
| `RangeValue_Get` | Get current range value | none |
| `RangeValue_Set` | Set range value | `{"value":...}` |
| `Scroll_Status` | Get scroll position | none |
| `Scroll_Scroll` | Scroll by amount | none |
| `Scroll_SetPosition` | Set absolute scroll position | none |

### screenshot

```bash
snoopwpfcli screenshot --pid <PID> [--output <PATH>] [--verbose]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | Yes | Target process ID |
| `--output` | No | Save as PNG file. If omitted, outputs base64 JSON. |
| `--verbose` | No | Enable verbose output |
