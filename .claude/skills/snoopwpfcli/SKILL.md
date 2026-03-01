---
name: snoopwpfcli
description: Inspect and interact with running WPF applications via the snoopwpfcli CLI. Use when you need to (1) discover running WPF processes, (2) inspect the visual tree of a WPF window, (3) find UI elements by name/type, (4) perform UI actions like clicking buttons, entering text, toggling checkboxes, or scrolling, (5) capture screenshots of WPF windows, or (6) automate E2E testing flows for WPF applications. Triggers on any mention of WPF inspection, WPF UI automation, WPF visual tree, or snoopwpfcli.
---

# SnoopWpfCLI

CLI tool for inspecting and automating running WPF applications via DLL injection and Named Pipes. `snoopwpfcli` must be on PATH.

## Core Workflow

Every interaction follows this sequence:

```
1. list-processes  -> Find target PID
2. ping --pid PID  -> Inject inspector DLL (required once per process)
3. get-tree / get-element / invoke / screenshot  -> Inspect or interact
```

## Quick Reference

### 1. Discover WPF Processes

```bash
snoopwpfcli list-processes
```

Returns JSON with `processId`, `processName`, `mainWindowTitle`, `isWpfApplication`.

### 2. Inject Inspector

```bash
snoopwpfcli ping --pid <PID>
```

Must run once before any inspection command. Idempotent -- safe to call again.

### 3. Get Visual Tree

```bash
# JSON format (default)
snoopwpfcli get-tree --pid <PID>

# Human-readable tree
snoopwpfcli get-tree --pid <PID> --format tree
```

Tree format shows: `Type "Content"  [Name]` with indent hierarchy. Use `--format tree` for quick scanning, JSON for programmatic access.

### 4. Find Elements

Elements are identified by `--type` (fully-qualified .NET type) and `--hash` (hashcode). Get these from the tree output.

```bash
# Get subtree from a specific element
snoopwpfcli get-subtree --pid <PID> --type <TYPE> --hash <HASH>

# Get single element details (includes automationPatterns)
snoopwpfcli get-element --pid <PID> --type <TYPE> --hash <HASH>
```

### 5. Perform Actions

```bash
snoopwpfcli invoke --pid <PID> --type <TYPE> --hash <HASH> --action <ACTION> [--params <JSON>]
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

Full action list: See [references/commands.md](references/commands.md)

### 6. Capture Screenshots

```bash
# Save to file
snoopwpfcli screenshot --pid <PID> --output screenshot.png

# Get base64 JSON
snoopwpfcli screenshot --pid <PID>
```

## E2E Testing Pattern

For multi-step automation, chain commands sequentially. Always verify state between actions:

```bash
# 1. Find and inject
PID=$(snoopwpfcli list-processes | jq '.processes[0].processId')
snoopwpfcli ping --pid $PID

# 2. Get tree to find elements
snoopwpfcli get-tree --pid $PID --format tree

# 3. Interact (click button, verify result)
snoopwpfcli invoke --pid $PID --type System.Windows.Controls.Button --hash 12345 --action Invoke_Invoke

# 4. Verify state changed
snoopwpfcli get-element --pid $PID --type System.Windows.Controls.TextBlock --hash 67890

# 5. Screenshot for evidence
snoopwpfcli screenshot --pid $PID --output result.png
```

## Tips

- **Element identification**: Use `[Name]` from tree output to find elements reliably, then get `--type` and `--hash` from the tree's JSON format.
- **Multiple windows**: `list-processes` returns all WPF processes. Ping each separately.
- **Re-injection**: If the target app restarts, run `ping` again.
- **All commands**: Add `--verbose` for detailed logging on failures.

## Exit Codes

| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | General error |
| 2 | Process not found |
| 3 | Injection failed |
| 4 | Timeout |
