-- Timer挿入用のPostgreSQL関数（NULL対応版）
-- IDはIDENTITYで自動採番されるため、挿入時に指定しない

DROP FUNCTION IF EXISTS insert_timer(INT, INT, INT, TEXT, INT, INT);

CREATE OR REPLACE FUNCTION insert_timer(
    cycle_id_param INT,
    timer_category_id_param INT DEFAULT NULL,
    timer_num_param INT DEFAULT NULL,
    timer_name_param TEXT DEFAULT NULL,
    mnemonic_id_param INT DEFAULT NULL,
    example_param INT DEFAULT NULL
)
RETURNS BIGINT
LANGUAGE plpgsql
AS $$
DECLARE
    new_id BIGINT;
BEGIN
    INSERT INTO "Timer" (
        "CycleId",
        "TimerCategoryId",
        "TimerNum",
        "TimerName",
        "MnemonicId",
        "Example"
    )
    VALUES (
        cycle_id_param,
        timer_category_id_param,
        timer_num_param,
        timer_name_param,
        mnemonic_id_param,
        example_param
    )
    RETURNING "ID" INTO new_id;

    RETURN new_id;
END;
$$;
