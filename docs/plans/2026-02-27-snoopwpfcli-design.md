# SnoopWpfCLI 設計書

## 概要

実行中のWPFアプリケーションのビジュアルツリーを取得・操作するCLIツール。
Playwright-CLIのWPF版として、CLIツール+スキルの組み合わせで巨大なビジュアルツリーを実用的に扱えるようにする。

## 背景

- SnoopWpfMcp（../SnoopWpfMcp）のMCPサーバー機能と同等のCLIツール
- MCPサーバー経由ではコンテキストウインドウがいっぱいになる問題を、CLIツール+Pythonフィルタリングで解決

## アーキテクチャ

### プロジェクト構成

```
SnoopWpfCLI/
├── snoopwpf/                    # git submodule（SnoopWPFインジェクター基盤）
├── src/
│   ├── App/                     # CLIアプリケーション本体
│   │   ├── Commands/            # サブコマンド定義（System.CommandLine）
│   │   ├── Services/            # InjectionService, WpfProcessService
│   │   ├── Models/              # データモデル
│   │   └── Formatters/          # JSON/TreeView出力フォーマッター
│   ├── WpfInspector/            # インジェクトDLL（SnoopWpfMcpからコピー・改良）
│   └── App.Tests/               # ユニットテスト
├── tests/
│   └── TestApp/                 # テスト用WPFアプリ
└── docs/
```

### 依存関係

```
CLI App ──→ InjectionService ──→ Snoop.InjectorLauncher (submodule)
   │                │
   │                └──→ Named Pipes ──→ WpfInspector (injected DLL)
   │
   └──→ System.CommandLine (CLIフレームワーク)
```

### SnoopWpfMcpとの違い

- MCPサーバー/HTTP層を完全に除去
- SemanticKernelへの依存を除去
- System.CommandLineでサブコマンド体系を構築
- --json/--format で出力形式切り替え
- 終了コードでスクリプト連携

## コマンド体系

### サブコマンド一覧

| コマンド | 対応MCPツール | 説明 |
|---------|-------------|------|
| `list-processes` | `get_wpf_processes` | 実行中WPFプロセス一覧 |
| `ping` | `ping` | DLLインジェクション＆通信確認 |
| `get-tree` | `get_visual_tree` | ビジュアルツリー全体取得 |
| `get-subtree` | `get_visual_tree_by_hashcode` | 指定要素のサブツリー取得 |
| `get-element` | `get_element_by_hashcode` | 特定要素の詳細取得 |
| `invoke` | `invoke_automation_peer` | UI要素に対する操作実行 |
| `screenshot` | `take_wpf_screenshot` | スクリーンショット取得 |

### コマンド詳細

#### list-processes

```bash
snoopwpf list-processes
snoopwpf list-processes --json
snoopwpf list-processes --format tree
```

出力例（JSON）:
```json
{
  "success": true,
  "count": 2,
  "processes": [
    {
      "processId": 1234,
      "processName": "MyApp",
      "mainWindowTitle": "My Application",
      "isWpfApplication": true
    }
  ]
}
```

#### ping

```bash
snoopwpf ping --pid 1234
```

#### get-tree

```bash
snoopwpf get-tree --pid 1234
snoopwpf get-tree --pid 1234 --format tree
```

#### get-subtree

```bash
snoopwpf get-subtree --pid 1234 --type System.Windows.Controls.Button --hash 5678
```

#### get-element

```bash
snoopwpf get-element --pid 1234 --type System.Windows.Controls.Button --hash 5678
```

#### invoke

```bash
snoopwpf invoke --pid 1234 --type System.Windows.Controls.Button --hash 5678 --action Invoke_Invoke
snoopwpf invoke --pid 1234 --type System.Windows.Controls.TextBox --hash 9012 --action Value_Set --params '{"value":"hello"}'
```

サポートするアクション:
- Invoke_Invoke（クリック）
- Value_Get / Value_Set（テキスト入出力）
- SelectionItem_Select / SelectionItem_AddToSelection / SelectionItem_RemoveFromSelection / SelectionItem_Status
- Toggle_Toggle / Toggle_Status（チェックボックス）
- ExpandCollapse_Expand / ExpandCollapse_Collapse / ExpandCollapse_Toggle / ExpandCollapse_Status
- RangeValue_Get / RangeValue_Set（スライダー等）
- Scroll_Status / Scroll_Scroll / Scroll_SetPosition

#### screenshot

```bash
snoopwpf screenshot --pid 1234 --output screenshot.png
snoopwpf screenshot --pid 1234  # base64 JSON出力
```

### 共通オプション

| オプション | デフォルト | 説明 |
|-----------|----------|------|
| `--json` | (デフォルト) | JSON出力 |
| `--format tree` | - | 人間可読ツリー表示 |
| `--timeout <ms>` | 30000 | 操作タイムアウト |
| `--verbose` | false | 詳細ログ出力 |

### 終了コード

| コード | 意味 |
|-------|------|
| 0 | 成功 |
| 1 | 一般エラー |
| 2 | プロセス未発見 |
| 3 | インジェクション失敗 |
| 4 | タイムアウト |

## 技術スタック

- .NET 10.0 + C#
- System.CommandLine（CLIフレームワーク）
- System.Text.Json（JSONシリアライズ）
- XUnit（テスト）
- snoopwpf（git submodule、インジェクター基盤）

## コードの由来

以下のファイルはSnoopWpfMcp（../SnoopWpfMcp）からコピー・改良:

- **WpfInspector/Inspector.cs** - インジェクトDLLのメインロジック
- **WpfInspector/AutomationPeerHandler.cs** - UI Automation操作
- **WpfInspector/DataContextTracker.cs** - DataContext管理
- **WpfInspector/DependencyPropertyCache.cs** - DependencyPropertyキャッシュ
- **Services/InjectionService.cs** - DLLインジェクション＆Named Pipe通信
- **Services/WpfProcessService.cs** - WPFプロセス検出
- **Models/** - データモデル類

除去するもの:
- WpfInspectorPlugin.cs（MCP関数定義 → CLI Commandsに置換）
- McpServer.cs（JSON-RPC stdio → 不要）
- HttpMcpController.cs（HTTP MCP → 不要）
- Startup.cs（ASP.NET Core → 不要）
- SemanticKernel依存

## テスト戦略

### テスト用WPFアプリ (tests/TestApp/)

既知の構造を持つWPFアプリ:
- Button, TextBox, ListBox, ComboBox, CheckBox
- DataBinding（ViewModel）
- DataTemplate, ControlTemplate
- 入れ子構造（Grid > StackPanel > Button）

### 統合テスト

1. TestAppを起動
2. CLIを実行してビジュアルツリー取得
3. 期待するJSON構造と比較
4. UI操作（invoke）の結果検証

### ユニットテスト

- フォーマッター（JSON/ツリー表示）
- モデル変換
- プロセスフィルタリングロジック
- コマンドライン引数パース
