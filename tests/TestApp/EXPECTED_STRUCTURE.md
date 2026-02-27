# TestApp - 期待されるビジュアルツリー構造

## 概要

このドキュメントはTestAppの既知のUI構造を記述する。
統合テストでCLIのget-treeコマンド出力と照合する際の参照資料。

## UI要素一覧

### Tab 1: Basic Controls

| x:Name | Type | DataBinding | AutomationPattern |
|--------|------|-------------|-------------------|
| HeaderTitle | TextBlock | - | - |
| ProcessInfoText | TextBlock | - | - |
| InputTextBox | TextBox | `{Binding InputText}` | Value (Get/Set) |
| MirrorTextBox | TextBox | `{Binding InputText, Mode=OneWay}` | Value (Get) |
| CountButton | Button | - | Invoke |
| CustomStyledButton | Button (ControlTemplate) | - | Invoke |
| ClickCountText | TextBlock | `{Binding ClickCount}` | - |
| BoundCheckBox | CheckBox | `{Binding IsChecked}` | Toggle |
| ThreeStateCheckBox | CheckBox (IsThreeState) | - | Toggle |
| TestToggleButton | ToggleButton | - | Toggle |
| TestSlider | Slider | `{Binding SliderValue}` | RangeValue |
| TestProgressBar | ProgressBar | `{Binding SliderValue}` | RangeValue |

### Tab 2: Selection Controls

| x:Name | Type | DataBinding | AutomationPattern |
|--------|------|-------------|-------------------|
| BoundComboBox | ComboBox | `ItemsSource={Binding Categories}` | ExpandCollapse, Selection |
| StaticComboBox | ComboBox (static items) | - | ExpandCollapse, Selection |
| PeopleListBox | ListBox | `ItemsSource={Binding People}`, `ItemTemplate=PersonItemTemplate` | Selection |
| TodoListBox | ListBox | `ItemsSource={Binding TodoItems}`, `ItemTemplate=TodoItemTemplate` | Selection |

### Tab 3: Nested Structure

| x:Name | Type | 入れ子の深さ | AutomationPattern |
|--------|------|------------|-------------------|
| NestedButton1 | Button | Border > DockPanel > StackPanel > Border > StackPanel > Button | Invoke |
| NestedTextBox1 | TextBox | 同上 > TextBox | Value |
| WrapButton1-3 | Button | Border > WrapPanel > Button | Invoke |
| WrapCheckBox | CheckBox | Border > WrapPanel > CheckBox | Toggle |
| TestTreeView | TreeView | GroupBox > TreeView > TreeViewItem (3層) | ExpandCollapse |

### Tab 4: Template Test

| x:Name | Type | テスト目的 | AutomationPattern |
|--------|------|----------|-------------------|
| PersonContentControl | ContentControl | DataTemplate境界テスト | - |
| TemplateTestButton | Button (ControlTemplate) | ControlTemplate境界テスト | Invoke |
| TestExpander | Expander | 内部ControlTemplateテスト | ExpandCollapse |
| ExpanderButton | Button | Expander内要素 | Invoke |
| ExpanderTextBox | TextBox | Expander内要素 | Value |
| InnerTabControl | TabControl | ネストされたテンプレート | Selection |
| InnerTabButton | Button | 内側TabControl内要素 | Invoke |

## ViewModel構造 (MainViewModel)

### Properties

| Property | Type | Initial Value |
|----------|------|---------------|
| InputText | string | "Initial Text" |
| IsChecked | bool | false |
| SelectedCategory | string | "" |
| SliderValue | double | 50.0 |
| ClickCount | int | 0 |
| StatusMessage | string | "Ready" |

### Collections

| Property | Type | Items |
|----------|------|-------|
| Categories | ObservableCollection&lt;string&gt; | "Category A", "Category B", "Category C" |
| People | ObservableCollection&lt;PersonItem&gt; | Alice/30/Developer, Bob/25/Designer, Carol/35/Manager |
| TodoItems | ObservableCollection&lt;TodoItem&gt; | "Write unit tests" (done), "Review pull request", "Deploy to staging" |

## テンプレート境界テスト

### LogicalTree vs VisualTree の差異

以下の箇所で、LogicalTreeHelper.GetChildren ではVisual Tree要素を取得できない:

1. **CustomStyledButton / TemplateTestButton** (CustomButtonStyle):
   - Logical Tree: Button のみ
   - Visual Tree: Button > Border(CustomBorder) > ContentPresenter > TextBlock

2. **PeopleListBox** (PersonItemTemplate):
   - Logical Tree: ListBox > ListBoxItem(s) のみ
   - Visual Tree: ListBoxItem > ContentPresenter > Border > Grid > TextBlock x3

3. **TodoListBox** (TodoItemTemplate):
   - Logical Tree: ListBox > ListBoxItem(s) のみ
   - Visual Tree: ListBoxItem > ContentPresenter > StackPanel > CheckBox + TextBlock

4. **PersonContentControl** (ContentControl + DataTemplate):
   - Logical Tree: ContentControl のみ
   - Visual Tree: ContentControl > ContentPresenter > Border > Grid > TextBlock x3

5. **TestExpander** (内部ControlTemplate):
   - Logical Tree: Expander > StackPanel > 子要素
   - Visual Tree: Expander > Border > DockPanel > ToggleButton(Header) + ContentPresenter > ...

## 入れ子構造テスト

最も深い入れ子パス:
```
Window > Grid > TabControl > TabItem(NestedTab) > ScrollViewer > Grid > StackPanel
  > Border > DockPanel > StackPanel > Border > StackPanel > Button(NestedButton1)
```
深さ: 約12レベル
