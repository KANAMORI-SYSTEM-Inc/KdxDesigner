-- TimerRecordIds中間テーブルの作成
CREATE TABLE TimerRecordIds (
    TimerId INTEGER NOT NULL,
    RecordId INTEGER NOT NULL,
    PRIMARY KEY (TimerId, RecordId)
);

-- 既存のTimer.RecordIdsデータをTimerRecordIdsテーブルに移行
-- 注意: このスクリプトは手動で実行する必要があります
-- RecordIdsはセミコロン区切りのテキストなので、Accessで以下の処理を行ってください：
-- 1. Timerテーブルの各レコードを確認
-- 2. RecordIdsフィールドをセミコロンで分割
-- 3. 各RecordIdに対してTimerRecordIdsにレコードを挿入

-- 例：
-- INSERT INTO TimerRecordIds (TimerId, RecordId) VALUES (1, 100);
-- INSERT INTO TimerRecordIds (TimerId, RecordId) VALUES (1, 101);
-- INSERT INTO TimerRecordIds (TimerId, RecordId) VALUES (2, 200);

-- 移行完了後、Timer.RecordIds列は削除できます（オプション）
-- ALTER TABLE Timer DROP COLUMN RecordIds;