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
snoopwpfcli get-tree --pid <PID> [--window <INDEX>] [--format tree] [--verbose]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | Yes | Target process ID |
| `--window` | No | Window index (use `list-windows` to find) |
| `--format tree` | No | Human-readable tree instead of JSON |
| `--verbose` | No | Enable verbose output |

### get-subtree

```bash
snoopwpfcli get-subtree --pid <PID> (--name <NAME> | --text <TEXT> | --binding-path <PATH> | --type <TYPE> --hash <HASHCODE>) [--format tree] [--verbose]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | Yes | Target process ID |
| `--name` | No | Element name (x:Name). Alternative to `--type`/`--hash` |
| `--text` | No | Element text/content to search for. Alternative to `--name` or `--type`/`--hash` |
| `--binding-path` | No | Binding path to search for. Alternative to `--name`, `--text`, or `--type`/`--hash` |
| `--type` | No | Fully-qualified element type |
| `--hash` | No | Element hashcode |
| `--format tree` | No | Human-readable tree |
| `--verbose` | No | Enable verbose output |

### get-element

```bash
snoopwpfcli get-element --pid <PID> (--name <NAME> | --text <TEXT> | --type <TYPE> --hash <HASHCODE>) [--format json|tree] [--verbose]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | Yes | Target process ID |
| `--name` | No | Element name (x:Name). Alternative to `--type`/`--hash` |
| `--text` | No | Element text/content to search for. Alternative to `--name` or `--type`/`--hash` |
| `--type` | No | Fully-qualified element type |
| `--hash` | No | Element hashcode |
| `--format` | No | Output format: `json` or `tree` |
| `--verbose` | No | Enable verbose output |

### find-element

```bash
snoopwpfcli find-element --pid <PID> [--name <NAME>] [--text <TEXT>] [--automationid <ID>] [--type <TYPE>] [--binding-path <PATH>] [--interactive-only] [--verbose]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | Yes | Target process ID |
| `--name` | No | Element name (x:Name), exact match |
| `--text` | No | Text/content, partial match |
| `--automationid` | No | AutomationId, exact match |
| `--type` | No | Filter by element type |
| `--binding-path` | No | Find elements with a binding to this property path |
| `--interactive-only` | No | Filter results to interactive controls only (Button, TextBox, CheckBox, etc.) |
| `--verbose` | No | Enable verbose output |

At least one search criterion is required.

### invoke

```bash
snoopwpfcli invoke --pid <PID> (--name <NAME> | --text <TEXT> | --binding-path <PATH> | --type <TYPE> --hash <HASHCODE>) --action <ACTION> [--params <JSON>] [--verbose]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | Yes | Target process ID |
| `--name` | No | Element name (x:Name). Alternative to `--type`/`--hash` |
| `--text` | No | Element text/content to search for. Alternative to `--name` or `--type`/`--hash` |
| `--binding-path` | No | Binding path to search for. Alternative to `--name`, `--text`, or `--type`/`--hash` |
| `--type` | No | Fully-qualified element type |
| `--hash` | No | Element hashcode |
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
| `ButtonBase_Click` | Fire Click event on ButtonBase (RadioButton, ToggleButton) | none |
| `ExecuteCommand` | Execute the ICommand bound to the element | none |

### wait

```bash
snoopwpfcli wait --pid <PID> [--name <NAME>] [--text <TEXT>] [--automationid <ID>] [--type <TYPE>] [--until <CONDITION>] [--timeout <MS>] [--interval <MS>] [--interactive-only] [--verbose]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | Yes | Target process ID |
| `--name` | No | Element name (x:Name) to wait for |
| `--text` | No | Text/content to wait for (partial match) |
| `--automationid` | No | AutomationId to wait for |
| `--type` | No | Element type name to filter by |
| `--until` | No | Wait condition: `found` (default), `gone`, `enabled`, `disabled` |
| `--timeout` | No | Timeout in milliseconds (default: 30000) |
| `--interval` | No | Polling interval in milliseconds (default: 500) |
| `--interactive-only` | No | Filter results to interactive controls only (Button, TextBox, CheckBox, etc.) |
| `--verbose` | No | Enable verbose output |

### list-windows

```bash
snoopwpfcli list-windows --pid <PID> [--format json|tree] [--verbose]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | Yes | Target process ID |
| `--format` | No | Output format: `json` or `tree` |
| `--verbose` | No | Enable verbose output |

### get-datacontext

```bash
snoopwpfcli get-datacontext --pid <PID> (--name <NAME> | --text <TEXT> | --binding-path <PATH> | --type <TYPE> --hash <HASHCODE>) [--property <NAME>] [--verbose]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | Yes | Target process ID |
| `--name` | No | Element name (x:Name). Alternative to `--type`/`--hash` |
| `--text` | No | Element text/content to search for. Alternative to `--name` or `--type`/`--hash` |
| `--binding-path` | No | Binding path to search for. Alternative to `--name`, `--text`, or `--type`/`--hash` |
| `--type` | No | Fully-qualified element type |
| `--hash` | No | Element hashcode |
| `--property` | No | Return only a specific property |
| `--verbose` | No | Enable verbose output |

### screenshot

```bash
snoopwpfcli screenshot --pid <PID> [--window <INDEX>] [--output <PATH>] [--verbose]
```

| Option | Required | Description |
|--------|----------|-------------|
| `--pid` | Yes | Target process ID |
| `--window` | No | Window index (use `list-windows` to find) |
| `--output` | No | Save as PNG file. If omitted, outputs base64 JSON. |
| `--verbose` | No | Enable verbose output |
