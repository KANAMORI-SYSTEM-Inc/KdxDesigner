# Process テーブル複数値フィールドの移行ドキュメント

## 概要
Microsoft Access独自の複数値フィールドを標準的な中間テーブル構造に移行し、他のデータベースシステムへの移行を可能にします。

## 移行対象フィールド

### 1. Process.AutoCondition
- **現状**: セミコロン区切りの文字列で複数のProcessDetailIdを格納
- **例**: "1;2;3;4;5"
- **新テーブル**: ProcessStartCondition

### 2. Process.FinishId  
- **現状**: 単一の整数値（将来的に複数値対応予定）
- **新テーブル**: ProcessFinishCondition

## 新しいテーブル構造

### ProcessStartCondition テーブル
```sql
CREATE TABLE ProcessStartCondition (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProcessId INTEGER NOT NULL,
    StartProcessDetailId INTEGER NOT NULL,
    StartSensor TEXT,
    FOREIGN KEY (ProcessId) REFERENCES Process(Id),
    FOREIGN KEY (StartProcessDetailId) REFERENCES ProcessDetail(Id)
);
```

### ProcessFinishCondition テーブル
```sql
CREATE TABLE ProcessFinishCondition (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProcessId INTEGER NOT NULL,
    FinishProcessDetailId INTEGER NOT NULL,
    FinishSensor TEXT,
    FOREIGN KEY (ProcessId) REFERENCES Process(Id),
    FOREIGN KEY (FinishProcessDetailId) REFERENCES ProcessDetail(Id)
);
```

## 実装ファイル

### 新規作成ファイル
1. **Models/ProcessStartCondition.cs**
   - 工程開始条件のモデルクラス

2. **Models/ProcessFinishCondition.cs**
   - 工程終了条件のモデルクラス

3. **Utils/Migration/ProcessConditionMigration.cs**
   - データ移行ユーティリティクラス
   - MigrateAutoConditions(): AutoConditionフィールドの移行
   - MigrateFinishIds(): FinishIdフィールドの移行
   - CheckMigrationStatus(): 移行状態の確認

4. **Utils/Process/BuildProcessUpdated.cs**
   - 新しい中間テーブルを使用するBuildProcessクラス
   - GetStartConditions(): 開始条件を中間テーブルから取得
   - GetFinishConditions(): 終了条件を中間テーブルから取得
   - 互換性のため、古いフィールドもフォールバックとして参照

### 更新ファイル
1. **Services/Access/IAccessRepository.cs**
   - ProcessStartCondition用メソッド追加
   - ProcessFinishCondition用メソッド追加

2. **Services/Access/AccessRepository.cs**
   - 各メソッドの実装追加
   - テーブル不在時のエラーハンドリング

## 移行手順

### 1. Accessデータベースの準備
```sql
-- ProcessStartConditionテーブルの作成
CREATE TABLE ProcessStartCondition (
    Id AUTOINCREMENT PRIMARY KEY,
    ProcessId LONG NOT NULL,
    StartProcessDetailId LONG NOT NULL,
    StartSensor TEXT(255)
);

-- ProcessFinishConditionテーブルの作成
CREATE TABLE ProcessFinishCondition (
    Id AUTOINCREMENT PRIMARY KEY,
    ProcessId LONG NOT NULL,
    FinishProcessDetailId LONG NOT NULL,
    FinishSensor TEXT(255)
);
```

### 2. データ移行の実行
```csharp
// MainViewModelまたは管理画面で実行
var migration = new ProcessConditionMigration(_repository);

// 移行状態の確認
var status = migration.CheckMigrationStatus();
MessageBox.Show(status.GetStatusMessage());

// 移行の実行
migration.MigrateAll();
```

### 3. アプリケーションコードの更新
BuildProcess.csの各メソッドをBuildProcessUpdated.csの新しいメソッドに置き換え：
- `BuildNormal` → `BuildNormalUpdated`
- `BuildSubProcess` → `BuildSubProcessUpdated`
- `BuildCondition` → `BuildConditionUpdated`
- `BuildConditionStart` → `BuildConditionStartUpdated`

### 4. 古いフィールドの削除（オプション）
移行が完全に完了し、動作確認後：
```sql
-- Process テーブルから古いフィールドを削除
ALTER TABLE Process DROP COLUMN AutoCondition;
ALTER TABLE Process DROP COLUMN FinishId;
```

## 互換性

新しい実装は以下の順序でデータを参照します：
1. 新しい中間テーブル（ProcessStartCondition/ProcessFinishCondition）
2. 古いフィールド（AutoCondition/FinishId）- フォールバック

これにより、段階的な移行が可能です。

## メリット

1. **データベース非依存**: ACCESS独自機能を使わない標準的な構造
2. **拡張性**: 条件ごとにセンサー情報など追加データを保持可能
3. **パフォーマンス**: インデックスを適切に設定可能
4. **保守性**: 標準的なSQL操作で管理可能
5. **移行性**: PostgreSQL、MySQL、SQL Serverなどへの移行が容易

## 注意事項

1. 移行前に必ずデータベースのバックアップを取得してください
2. 移行は段階的に実行可能（AutoConditionとFinishIdを別々に移行）
3. 大量データの場合、移行には時間がかかる可能性があります
4. 移行後は必ず動作確認を実施してください

## トラブルシューティング

### テーブルが作成できない
- Accessの権限を確認
- テーブル名の重複を確認

### 移行中にエラーが発生
- ProcessDetailIdが存在することを確認
- AutoConditionフィールドの形式を確認（セミコロン区切り）

### アプリケーションが動作しない
- BuildProcessUpdated.csが正しく参照されているか確認
- リポジトリメソッドが実装されているか確認