---
name: snoopwpfcli
description: Inspect and interact with running WPF applications via the snoopwpfcli CLI. Use when you need to (1) discover running WPF processes, (2) inspect the visual tree of a WPF window, (3) find UI elements by name/text/AutomationId, (4) perform UI actions like clicking buttons, entering text, toggling checkboxes, firing ICommand, or scrolling, (5) wait for elements to appear/disappear/change state, (6) inspect multiple windows and dialogs, (7) read ViewModel DataContext properties, (8) capture screenshots of WPF windows, or (9) automate E2E testing flows for WPF applications. Triggers on any mention of WPF inspection, WPF UI automation, WPF visual tree, or snoopwpfcli.
---

# SnoopWpfCLI

CLI tool for inspecting and automating running WPF applications via DLL injection and Named Pipes. `snoopwpfcli` must be on PATH.

## Core Workflow

```
1. list-processes       -> Find target PID
2. ping --pid PID       -> Inject inspector DLL (required once per process)
3. find-element / get-tree / invoke / wait / screenshot  -> Inspect or interact
```

## Quick Reference

### 1. Discover & Inject

```bash
snoopwpfcli list-processes
snoopwpfcli ping --pid <PID>
```

### 2. Find Elements (stable, name-based)

```bash
# By name (x:Name)
snoopwpfcli find-element --pid <PID> --name CountButton

# By text content (partial match)
snoopwpfcli find-element --pid <PID> --text "Click Me"

# By AutomationId
snoopwpfcli find-element --pid <PID> --automationid BtnSubmit

# Combine filters
snoopwpfcli find-element --pid <PID> --type System.Windows.Controls.Button --text "OK"
```

### 3. Get Visual Tree

```bash
snoopwpfcli get-tree --pid <PID> --format tree
snoopwpfcli get-tree --pid <PID> --window 1          # specific window
```

### 4. Inspect Elements

```bash
# By name (recommended -- stable across restarts)
snoopwpfcli get-element --pid <PID> --name CountButton

# By type + hashcode (from tree output)
snoopwpfcli get-element --pid <PID> --type System.Windows.Controls.Button --hash 56789
```

### 5. Perform Actions

```bash
# By name
snoopwpfcli invoke --pid <PID> --name CountButton --action Invoke_Invoke

# By type + hash
snoopwpfcli invoke --pid <PID> --type <TYPE> --hash <HASH> --action <ACTION>
```

Common actions:

| Action | Use for | Params |
|--------|---------|--------|
| `Invoke_Invoke` | Click buttons | none |
| `Value_Set` | Set text | `--params '{"value":"text"}'` |
| `Value_Get` | Read text value | none |
| `Toggle_Toggle` | Toggle checkbox | none |
| `SelectionItem_Select` | Select item | none |
| `RangeValue_Set` | Set slider/progress | `--params '{"value":50}'` |
| `ExpandCollapse_Toggle` | Expand/collapse | none |
| `ButtonBase_Click` | Fire Click on RadioButton/ToggleButton (triggers ICommand) | none |
| `ExecuteCommand` | Execute bound ICommand directly | none |

Full action list: See [references/commands.md](references/commands.md)

### 6. Wait for State Changes

```bash
# Wait for element to appear
snoopwpfcli wait --pid <PID> --name LoadingSpinner --until found --timeout 5000

# Wait for element to disappear
snoopwpfcli wait --pid <PID> --name LoadingSpinner --until gone --timeout 10000

# Wait for element to become enabled
snoopwpfcli wait --pid <PID> --name SubmitButton --until enabled --timeout 5000

# Wait for specific text
snoopwpfcli wait --pid <PID> --text "Complete" --timeout 5000
```

### 7. Multiple Windows

```bash
snoopwpfcli list-windows --pid <PID>
snoopwpfcli get-tree --pid <PID> --window 1
snoopwpfcli screenshot --pid <PID> --window 1 --output dialog.png
```

### 8. Read DataContext (ViewModel)

```bash
snoopwpfcli get-datacontext --pid <PID> --type <TYPE> --hash <HASH>
snoopwpfcli get-datacontext --pid <PID> --type <TYPE> --hash <HASH> --property Title
```

### 9. Capture Screenshots

```bash
snoopwpfcli screenshot --pid <PID> --output screenshot.png
snoopwpfcli screenshot --pid <PID>   # base64 JSON output
```

## E2E Testing Pattern

```bash
# 1. Find and inject
PID=$(snoopwpfcli list-processes | jq '.processes[0].processId')
snoopwpfcli ping --pid $PID

# 2. Interact using stable names (no hashcode needed)
snoopwpfcli invoke --pid $PID --name InputTextBox --action Value_Set --params '{"value":"Hello"}'
snoopwpfcli invoke --pid $PID --name SubmitButton --action Invoke_Invoke

# 3. Wait for result
snoopwpfcli wait --pid $PID --name StatusText --text "Success" --timeout 5000

# 4. Verify
snoopwpfcli get-element --pid $PID --name ResultLabel

# 5. Screenshot for evidence
snoopwpfcli screenshot --pid $PID --output result.png
```

## Output

- **JSON results** (success and error) are always written to **stdout**.
- **stderr** is reserved for verbose/diagnostic output (`--verbose`).
- **Exit code** determines success (0) or failure (non-zero).
- This means `command | jq '.error'` works reliably regardless of outcome.

## Tips

- **Prefer `--name` over `--type`/`--hash`**: Names are stable across process restarts.
- **Multiple windows**: Use `list-windows` then `--window <index>` on get-tree/screenshot.
- **RadioButton with ICommand**: Use `ButtonBase_Click` instead of `SelectionItem_Select`.
- **Re-injection**: If the target app restarts, run `ping` again.
- **Verbose mode**: Add `--verbose` to any command for detailed logging.

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | General error |
| 2 | Process not found |
| 3 | Injection failed |
| 4 | Timeout |
