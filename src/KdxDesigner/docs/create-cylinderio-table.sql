-- CylinderIOテーブルの作成
-- 実行日: 2025-08-02
-- 目的: CYとIOの関連付けを管理する中間テーブル

CREATE TABLE CylinderIO (
    CylinderId INTEGER NOT NULL,
    IOAddress TEXT NOT NULL,
    PlcId INTEGER NOT NULL,
    IOType TEXT NOT NULL,
    SortOrder INTEGER,
    Comment TEXT,
    PRIMARY KEY (CylinderId, IOAddress, PlcId)
);