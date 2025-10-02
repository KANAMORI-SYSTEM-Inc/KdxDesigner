-- MnemonicTimerDeviceテーブルの複合キー化マイグレーション
-- 実行日: 2025-08-02
-- 目的: (MnemonicId, RecordId, TimerId)の複合プライマリキーに変更

-- 1. ProcessDetailテーブルにStartTimer列を追加
ALTER TABLE ProcessDetail ADD COLUMN StartTimer INTEGER;

-- 2. 既存のMnemonicTimerDeviceテーブルのバックアップ
CREATE TABLE MnemonicTimerDevice_Backup AS 
SELECT * FROM MnemonicTimerDevice;

-- 3. 既存のMnemonicTimerDeviceテーブルを削除
DROP TABLE MnemonicTimerDevice;

-- 4. 新しい複合キー構造でMnemonicTimerDeviceテーブルを作成
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
);

-- 5. バックアップからデータを復元（TimerIdがNULLでないデータのみ）
INSERT INTO MnemonicTimerDevice (
    MnemonicId, RecordId, TimerId, TimerCategoryId, 
    ProcessTimerDevice, TimerDevice, PlcId, CycleId, 
    Comment1, Comment2, Comment3
)
SELECT 
    MnemonicId, RecordId, TimerId, TimerCategoryId,
    ProcessTimerDevice, TimerDevice, PlcId, CycleId,
    Comment1, Comment2, Comment3
FROM MnemonicTimerDevice_Backup
WHERE TimerId IS NOT NULL;

-- 6. インデックスの作成（検索性能向上のため）
CREATE INDEX idx_mnemonic_timer_plc ON MnemonicTimerDevice(PlcId, MnemonicId);
CREATE INDEX idx_mnemonic_timer_cycle ON MnemonicTimerDevice(PlcId, CycleId);
CREATE INDEX idx_mnemonic_timer_record ON MnemonicTimerDevice(RecordId);

-- 7. バックアップテーブルの削除（確認後に実行）
-- DROP TABLE MnemonicTimerDevice_Backup;