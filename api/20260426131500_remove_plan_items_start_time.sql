-- Remove plan_items.start_time and keep time only in plan_items.item_date (timestamptz).

BEGIN;

DO $$
DECLARE
  item_date_data_type text;
  has_start_time boolean;
BEGIN
  SELECT data_type
  INTO item_date_data_type
  FROM information_schema.columns
  WHERE table_name = 'plan_items'
    AND column_name = 'item_date'
  LIMIT 1;

  IF item_date_data_type = 'date' THEN
    ALTER TABLE plan_items
      ALTER COLUMN item_date TYPE timestamptz
      USING item_date::timestamp AT TIME ZONE 'UTC';
  END IF;

  SELECT EXISTS (
    SELECT 1
    FROM information_schema.columns
    WHERE table_name = 'plan_items'
      AND column_name = 'start_time'
  )
  INTO has_start_time;

  IF has_start_time THEN
    UPDATE plan_items
    SET item_date = (((item_date AT TIME ZONE 'UTC')::date + start_time)::timestamp AT TIME ZONE 'UTC')
    WHERE start_time IS NOT NULL;

    ALTER TABLE plan_items
      DROP COLUMN start_time;
  END IF;
END $$;

COMMIT;
