-- CY-IO関連付け機能の実装に伴うデータベース変更
-- 実行日: 2025-08-02

-- 1. IOテーブルの複合キー化（IDを削除してAddress+PlcIdを複合キーに）
-- バックアップテーブルの作成
CREATE TABLE IO_Backup AS 
SELECT * FROM IO;

-- 既存のIOテーブルを削除
DROP TABLE IO;

-- 新しい複合キー構造でIOテーブルを作成
CREATE TABLE IO (
    IOText TEXT,
    XComment TEXT,
    YComment TEXT,
    FComment TEXT,
    Address TEXT NOT NULL,
    PlcId INTEGER NOT NULL,
    IOName TEXT,
    IOExplanation TEXT,
    IOSpot TEXT,
    UnitName TEXT,
    System TEXT,
    StationNumber TEXT,
    IONameNaked TEXT,
    LinkDevice TEXT,
    PRIMARY KEY (Address, PlcId)
);

-- バックアップからデータを復元
INSERT INTO IO (
    IOText, XComment, YComment, FComment, Address, PlcId,
    IOName, IOExplanation, IOSpot, UnitName, System,
    StationNumber, IONameNaked, LinkDevice
)
SELECT 
    IOText, XComment, YComment, FComment, Address, PlcId,
    IOName, IOExplanation, IOSpot, UnitName, System,
    StationNumber, IONameNaked, LinkDevice
FROM IO_Backup
WHERE Address IS NOT NULL AND PlcId IS NOT NULL;

-- 2. CylinderIO中間テーブルの作成
CREATE TABLE CylinderIO (
    CylinderId INTEGER NOT NULL,
    IOAddress TEXT NOT NULL,
    PlcId INTEGER NOT NULL,
    IOType TEXT NOT NULL,
    SortOrder INTEGER,
    Comment TEXT,
    PRIMARY KEY (CylinderId, IOAddress, PlcId),
    FOREIGN KEY (CylinderId) REFERENCES CY(Id),
    FOREIGN KEY (IOAddress, PlcId) REFERENCES IO(Address, PlcId)
);

-- インデックスの作成
CREATE INDEX idx_cylinder_io_cylinder ON CylinderIO(CylinderId);
CREATE INDEX idx_cylinder_io_io ON CylinderIO(IOAddress, PlcId);
CREATE INDEX idx_cylinder_io_type ON CylinderIO(IOType);

-- 3. バックアップテーブルの削除（確認後に実行）
-- DROP TABLE IO_Backup;