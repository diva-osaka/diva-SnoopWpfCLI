# CLAUDE.md

## プロジェクト構成

```
docs/
├── plans/          # Claude CodeのPlan関連ファイル
├── tasks/          # Claude CodeのTask関連ファイル
├── references /    # 参考資料など
└── specs/          # 仕様書・設計書

src/
├── App/                              # メインアプリケーション
└── App.Tests/                        # ユニットテスト
```

## 技術スタック

- **ターゲットOS**: Windows 10/11
- **フレームワーク**: .NET 10.0 + C#
- **テストフレームワーク**: XUnit

### ビルド・実行コマンド

```bash
# ビルド
dotnet build src\SnoopWpfCLI.slnx

# 実行
dotnet run --project src\App\App.csproj --framework net10.0-windows

# テスト実行（単体）
dotnet run --project src\App.Tests\App.Tests.csproj
```

## 開発時の重要な注意点

## 機能ブランチの作成（MUST）

新機能の開発を開始する際は、コード変更を行う前に必ず `feature/<機能名>` のパターンで機能ブランチを作成してください。

## TDD（テスト駆動開発）（MUST）

サービスやモデルの機能実装時は、まず、正常処理、エッジケース、エラー処理を網羅する包括的なテストを記述してください。その後、機能を一括で実装せず、変更ごとにテストを実行し、すべてがパスするまで反復的に進めてください。単一のユニットテストと全体のテストをサブタスクをもちいて並列化してください。テストが100%パスするまで停止しないでください。障害に遭遇した場合は、その内容と対処法の案を提示してください。

ビューの機能実装時も、可能であればTDDでおこなってください。UIコンポーネントなどユニットテストが難しいものに関しては無理にTDDにする必要はありません。必要であればユーザー（人間）にテストを依頼してください。

## テスト前の必須チェック（MUST）

ユニットテストを実行する前、もしくは、ユーザーによるテストを依頼する前にビルドを実行しエラーがないことを確認してださい。

## プッシュ前の必須チェック（MUST）

コードをプッシュする前にビルドおよびテストを実行しエラーがないことを確認してください。

- 個別のテストだけでなく、必ず全テストを実行する。
- 一つでも失敗したらプッシュしない。
- 修正したら再度全チェックを実行する。

## Workflow Orchestration

### 1. Plan Mode Default
- Enter plan mode for ANY non-trivial task (3+ steps or architectural decisions)
- If something goes sideways, STOP and re-plan immediately – don't keep pushing
- Use plan mode for verification steps, not just building
- Write detailed specs upfront to reduce ambiguity

### 2. Subagent Strategy
- Use subagents liberally to keep main context window clean
- Offload research, exploration, and parallel analysis to subagents
- For complex problems, throw more compute at it via subagents
- One task per subagent for focused execution

### 3. Self-Improvement Loop
- After ANY correction from the user: update `tasks/lessons.md` with the pattern
- Write rules for yourself that prevent the same mistake
- Ruthlessly iterate on these lessons until mistake rate drops
- Review lessons at session start for relevant project

### 4. Verification Before Done
- Never mark a task complete without proving it works
- Diff behavior between main and your changes when relevant
- Ask yourself: "Would a staff engineer approve this?"
- Run tests, check logs, demonstrate correctness

### 5. Demand Elegance (Balanced)
- For non-trivial changes: pause and ask "is there a more elegant way?"
- If a fix feels hacky: "Knowing everything I know now, implement the elegant solution"
- Skip this for simple, obvious fixes – don't over-engineer
- Challenge your own work before presenting it

### 6. Autonomous Bug Fixing
- When given a bug report: just fix it. Don't ask for hand-holding
- Point at logs, errors, failing tests – then resolve them
- Zero context switching required from the user
- Go fix failing CI tests without being told how

## Task Management

1. **Plan First**: Write plan to `tasks/todo.md` with checkable items
2. **Verify Plan**: Check in before starting implementation
3. **Track Progress**: Mark items complete as you go
4. **Explain Changes**: High-level summary at each step
5. **Document Results**: Add review section to `tasks/todo.md`
6. **Capture Lessons**: Update `tasks/lessons.md` after corrections

## Core Principles

- **Simplicity First**: Make every change as simple as possible. Impact minimal code.
- **No Laziness**: Find root causes. No temporary fixes. Senior developer standards.
- **Minimal Impact**: Changes should only touch what's necessary. Avoid introducing bugs.
