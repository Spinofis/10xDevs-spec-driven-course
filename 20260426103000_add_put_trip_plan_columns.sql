-- Add columns required by PUT /trips/{tripId}/plan contract.

BEGIN;

ALTER TABLE trip_plans
  ADD COLUMN IF NOT EXISTS version integer NOT NULL DEFAULT 1;

ALTER TABLE trip_plans
  ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'generated';

ALTER TABLE trip_plans
  ADD COLUMN IF NOT EXISTS generated_at timestamptz NULL;

ALTER TABLE trip_plans
  ADD COLUMN IF NOT EXISTS saved_at timestamptz NULL;

UPDATE trip_plans
SET generated_at = COALESCE(generated_at, updated_at)
WHERE generation_job_id IS NOT NULL;

UPDATE trip_plans
SET saved_at = COALESCE(saved_at, updated_at)
WHERE generation_job_id IS NULL;

ALTER TABLE plan_items
  ADD COLUMN IF NOT EXISTS day_number integer NOT NULL DEFAULT 1;

ALTER TABLE plan_items
  ADD COLUMN IF NOT EXISTS start_time time NULL;

ALTER TABLE plan_items
  ADD COLUMN IF NOT EXISTS title text NULL;

ALTER TABLE plan_items
  ADD COLUMN IF NOT EXISTS location_text text NULL;

ALTER TABLE plan_items
  ADD COLUMN IF NOT EXISTS updated_at timestamptz NOT NULL DEFAULT now();

UPDATE plan_items
SET title = COALESCE(title, place_name)
WHERE title IS NULL;

UPDATE plan_items
SET location_text = COALESCE(location_text, place_name)
WHERE location_text IS NULL;

UPDATE plan_items
SET start_time = COALESCE(start_time, item_time)
WHERE item_time IS NOT NULL;

DO $$
DECLARE
  column_type text;
BEGIN
  SELECT data_type
  INTO column_type
  FROM information_schema.columns
  WHERE table_name = 'plan_items'
    AND column_name = 'item_date'
  LIMIT 1;

  IF column_type = 'date' THEN
    ALTER TABLE plan_items
      ALTER COLUMN item_date TYPE timestamptz
      USING item_date::timestamp AT TIME ZONE 'UTC';
  END IF;
END $$;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'trip_plans_version_chk'
  ) THEN
    ALTER TABLE trip_plans
      ADD CONSTRAINT trip_plans_version_chk CHECK (version > 0);
  END IF;
END $$;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'trip_plans_status_chk'
  ) THEN
    ALTER TABLE trip_plans
      ADD CONSTRAINT trip_plans_status_chk CHECK (status IN ('generated', 'saved'));
  END IF;
END $$;

CREATE INDEX IF NOT EXISTS plan_items_trip_id_day_order_idx
  ON plan_items (trip_id, day_number, sort_order, id);

COMMIT;
