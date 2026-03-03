# SnoopWpfCLI

> Playwright-CLI for WPF -- 実行中のWPFアプリケーションをコマンドラインから検査・操作するツール

[English](README.md) | [日本語](README.ja.md)

## 機能

- **list-processes** -- 実行中のWPFプロセスを検出・一覧表示
- **ping** -- インスペクタDLLをインジェクションし、通信を確認
- **get-tree** -- WPFウィンドウのビジュアルツリー全体を取得（JSON形式またはツリー形式）
- **get-subtree** -- 指定した要素を起点としたサブツリーを取得
- **get-element** -- 単一要素の詳細情報を取得
- **find-element** -- 名前、テキスト、AutomationIdで要素を検索
- **invoke** -- UI Automationアクションを実行（ボタンクリック、テキスト入力、チェックボックス操作など）
- **wait** -- 要素の出現・消失・状態変化を待機
- **list-windows** -- WPFアプリケーション内の全ウィンドウを一覧表示
- **get-datacontext** -- 要素にバインドされたViewModelのプロパティを取得
- **screenshot** -- WPFウィンドウのスクリーンショットを取得（ファイル保存またはbase64出力）
- **assert** -- 要素の存在、テキスト内容、DataContextプロパティ値をアサーション

## 前提条件

- **.NET 10.0 SDK** 以降
- **Windows 10 / 11**

## インストール

```bash
git clone --recursive https://github.com/diva-osaka/diva-SnoopWpfCLI.git
cd diva-SnoopWpfCLI
dotnet publish src/App/App.csproj -c Release -o ./publish
```

`./publish` フォルダに `snoopwpfcli.exe` と必要なDLLがすべて含まれます。このフォルダを `PATH` に追加すると、`snoopwpfcli` コマンドとしてどこからでも使えます。

