# MnemonicTimerDeviceテーブルの複合キー化とProcessDetail.StartTimer追加

## 概要
MnemonicTimerDeviceテーブルを単一のIDキーから(MnemonicId, RecordId, TimerId)の複合プライマリキーに変更し、ProcessDetailテーブルにStartTimer列を追加しました。これにより、各レコードに対して複数のタイマーを管理できるようになり、データの整合性が向上します。

## 変更内容

### 新規作成ファイル
- `docs/migration-mnemonic-timer-composite-key.sql` - データベースマイグレーションスクリプト
- `docs/wiki-mnemonic-timer-composite-key.md` - この要約ドキュメント

### 更新ファイル
- `Models/ProcessDetail.cs` - StartTimerプロパティを追加
- `Models/MnemonicTimerDevice.cs` - 複合キー対応（IDプロパティを削除、3つのキープロパティを設定）
- `Services/MnemonicTimerDeviceService.cs` - 複合キーに対応したCRUD操作の更新

## 主な変更点

1. **ProcessDetailテーブルの拡張**
   - StartTimer列（INTEGER型）を追加
   - Timerテーブルとの関連を管理

2. **MnemonicTimerDeviceテーブルの複合キー化**
   - プライマリキー: (MnemonicId, RecordId, TimerId)
   - TimerIdをNOT NULLに変更
   - 検索性能向上のためのインデックスを追加

3. **サービスクラスの更新**
   - UPDATE文のWHERE句を複合キー対応に変更
   - 辞書のキーを3つのタプルに変更
   - GetMnemonicTimerDeviceByTimerIdメソッドの戻り値をリストに変更

## テーブル構造

### ProcessDetail（更新後）
```sql
CREATE TABLE ProcessDetail (
    Id INTEGER PRIMARY KEY,
    ProcessId INTEGER,
    OperationId INTEGER,
    DetailName TEXT,
    StartSensor TEXT,
    CategoryId INTEGER,
    FinishSensor TEXT,
    BlockNumber INTEGER,
    SkipMode TEXT,
    CycleId INTEGER,
    SortNumber INTEGER,
    Comment TEXT,
    ILStart TEXT,
    StartTimer INTEGER  -- 新規追加
)
```

### MnemonicTimerDevice（更新後）
```sql
CREATE TABLE MnemonicTimerDevice (
    MnemonicId INTEGER NOT NULL,
    RecordId INTEGER NOT NULL,
    TimerId INTEGER NOT NULL,
    TimerCategoryId INTEGER,
    ProcessTimerDevice TEXT,
    TimerDevice TEXT,
    PlcId INTEGER NOT NULL,
    CycleId INTEGER,
    Comment1 TEXT,
    Comment2 TEXT,
    Comment3 TEXT,
    PRIMARY KEY (MnemonicId, RecordId, TimerId)
)
```

## メリット
- データの一意性が保証される（同じMnemonic/Record/Timerの組み合わせの重複を防ぐ）
- 複合キーによる検索性能の向上
- ProcessDetailとTimerの関連が明確になる
- 将来的な拡張性の向上