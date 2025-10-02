-- OperationIOテーブルの作成
-- 実行日: 2025-08-02
-- 目的: OperationとIOの関連付けを管理する中間テーブル

CREATE TABLE OperationIO (
    OperationId INTEGER NOT NULL,
    IOAddress TEXT NOT NULL,
    PlcId INTEGER NOT NULL,
    IOUsage TEXT NOT NULL,
    SortOrder INTEGER,
    Comment TEXT,
    PRIMARY KEY (OperationId, IOAddress, PlcId)
);