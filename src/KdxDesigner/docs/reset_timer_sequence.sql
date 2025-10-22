-- TimerテーブルのIDシーケンスをリセット
-- 現在の最大IDが1078なので、次のIDが1079になるように設定

-- 方法1: 現在の最大IDを自動的に取得してシーケンスをリセット
SELECT setval(
    pg_get_serial_sequence('"Timer"', 'ID'),
    COALESCE((SELECT MAX("ID") FROM "Timer"), 1),
    true
);

-- 方法2: 手動で次のID値を指定する場合（最大IDが1078なら1079に設定）
-- SELECT setval(pg_get_serial_sequence('"Timer"', 'ID'), 1078, true);

-- 確認: 現在のシーケンス値を表示
-- SELECT currval(pg_get_serial_sequence('"Timer"', 'ID'));
