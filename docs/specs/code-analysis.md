# SnoopWpfMcp コード分析レポート

## 分析対象

ソースコード: `C:\Users\aoyagi\Documents\Prj\SnoopWpfMcp\MCP\`

---

## 1. WpfInspector/Inspector.cs（1402行）

### 概要
ターゲットWPFプロセスにインジェクトされるDLLのメインクラス。Named Pipeサーバーを起動し、JSON形式のコマンドを受信・処理する。

### MCPサーバー固有の部分（削除対象）
- なし。このファイルはMCPサーバーとは独立しており、Named Pipe通信のみ。

### CLI化で変更が必要な部分
- **名前空間**: `WpfInspector` → そのまま利用可能（独立したDLLプロジェクトのため）
- **ターゲットフレームワーク**: `net8.0-windows` → `net10.0-windows` に変更

### そのまま利用可能な部分
- 全体がほぼそのまま利用可能

### 主要メソッドの構造

| メソッド | 行 | 役割 | CLI移植 |
|---------|-----|------|---------|
| `Initialize(string)` | 40-61 | エントリポイント、Named Pipeサーバー起動 | そのまま |
| `StartPipeServerAsync` | 63-92 | パイプ接続待ち受けループ | そのまま |
| `HandleClientAsync` | 94-126 | コマンド受信・応答 | そのまま |
| `ProcessMessage` | 128-160 | PING/STATUS/EXIT/JSONコマンド振り分け | そのまま |
| `ProcessJsonCommand` | 162-204 | JSONコマンドパース、コマンド別処理呼び出し | そのまま |
| `ProcessGetVisualTreeCommand` | 529-605 | ビジュアルツリー全体取得 | そのまま |
| `ProcessGetElementByHashcodeCommand` | 607-716 | 単一要素取得 | そのまま |
| `ProcessGetVisualTreeByHashcodeCommand` | 718-814 | サブツリー取得 | そのまま |
| `ProcessAutomationPeerCommand` | 207-269 | AutomationPeerコマンド実行 | そのまま |
| `ProcessTakeScreenshotCommand` | 271-336 | スクリーンショット取得 | そのまま |
| `GetAllWpfControls` | 398-434 | WPFコントロール検索（WPF Window + WinForms hosted） | そのまま |
| `GetAllWpfControlsHostedInWinforms` | 441-515 | WinFormsホスト内のWPFコントロール検索 | そのまま |
| `CreateVisualTreeNode` | 875-918 | 再帰的ビジュアルツリーノード生成 | そのまま |
| `CreateVisualTreeNodeWithoutChildren` | 920-982 | 子なしノード生成（DependencyProperty + AutomationPeer） | そのまま |
| `GetChildren` | 1001-1029 | 子要素列挙（LogicalTree + ItemsControl） | そのまま |
| `GetValue` | 1123-1185 | DependencyProperty値取得（Binding検出含む） | そのまま |
| `GetBindingInfo` | 1187-1256 | Binding情報構築 | そのまま |
| `SerializePropertyValue` | 1369-1399 | 値のJSON用シリアライズ | そのまま |

### 潜在的な問題点

#### テンプレートでビジュアルツリーが途切れる問題
- **原因箇所**: `GetChildren` メソッド（1001-1029行）
- **問題**: `LogicalTreeHelper.GetChildren` はLogical Treeを走査するため、ControlTemplateやDataTemplateの内部要素（Visual Treeに存在するがLogical Treeには存在しない要素）を取得できない
- **具体例**: `ContentPresenter`やカスタム`ControlTemplate`内の`Border`, `Grid`等はVisualTreeHelperでしか到達できない
- **GetContentControlChild** (816-873行): ContentControlの場合にContentPresenter配下への探索を試みるが、これは`ProcessGetVisualTreeByHashcodeCommand`内でのみ使用され、通常の`GetChildren`/`CreateVisualTreeNode`では呼ばれない
- **改善案**: `GetChildren`にVisualTreeHelperフォールバックを追加し、LogicalTreeで子が見つからない場合にVisualTree走査を行う

#### Named Pipe通信のバッファサイズ
- `HandleClientAsync`（98行）: 固定1024バイトバッファ。大きなコマンドが切り捨てられる可能性
- Inspector側が受信するのは比較的小さなコマンドJSONなので実用上の問題は少ない

#### P/Invoke宣言
- `EnumWindows`, `EnumChildWindows`, `GetWindowThreadProcessId`（518-527行）: WinFormsホスト検出用
- CLI移植時もそのまま必要

---

## 2. WpfInspector/AutomationPeerHandler.cs（653行）

### 概要
UI Automationパターン（Invoke, Value, Toggle, ExpandCollapse, SelectionItem, RangeValue, Scroll）の操作を提供するstaticクラス。

### MCPサーバー固有の部分
- なし

### CLI化で変更が必要な部分
- なし。完全にそのまま利用可能。

### そのまま利用可能な部分
- 全体

### 主要構造

| クラス/メソッド | 行 | 役割 |
|---------------|-----|------|
| `AutomationActions` (定数クラス) | 18-52 | アクション名の定数定義 |
| `GetAutomationPeerInfo` | 62-219 | 要素のAutomationPeer情報取得 |
| `ExecuteInvokeAutomationPeerCommand` | 224-257 | アクション実行のディスパッチ |
| `ExecuteAction` | 259-298 | アクション名→実行メソッドのswitch |
| 各`Execute*Action` | 301-651 | パターン別のアクション実行 |

### 依存
- `System.Text.Json.JsonElement`（commandDataの型）
- `VariousExtensions.GetDoubleOrStringAsDouble`（569行）

---

## 3. WpfInspector/DataContextTracker.cs（184行）

### 概要
DataContextオブジェクトの一意ID管理とリフレクションベースのプロパティ情報収集。

### MCPサーバー固有の部分
- なし

### CLI化で変更が必要な部分
- なし

### そのまま利用可能な部分
- 全体

### 主要構造

| メソッド | 行 | 役割 |
|---------|-----|------|
| `RegisterDataContext` | 17-45 | DataContextのID登録（ハッシュコードベース） |
| `GetDataContexts` | 50-62 | 登録済みDataContext一覧取得 |
| `CreateDataContextInfo` | 64-153 | リフレクションによるプロパティ情報構築 |
| `SerializeDataContextPropertyValue` | 155-182 | プロパティ値のシリアライズ |

---

## 4. WpfInspector/DependencyPropertyCache.cs（95行）

### 概要
DependencyPropertyの型別キャッシュ。継承階層を走査し、各型のDependencyPropertyを収集。

### MCPサーバー固有の部分
- なし

### CLI化で変更が必要な部分
- なし

### そのまま利用可能な部分
- 全体

---

## 5. WpfInspector/VariousExtensions.cs（18行）

### 概要
`JsonElement`の`GetDoubleOrStringAsDouble`拡張メソッド。

### 変更
- なし。そのまま利用可能。

---

## 6. Services/InjectionService.cs（1147行）

### 概要
DLLインジェクションとNamed Pipe通信を管理するサービス。Snoop.InjectorLauncherプロセスを起動してWpfInspector.dllをターゲットプロセスに注入し、Named Pipeでコマンドを送受信する。

### MCPサーバー固有の部分（削除対象）
- `ILogger<InjectionService>` への依存: Microsoft.Extensions.Logging → CLI用に簡素化するか、DIで注入
- `IWpfProcessService` への依存: 間接的に使用（注入確認用のping等）

### CLI化で変更が必要な部分

| 項目 | 現状 | CLI化 |
|-----|------|-------|
| 名前空間 | `SnoopWpfMcpServer.Services` | `SnoopWpfCLI.Services` |
| ロガー | `ILogger<InjectionService>` | そのまま使用可能（Microsoft.Extensions.LoggingはCLIでも使える） |
| モデル参照 | `SnoopWpfMcpServer.Models` | `SnoopWpfCLI.Models` |

### そのまま利用可能な部分（ロジック）

| メソッド | 行 | 役割 | 移植方針 |
|---------|-----|------|---------|
| `PingAsync` | 49-154 | インジェクション＋通信確認 | そのまま |
| `InvokeAutomationPeerAsync` | 177-323 | AutomationPeerアクション実行 | そのまま |
| `TakeScreenshotAsync` | 325-432 | スクリーンショット取得 | そのまま |
| `GetVisualTreeAsync` | 434-516 | ビジュアルツリー全体取得 | そのまま |
| `GetElementByHashcodeAsync` | 518-644 | 単一要素取得 | そのまま |
| `GetVisualTreeByHashcodeAsync` | 646-722 | サブツリー取得 | そのまま |
| `InjectWpfInspectorAsync` | 737-812 | Snoop.InjectorLauncher実行 | そのまま |
| `SendPingAsync` | 814-863 | Named Pipe経由のPING送信 | そのまま |
| `SendVisualTreeCommandAsync` | 936-1017 | Named Pipe経由のGET_VISUAL_TREE | そのまま |
| `SendRunCommandAsync` | 1084-1132 | Named Pipe経由の汎用コマンド送信 | そのまま |
| `IsCompleteJson` | 1134-1145 | JSON完全性チェック | そのまま |

### 重要な実装詳細

#### インジェクション手順（`InjectWpfInspectorAsync` 737-812行）
1. 実行ディレクトリから `WpfInspector.dll` のパスを取得
2. `Snoop.InjectorLauncher.x64.exe` のパスを取得
3. 引数: `--targetPID {pid} --assembly "{dllPath}" --className "WpfInspector.Inspector" --methodName "Initialize"`
4. プロセスを起動して終了コードで成功/失敗を判定

#### Named Pipe通信プロトコル
- パイプ名: `WpfInspector_{processId}`
- 文字列コマンド: `PING`, `STATUS`, `EXIT`
- JSONコマンド: `{ "commandType": "GET_VISUAL_TREE" }` など
- インジェクション後2秒待機してからPING（115行, 371行等）

#### バッファ管理
- PING応答: 1024バイト固定バッファ（841行）
- スクリーンショット: 1MBバッファ + チャンク読み取り（896-912行）
- ビジュアルツリー: 8KBバッファ + JSON完全性チェックによるチャンク読み取り + 10MB上限（968-995行）
- 汎用コマンド: 4KBバッファ（1109行）

---

## 7. Services/WpfProcessService.cs（268行）

### 概要
WPFプロセスの検出サービス。2段階チェック（ウィンドウクラス名パターン、WPFグラフィックスモジュール）でWPFプロセスを特定する。

### MCPサーバー固有の部分（削除対象）
- なし（ロガー依存はCLIでも利用可能）

### CLI化で変更が必要な部分

| 項目 | 現状 | CLI化 |
|-----|------|-------|
| 名前空間 | `SnoopWpfMcpServer.Services` | `SnoopWpfCLI.Services` |
| モデル参照 | `SnoopWpfMcpServer.Models` | `SnoopWpfCLI.Models` |
| `System.Management` 依存 | WMI経由でWorkingDirectory取得 | NuGetパッケージ `System.Management` が必要 |

### そのまま利用可能な部分（ロジック）

| メソッド | 行 | 役割 | 移植方針 |
|---------|-----|------|---------|
| `GetWpfProcessesAsync` | 40-83 | 全プロセス走査、WPF判定 | そのまま |
| `IsWpfProcess` | 85-124 | WPFプロセス判定（2段階チェック） | そのまま |
| `CheckForWpfWindowClasses` | 126-146 | HwndWrapperパターンチェック | そのまま |
| `CheckForWpfGraphicsModules` | 148-176 | wpfgfx_* DLLチェック | そのまま |
| `CreateProcessInfoAsync` | 201-222 | プロセス情報構築 | そのまま |

### P/Invoke
- `GetClassName` (198-199行): ウィンドウクラス名取得

---

## 8. Models/（6ファイル）

### 概要
すべてシンプルなPOCOクラス。`System.Text.Json.Serialization.JsonPropertyName`属性付き。

| ファイル | 行数 | 役割 | 移植方針 |
|---------|------|------|---------|
| `WpfProcessInfo.cs` | 33 | プロセス情報 | 名前空間変更のみ |
| `InjectionResult.cs` | 26 | インジェクション結果 | 名前空間変更のみ |
| `VisualTreeResult.cs` | 32 | ビジュアルツリー取得結果 | 名前空間変更のみ |
| `ElementResult.cs` | 34 | 要素取得結果 | 名前空間変更のみ |
| `AutomationPeerResult.cs` | 33 | AutomationPeerアクション結果 | 名前空間変更のみ |
| `ScreenshotResult.cs` | 38 | スクリーンショット結果 | 名前空間変更のみ |

---

## 9. 削除対象ファイル（MCPサーバー固有）

| ファイル | 理由 |
|---------|------|
| `Services/McpServer.cs` | JSON-RPC stdin/stdout MCPサーバー |
| `Services/HttpMcpController.cs` | ASP.NET Core HTTPコントローラー |
| `WpfInspectorPlugin.cs` | SemanticKernel KernelFunction定義 → CLI Commandsに置換 |
| `Startup.cs` | ASP.NET Core Startup |
| `Program.cs` | エントリポイント → CLI用に完全書き換え |

---

## 10. 外部依存関係の整理

### WpfInspectorプロジェクト（インジェクトDLL）
- `net10.0-windows` + `UseWPF=true`
- NuGetパッケージ不要（.NET標準ライブラリのみ）

### CLIアプリケーションプロジェクト
- `net10.0-windows`
- NuGet: `System.CommandLine`, `System.Text.Json`, `System.Management`, `Microsoft.Extensions.Logging`, `Microsoft.Extensions.Logging.Console`
- プロジェクト参照: WpfInspector（ビルドしてDLLを出力にコピー）
- snoopwpfサブモジュール: `Snoop.InjectorLauncher` プロジェクト参照 + ビルド後ファイルコピー

### snoopwpfサブモジュールから必要なファイル
- `Snoop.InjectorLauncher.x64.exe` - インジェクターランチャー
- `Snoop.GenericInjector.x64.dll` - 64bitインジェクター
- `Snoop.GenericInjector.x86.dll` - 32bitインジェクター
- `Shared/InjectorData.cs` - インジェクターデータ構造（参照のみ）

---

## 11. CLI Commands → MCPツール マッピング

| CLI コマンド | 対応MCPツール | 主要ロジック元 |
|-------------|-------------|--------------|
| `list-processes` | `get_wpf_processes` | `WpfProcessService.GetWpfProcessesAsync` + `WpfInspectorPlugin.IsInterestingProcess` フィルタリング |
| `ping` | `ping` | `InjectionService.PingAsync` |
| `get-tree` | `get_visual_tree` | `InjectionService.GetVisualTreeAsync` |
| `get-subtree` | `get_visual_tree_by_hashcode` | `InjectionService.GetVisualTreeByHashcodeAsync` |
| `get-element` | `get_element_by_hashcode` | `InjectionService.GetElementByHashcodeAsync` |
| `invoke` | `invoke_automation_peer` | `InjectionService.InvokeAutomationPeerAsync` |
| `screenshot` | `take_wpf_screenshot` | `InjectionService.TakeScreenshotAsync` |

### 各CLIコマンドの実装方針

1. **list-processes**: `WpfProcessService`を直接呼び出し、`IsInterestingProcess`フィルタをそのまま移植
2. **ping**: `InjectionService.PingAsync`を呼び出し、結果をJSON出力
3. **get-tree**: `InjectionService.GetVisualTreeAsync`を呼び出し、`--format tree`でツリー表示
4. **get-subtree**: `InjectionService.GetVisualTreeByHashcodeAsync`を呼び出し
5. **get-element**: `InjectionService.GetElementByHashcodeAsync`を呼び出し
6. **invoke**: `InjectionService.InvokeAutomationPeerAsync`を呼び出し
7. **screenshot**: `InjectionService.TakeScreenshotAsync`を呼び出し、`--output`でファイル保存

---

## 12. テンプレートでビジュアルツリーが途切れる問題の詳細分析

### 問題の根本原因

`Inspector.cs` の `GetChildren` メソッド（1001-1029行）:

```csharp
private static IEnumerable<DependencyObject> GetChildren(DependencyObject element)
{
    // Get children from LogicalTreeHelper
    foreach (object logicalChild in LogicalTreeHelper.GetChildren(element))
    {
        if (logicalChild is DependencyObject depObj)
        {
            yield return depObj;
        }
    }

    if (element is ItemsControl itemsControl)
    {
        foreach (var item in itemsControl.Items)
        {
            if (item is DependencyObject) continue;
            var container = itemsControl.ItemContainerGenerator.ContainerFromItem(item);
            if (container is not null) yield return container;
        }
    }
}
```

**LogicalTreeHelper.GetChildren** はLogical Treeのみを走査する。WPFでは:
- Logical Tree: XAML構造（開発者が定義したツリー）
- Visual Tree: 実際の描画要素（テンプレートが展開された後のツリー）

ControlTemplateやDataTemplateの内部要素はVisual Treeには存在するが、Logical Treeには存在しない。そのため:
- `Button`の内部にある`Border > ContentPresenter > TextBlock`がツリーに表示されない
- カスタム`ControlTemplate`内の要素が全て欠落する
- `DataTemplate`で定義されたUIがテンプレート適用後に見えない

### 部分的な対応（既存コード）

`GetContentControlChild` (816-873行) はContentControlに限定した対応:
- `ProcessGetVisualTreeByHashcodeCommand`でのみ呼び出される
- ContentPresenterを探索して内部の要素を返す
- しかし`CreateVisualTreeNode`の再帰走査では利用されていない

### 改善案

`GetChildren`にVisualTreeHelperフォールバックを追加:

```csharp
private static IEnumerable<DependencyObject> GetChildren(DependencyObject element)
{
    var logicalChildren = new HashSet<DependencyObject>();

    // 1. Logical Tree子要素
    foreach (object logicalChild in LogicalTreeHelper.GetChildren(element))
    {
        if (logicalChild is DependencyObject depObj)
        {
            logicalChildren.Add(depObj);
            yield return depObj;
        }
    }

    // 2. ItemsControlのアイテムコンテナ
    if (element is ItemsControl itemsControl)
    {
        foreach (var item in itemsControl.Items)
        {
            if (item is DependencyObject) continue;
            var container = itemsControl.ItemContainerGenerator.ContainerFromItem(item);
            if (container is not null)
            {
                logicalChildren.Add(container);
                yield return container;
            }
        }
    }

    // 3. Visual Tree子要素（Logical Treeで見つからなかったもの）
    var visualChildCount = VisualTreeHelper.GetChildrenCount(element);
    for (int i = 0; i < visualChildCount; i++)
    {
        var visualChild = VisualTreeHelper.GetChild(element, i);
        if (visualChild is DependencyObject depObj && !logicalChildren.Contains(depObj))
        {
            yield return depObj;
        }
    }
}
```

**注意**: この改善はツリーのサイズを大幅に増加させる可能性がある。オプションフラグ（`--include-visual-tree`等）での制御が望ましい。

---

## 13. プロジェクト構成の推奨

```
SnoopWpfCLI/
├── snoopwpf/                       # git submodule
├── src/
│   ├── App/                        # CLIアプリ（net10.0-windows, Exe）
│   │   ├── Commands/               # System.CommandLine コマンド定義
│   │   │   ├── ListProcessesCommand.cs
│   │   │   ├── PingCommand.cs
│   │   │   ├── GetTreeCommand.cs
│   │   │   ├── GetSubtreeCommand.cs
│   │   │   ├── GetElementCommand.cs
│   │   │   ├── InvokeCommand.cs
│   │   │   └── ScreenshotCommand.cs
│   │   ├── Services/               # InjectionService, WpfProcessService
│   │   ├── Models/                 # データモデル
│   │   ├── Formatters/             # 出力フォーマッター
│   │   └── Program.cs              # エントリポイント
│   ├── WpfInspector/               # インジェクトDLL（net10.0-windows, Library）
│   │   ├── Inspector.cs
│   │   ├── AutomationPeerHandler.cs
│   │   ├── DataContextTracker.cs
│   │   ├── DependencyPropertyCache.cs
│   │   └── VariousExtensions.cs
│   └── App.Tests/                  # ユニットテスト
└── tests/
    └── TestApp/                    # テスト用WPFアプリ
```