> `--recursive` フラグは、DLLインジェクションに使用する [SnoopWPF](https://github.com/snoopwpf/snoopwpf) サブモジュールの取得に必要です。

## クイックスタート

付属の **TestApp** を使って一通りのワークフローを体験できます。

### 1. TestApp をビルド・起動

```bash
dotnet build tests/TestApp/TestApp.csproj
dotnet run --project tests/TestApp/TestApp.csproj
```

TestApp のウィンドウが開きます。Basic Controls、Selection Controls、Nested Structure、Template Test の4つのタブがあります。

### 2. WPFプロセスの一覧表示

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

`processId` の値を以降のコマンドで使用します。

### 3. Ping（インスペクタDLLのインジェクション）

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

### 4. ビジュアルツリーの取得（JSON形式）

```bash
dotnet run --project src/App/App.csproj -- get-tree --pid 12345
```

レスポンスの `visualTreeJson` にビジュアルツリー全体がネストされたJSON構造として格納されます。

```json
{
  "success": true,
  "processId": 12345,
  "processName": "TestApp",
  "windowTitle": "SnoopWpfCLI Test App",
  "visualTreeJson": "{ ... ネストされたビジュアルツリー ... }"
}
```

### 5. ビジュアルツリーの取得（ツリー形式）

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

### 6. 特定要素の取得

ツリー出力から要素を特定し（例: `CountButton`、型 `System.Windows.Controls.Button`、ハッシュコード `56789`）、詳細情報を取得します。

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

### 7. アクションの実行（ボタンクリック）

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

TestApp のクリックカウンターが増加します。

### 8. スクリーンショットの取得

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

## コマンドリファレンス

### list-processes

実行中のWPFプロセスを一覧表示します。

```bash
snoopwpfcli list-processes [--json] [--format json|tree] [--verbose]
```

| オプション | デフォルト | 説明 |
|-----------|----------|------|
| `--json` | `true` | JSON形式で出力 |
| `--format` | `json` | 出力形式: `json` または `tree` |
| `--verbose` | `false` | 詳細ログを出力 |

### ping

WPFプロセスにインスペクタDLLをインジェクションし、通信を確認します。

```bash
snoopwpfcli ping --pid <PID> [--verbose]
```

| オプション | 必須 | 説明 |
|-----------|------|------|
| `--pid` | はい | 対象プロセスID |
| `--verbose` | いいえ | 詳細ログを出力 |

### get-tree

対象WPFウィンドウのビジュアルツリー全体を取得します。

```bash
snoopwpfcli get-tree --pid <PID> [--window <INDEX>] [--format tree] [--verbose]
```

| オプション | 必須 | 説明 |
|-----------|------|------|
| `--pid` | はい | 対象プロセスID |
| `--window` | いいえ | ウィンドウインデックス（`list-windows` で確認） |
| `--format tree` | いいえ | JSON形式の代わりに人間可読のツリー形式で出力 |
| `--verbose` | いいえ | 詳細ログを出力 |

### get-subtree

指定した要素を起点としたサブツリーを取得します。

```bash
snoopwpfcli get-subtree --pid <PID> (--name <NAME> | --type <TYPE> --hash <HASH>) [--format tree] [--verbose]
```

| オプション | 必須 | 説明 |
|-----------|------|------|
| `--pid` | はい | 対象プロセスID |
| `--name` | いいえ | 要素名（x:Name）。`--type`/`--hash` の代替 |
| `--type` | いいえ | 要素の完全修飾型名（例: `System.Windows.Controls.Button`） |
| `--hash` | いいえ | 要素のハッシュコード |
| `--format tree` | いいえ | 人間可読のツリー形式で出力 |
| `--verbose` | いいえ | 詳細ログを出力 |

### get-element

単一要素の詳細情報を取得します。

```bash
snoopwpfcli get-element --pid <PID> (--name <NAME> | --type <TYPE> --hash <HASH>) [--verbose]
```

| オプション | 必須 | 説明 |
|-----------|------|------|
| `--pid` | はい | 対象プロセスID |
| `--name` | いいえ | 要素名（x:Name）。`--type`/`--hash` の代替 |
| `--type` | いいえ | 要素の完全修飾型名 |
| `--hash` | いいえ | 要素のハッシュコード |
| `--verbose` | いいえ | 詳細ログを出力 |

### find-element

名前、テキスト、AutomationId、バインディングパスで要素を検索します。

```bash
snoopwpfcli find-element --pid <PID> [--name <NAME>] [--text <TEXT>] [--automationid <ID>] [--type <TYPE>] [--binding-path <PATH>] [--verbose]
```

| オプション | 必須 | 説明 |
|-----------|------|------|
| `--pid` | はい | 対象プロセスID |
| `--name` | いいえ | 要素名（x:Name）、完全一致 |
| `--text` | いいえ | テキスト/コンテンツ、部分一致 |
| `--automationid` | いいえ | AutomationId、完全一致 |
| `--type` | いいえ | 要素型でフィルタ |
| `--binding-path` | いいえ | 指定したプロパティパスへのバインディングを持つ要素を検索 |
| `--verbose` | いいえ | 詳細ログを出力 |

検索条件（`--name`、`--text`、`--automationid`、`--type`、`--binding-path`）のうち少なくとも1つが必要です。

### invoke

要素に対してUI Automationアクションを実行します。

```bash
snoopwpfcli invoke --pid <PID> (--name <NAME> | --type <TYPE> --hash <HASH>) --action <ACTION> [--params <JSON>] [--verbose]
```

| オプション | 必須 | 説明 |
|-----------|------|------|
| `--pid` | はい | 対象プロセスID |
| `--name` | いいえ | 要素名（x:Name）。`--type`/`--hash` の代替 |
| `--type` | いいえ | 要素の完全修飾型名 |
| `--hash` | いいえ | 要素のハッシュコード |
| `--action` | はい | Automation Peerアクション名 |
| `--params` | いいえ | 追加パラメータ（JSON文字列） |
| `--verbose` | いいえ | 詳細ログを出力 |

**サポートされるアクション:**

| アクション | 説明 |
|-----------|------|
| `Invoke_Invoke` | ボタンをクリック |
| `Value_Get` | 現在のテキスト値を取得 |
| `Value_Set` | テキスト値を設定（`--params '{"value":"..."}'` が必要） |
| `Toggle_Toggle` | チェックボックスやトグルボタンの切り替え |
| `Toggle_Status` | 現在のトグル状態を取得 |
| `SelectionItem_Select` | アイテムを選択 |
| `SelectionItem_AddToSelection` | 選択に追加 |
| `SelectionItem_RemoveFromSelection` | 選択から削除 |
| `SelectionItem_Status` | 選択状態を取得 |
| `ExpandCollapse_Expand` | ノードを展開 |
| `ExpandCollapse_Collapse` | ノードを折りたたみ |
| `ExpandCollapse_Toggle` | 展開/折りたたみを切り替え |
| `ExpandCollapse_Status` | 展開/折りたたみ状態を取得 |
| `RangeValue_Get` | 現在の範囲値を取得 |
| `RangeValue_Set` | 範囲値を設定（`--params '{"value":...}'` が必要） |
| `Scroll_Status` | スクロール位置を取得 |
| `Scroll_Scroll` | スクロール量を指定してスクロール |
| `Scroll_SetPosition` | スクロール位置を絶対値で設定 |
| `ButtonBase_Click` | ButtonBase派生要素（RadioButton、ToggleButton）のClickイベントを発火 |
| `ExecuteCommand` | 要素にバインドされたICommandを実行 |

### wait

要素の出現・消失・状態変化を待機します。

```bash
snoopwpfcli wait --pid <PID> [--name <NAME>] [--text <TEXT>] [--automationid <ID>] [--type <TYPE>] [--until <CONDITION>] [--timeout <MS>] [--interval <MS>] [--verbose]
```

| オプション | 必須 | 説明 |
|-----------|------|------|
| `--pid` | はい | 対象プロセスID |
| `--name` | いいえ | 待機対象の要素名（x:Name） |
| `--text` | いいえ | 待機対象のテキスト/コンテンツ（部分一致） |
| `--automationid` | いいえ | 待機対象のAutomationId |
| `--type` | いいえ | 要素型でフィルタ |
| `--until` | いいえ | 待機条件: `found`（デフォルト）、`gone`、`enabled`、`disabled` |
| `--timeout` | いいえ | タイムアウト（ミリ秒、デフォルト: 30000） |
| `--interval` | いいえ | ポーリング間隔（ミリ秒、デフォルト: 500） |
| `--verbose` | いいえ | 詳細ログを出力 |

### list-windows

WPFアプリケーション内の全ウィンドウを一覧表示します。

```bash
snoopwpfcli list-windows --pid <PID> [--format json|tree] [--verbose]
```

| オプション | 必須 | 説明 |
|-----------|------|------|
| `--pid` | はい | 対象プロセスID |
| `--format` | いいえ | 出力形式: `json` または `tree` |
| `--verbose` | いいえ | 詳細ログを出力 |

### get-datacontext

要素のDataContextにバインドされたViewModelのプロパティを取得します。

```bash
snoopwpfcli get-datacontext --pid <PID> --type <TYPE> --hash <HASH> [--property <NAME>] [--verbose]
```

| オプション | 必須 | 説明 |
|-----------|------|------|
| `--pid` | はい | 対象プロセスID |
| `--type` | はい | 要素の完全修飾型名 |
| `--hash` | はい | 要素のハッシュコード |
| `--property` | いいえ | 特定プロパティのみ取得 |
| `--verbose` | いいえ | 詳細ログを出力 |

### screenshot

WPFウィンドウのスクリーンショットを取得します。

```bash
snoopwpfcli screenshot --pid <PID> [--window <INDEX>] [--output <PATH>] [--verbose]
```

| オプション | 必須 | 説明 |
|-----------|------|------|
| `--pid` | はい | 対象プロセスID |
| `--window` | いいえ | ウィンドウインデックス（`list-windows` で確認） |
| `--output` | いいえ | PNGファイルとして保存。省略時はbase64形式のJSONを出力。 |
| `--verbose` | いいえ | 詳細ログを出力 |

### assert

要素の存在、テキスト内容、DataContextプロパティ値をアサーションします。自動UIテストに最適です。

```bash
snoopwpfcli assert --pid <PID> [--name <NAME>] [--text <TEXT>] [--automationid <ID>] [--type <TYPE>] [--hash <HASH>] [--exists] [--property <NAME>] [--expected <VALUE>] [--format json|tree] [--verbose]
```

| オプション | 必須 | 説明 |
|-----------|------|------|
| `--pid` | はい | 対象プロセスID |
| `--name` | いいえ | 要素名（x:Name） |
| `--text` | いいえ | 検索対象のテキスト/コンテンツ（部分一致）、またはアサート対象値（完全一致） |
| `--automationid` | いいえ | AutomationId |
| `--type` | いいえ | 要素型名 |
| `--hash` | いいえ | 要素のハッシュコード（`--type` と併用） |
| `--exists` | いいえ | 要素の存在をアサート |
| `--property` | いいえ | アサート対象のDataContextプロパティ名 |
| `--expected` | いいえ | `--property` アサーションの期待値 |
| `--format` | いいえ | 出力形式: `json` または `tree` |
| `--verbose` | いいえ | 詳細ログを出力 |

アサーションモード（`--exists`、`--text`、`--property`）は排他的です。

**使用例:**

```bash
# 要素が存在することをアサート
snoopwpfcli assert --pid 12345 --name StatusText --exists

# 要素のテキストが一致することをアサート
snoopwpfcli assert --pid 12345 --name StatusText --text "Success"

# DataContextプロパティ値をアサート
snoopwpfcli assert --pid 12345 --type MyApp.MainWindow --hash 99999 \
    --property HasUnsavedChanges --expected true
```

## 出力

JSON結果（成功・エラーとも）はすべて **stdout** に出力されます。終了コードで成功（`0`）または失敗（非ゼロ）を判定してください。**stderr** は診断出力（`--verbose`）専用です。

これにより、結果に関わらず常にstdoutをパースできます:

```bash
snoopwpfcli find-element --pid 12345 --name MyButton | jq '.matchCount'
```

## 終了コード

| コード | 意味 |
|-------|------|
| 0 | 成功 |
| 1 | 一般エラー |
| 2 | プロセス未発見 |
| 3 | インジェクション失敗 |
| 4 | タイムアウト |

## アーキテクチャ

SnoopWpfCLI は **DLLインジェクション** と **Named Pipes** を使用して対象のWPFプロセスと通信します。

```
                          Named Pipe (IPC)
  +-----------+          +------------------+          +------------------+
  |           |  inject  |                  |  query/  |                  |
  |  CLI App  | -------> | WpfInspector.dll | <------> |  対象WPFアプリ    |
  |           |          | (インジェクトDLL)  |  respond |  (例: TestApp)   |
  +-----------+          +------------------+          +------------------+
       |
       v
  System.CommandLine         Snoop.InjectorLauncher
  (CLIフレームワーク)         (snoopwpfサブモジュール)
```

1. CLI は **Snoop.InjectorLauncher**（snoopwpf サブモジュール）を使用して、対象プロセスに `WpfInspector.dll` をインジェクションします。
2. インジェクションされたDLLは、対象プロセス内で **Named Pipe サーバー** を起動します。
3. CLI はNamed Pipes経由でインジェクトされたDLLと通信し、ビジュアルツリーの取得、アクションの実行、スクリーンショットの取得を行います。

## 開発

### ビルド

```bash
dotnet build src/SnoopWpfCLI.slnx
```

### 実行

```bash
dotnet run --project src/App/App.csproj --framework net10.0-windows -- <command> [options]
```

### テスト実行

```bash
dotnet test src/App.Tests/App.Tests.csproj
```

### プロジェクト構成

```
SnoopWpfCLI/
├── snoopwpf/                    # Gitサブモジュール（SnoopWPFインジェクター）
├── src/
│   ├── App/                     # CLIアプリケーション
│   │   ├── Commands/            # サブコマンド定義（System.CommandLine）
│   │   ├── Services/            # InjectionService, WpfProcessService
│   │   ├── Models/              # データモデル
│   │   └── Formatters/          # 出力フォーマッター（JSON / ツリー）
│   ├── WpfInspector/            # インジェクトDLL（ビジュアルツリー検査）
│   └── App.Tests/               # ユニットテスト（xUnit）
├── tests/
│   └── TestApp/                 # テスト用WPFアプリ
└── docs/
    ├── plans/                   # 設計ドキュメント
    ├── specs/                   # 仕様書
    └── references/              # 参考資料
```

## ライセンス

MIT

## 謝辞

- [SnoopWPF](https://github.com/snoopwpf/snoopwpf) -- DLLインジェクション基盤を提供するWPF検査ツール
- [SnoopWpfMcp](https://github.com/aoyagi/SnoopWpfMcp) -- 本CLIの元となったMCPサーバー実装
