# FinishIds中間テーブル実装

## 概要
ProcessDetailテーブルのFinishIdsフィールド（カンマ区切りテキスト）を、正規化された中間テーブル（ProcessDetailFinish）に移行しました。

## 変更内容

### 新規作成ファイル
- `Models/ProcessDetailFinish.cs` - 中間テーブルのモデル
- `Models/ProcessDetailConnectionExtensions.cs` - 拡張メソッド（StartIds関連）
- `Views/InverseBooleanConverter.cs` - XAML用のブール値反転コンバーター

### 更新ファイル

#### データベース関連
- `Services/Access/IAccessRepository.cs` - FinishIds用メソッドを追加
- `Services/Access/AccessRepository.cs` - FinishIds用メソッドの実装

#### モデル
- `Models/ProcessDetail.cs` - FinishIdsプロパティを削除

#### ビジネスロジック
- `Utils/ProcessDetail/BuildDetailFunctions.cs` - FinishDevices()メソッドを中間テーブル使用に更新
- `Utils/ProcessDetail/DetailUnitBuilder.cs` - 中間テーブルから終了工程を取得するように更新

#### UI
- `ViewModels/ProcessFlowViewModel.cs` - FinishIds関連のプロパティを削除、移行機能を削除
- `Views/ProcessFlowView.xaml` - 移行ボタンを削除

## テーブル構造

### ProcessDetailFinish
```sql
CREATE TABLE ProcessDetailFinish (
    Id COUNTER PRIMARY KEY,
    ProcessDetailId INTEGER NOT NULL,
    FinishProcessDetailId INTEGER NOT NULL,
    CONSTRAINT FK_ProcessDetailFinish_ProcessDetail 
        FOREIGN KEY (ProcessDetailId) REFERENCES ProcessDetail(Id),
    CONSTRAINT FK_ProcessDetailFinish_FinishProcessDetail 
        FOREIGN KEY (FinishProcessDetailId) REFERENCES ProcessDetail(Id)
)
```

## 主な変更点

1. **データアクセス層**
   - 新しいリポジトリメソッドで中間テーブルの操作をサポート
   - OleDbの制約に対応（匿名タプル不可、パラメータは`?`使用）

2. **ビジネスロジック層**
   - FinishIdsの文字列分割処理を削除
   - リポジトリから直接関係データを取得

3. **UI層**
   - 移行完了により、移行関連の機能をすべて削除
   - クリーンなコードベースを維持

## メリット
- データの正規化による整合性向上
- 外部キー制約による参照整合性の保証
- クエリパフォーマンスの向上
- メンテナンス性の向上