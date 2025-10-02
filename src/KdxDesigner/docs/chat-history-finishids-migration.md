# FinishIds中間テーブル実装 チャット履歴

## 日付: 2025-08-01

## 概要
ProcessDetailのFinishIdsフィールドを中間テーブル（ProcessDetailFinish）に移行する作業を実施。

## 作業内容

### 1. 初期エラー対応
- **エラー**: `'ProcessFlowNode' 型の修飾子をとおしてプロテクト メンバー 'ObservableObject.OnPropertyChanged(PropertyChangedEventArgs)' にアクセスすることはできません`
- **解決**: ProcessFlowViewModel.cs line 423で、`SelectedNode.OnPropertyChanged(nameof(SelectedNode.DisplayName))` を `OnPropertyChanged(nameof(SelectedNode))` に変更

### 2. ProcessDetailFinish中間テーブルの実装

#### 2.1 モデルクラスの作成
- `Models/ProcessDetailFinish.cs`を作成
- ProcessDetailIdとFinishProcessDetailIdを持つ中間テーブル

#### 2.2 リポジトリインターフェースの更新
- `IAccessRepository.cs`に以下のメソッドを追加：
  - GetProcessDetailFinishes(int cycleId)
  - GetFinishesByProcessDetailId(int processDetailId)
  - GetFinishesByFinishId(int finishProcessDetailId)
  - AddProcessDetailFinish(ProcessDetailFinish finish)
  - DeleteProcessDetailFinish(int id)
  - DeleteFinishesByProcessAndFinish(int processDetailId, int finishProcessDetailId)

#### 2.3 リポジトリ実装の更新
- `AccessRepository.cs`に上記メソッドの実装を追加

#### 2.4 移行ツールの作成
- `ProcessDetailFinishMigration.cs`を作成
- CreateTable()メソッド：テーブル作成
- MigrateFinishIdsToFinishTable()メソッド：既存データの移行
- CheckMigrationStatus()メソッド：移行状態確認

### 3. 移行処理のエラー対応

#### 3.1 データ型不一致エラー
- **問題**: OleDbでDapperの匿名タプルがサポートされない
- **解決**: 具象クラス`ProcessDetailFinishData`を定義して使用

#### 3.2 SQL WHERE句エラー
- **問題**: `WHERE FinishIds IS NOT NULL AND FinishIds <> ''`でデータ型不一致
- **解決**: 全レコードを取得してC#側でフィルタリング

#### 3.3 外部キー制約エラー
- **問題**: 存在しないProcessDetailIdを参照
- **解決**: 事前に存在するIDをHashSetに読み込んで検証

### 4. UI更新
- ProcessFlowView.xamlに「FinishIds移行」ボタンを追加
- ProcessFlowViewModel.csにMigrateFinishIdsToFinishTableCommandを追加

### 5. 既存コードの更新

#### 5.1 ProcessDetail.cs
- FinishIdsプロパティを削除

#### 5.2 ProcessFlowViewModel.cs
- SelectedNodeFinishIds関連のプロパティと参照を削除

#### 5.3 BuildDetailFunctions.cs
- FinishDevices()メソッドを更新
- ProcessDetailFinishテーブルから取得するように変更

#### 5.4 DetailUnitBuilder.cs
- BuildWarehouseメソッドを更新
- エラーメッセージを「FinishIds」から「終了工程」に変更

### 6. クリーンアップ
- 移行完了後、以下を削除：
  - 移行用ボタン（UI）
  - 移行用メソッド（ViewModel）
  - 移行用プロパティ（IsMigrationInProgress、MigrationStatus）
  - 移行用ファイル：
    - ProcessDetailFinishMigration.cs
    - ProcessDetailConnectionMigration.cs
    - MigrateFinishIdsData.sql
    - MigrateStartIdsData.sql
    - CreateProcessDetailConnectionTable.sql
  - 空のディレクトリ（DatabaseMigration、Database）

## 技術的な注意点

1. **OleDbとDapperの組み合わせ**
   - 匿名タプルは使用不可
   - パラメータは`?`を使用（名前付きパラメータは使用不可）
   - WHERE句でのテキストフィールド比較は避ける

2. **外部キー制約**
   - 中間テーブルに挿入する前に、参照先のレコードが存在することを確認

3. **CommunityToolkit.Mvvm**
   - ObservableObjectのOnPropertyChangedメソッドはprotected
   - 派生クラス外からは直接呼び出せない

## 最終状態
- FinishIdsフィールドは完全に削除
- ProcessDetailFinish中間テーブルで関係を管理
- 全ての参照箇所が更新済み
- 移行関連のコードは削除済み