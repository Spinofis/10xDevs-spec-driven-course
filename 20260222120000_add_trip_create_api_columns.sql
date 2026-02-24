-- 20260222120000_add_trip_create_api_columns.sql
-- Adds columns required by POST /trips API contract (idempotent).

BEGIN;

ALTER TABLE trips
  ADD COLUMN IF NOT EXISTS place_text text NULL;

ALTER TABLE trips
  ADD COLUMN IF NOT EXISTS people_count integer NULL;

ALTER TABLE trips
  ADD COLUMN IF NOT EXISTS budget_level text NULL;

ALTER TABLE trips
  ADD COLUMN IF NOT EXISTS pace text NULL;

ALTER TABLE trips
  ADD COLUMN IF NOT EXISTS generated_at timestamptz NULL;

ALTER TABLE trips
  ADD COLUMN IF NOT EXISTS has_generated_plan boolean NOT NULL DEFAULT false;

ALTER TABLE trip_tags
  ADD COLUMN IF NOT EXISTS created_at timestamptz NOT NULL DEFAULT now();

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'trips_people_count_chk'
  ) THEN
    ALTER TABLE trips
      ADD CONSTRAINT trips_people_count_chk
      CHECK (people_count IS NULL OR people_count > 0);
  END IF;
END $$;

COMMIT;

