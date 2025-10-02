-- Process複数値フィールド移行用テーブル作成スクリプト
-- Microsoft Access用
-- 
-- 使用方法:
-- 1. Microsoft Accessでデータベースファイルを開く
-- 2. 「作成」タブから「クエリデザイン」を選択
-- 3. SQLビューに切り替え
-- 4. 各CREATE TABLE文を個別に実行（Accessは複数のCREATE文を一度に実行できません）

-- ================================================================
-- ProcessStartConditionテーブル
-- Process.AutoConditionフィールドの移行先
-- ================================================================
CREATE TABLE ProcessStartCondition (
    Id AUTOINCREMENT PRIMARY KEY,
    ProcessId LONG NOT NULL,
    StartProcessDetailId LONG NOT NULL,
    StartSensor TEXT(255),
    CONSTRAINT FK_ProcessStartCondition_Process 
        FOREIGN KEY (ProcessId) REFERENCES Process(Id),
    CONSTRAINT FK_ProcessStartCondition_ProcessDetail 
        FOREIGN KEY (StartProcessDetailId) REFERENCES ProcessDetail(Id)
);

-- ================================================================
-- ProcessFinishConditionテーブル
-- Process.FinishIdフィールドの移行先
-- ================================================================
CREATE TABLE ProcessFinishCondition (
    Id AUTOINCREMENT PRIMARY KEY,
    ProcessId LONG NOT NULL,
    FinishProcessDetailId LONG NOT NULL,
    FinishSensor TEXT(255),
    CONSTRAINT FK_ProcessFinishCondition_Process 
        FOREIGN KEY (ProcessId) REFERENCES Process(Id),
    CONSTRAINT FK_ProcessFinishCondition_ProcessDetail 
        FOREIGN KEY (FinishProcessDetailId) REFERENCES ProcessDetail(Id)
);

-- ================================================================
-- インデックスの作成（オプション - パフォーマンス向上のため）
-- これらも個別に実行してください
-- ================================================================

-- ProcessStartConditionのインデックス
CREATE INDEX IX_ProcessStartCondition_ProcessId 
    ON ProcessStartCondition (ProcessId);

CREATE INDEX IX_ProcessStartCondition_StartProcessDetailId 
    ON ProcessStartCondition (StartProcessDetailId);

-- ProcessFinishConditionのインデックス
CREATE INDEX IX_ProcessFinishCondition_ProcessId 
    ON ProcessFinishCondition (ProcessId);

CREATE INDEX IX_ProcessFinishCondition_FinishProcessDetailId 
    ON ProcessFinishCondition (FinishProcessDetailId);

-- ================================================================
-- 確認用クエリ
-- テーブルが正しく作成されたか確認
-- ================================================================
-- SELECT * FROM ProcessStartCondition;
-- SELECT * FROM ProcessFinishCondition;