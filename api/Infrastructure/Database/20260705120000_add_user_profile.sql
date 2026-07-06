-- 20260705120000_add_user_profile.sql
-- Adds user_profile table for storing user preferences (budget, pace, people count, notes).
-- user_preference_tags already exists from initial migration.

BEGIN;

CREATE TABLE IF NOT EXISTS user_profile (
  user_id              uuid        PRIMARY KEY,
  default_budget_level text        NULL,
  default_people_count integer     NULL,
  default_pace         text        NULL,
  default_notes        text        NULL,
  is_default           boolean     NOT NULL DEFAULT true,
  created_at           timestamptz NOT NULL DEFAULT now(),
  updated_at           timestamptz NOT NULL DEFAULT now()
);

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'user_profile_user_id_fk'
  ) THEN
    ALTER TABLE user_profile
      ADD CONSTRAINT user_profile_user_id_fk
      FOREIGN KEY (user_id) REFERENCES users(id)
      ON DELETE CASCADE;
  END IF;
END $$;

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM pg_constraint WHERE conname = 'user_profile_people_count_chk'
  ) THEN
    ALTER TABLE user_profile
      ADD CONSTRAINT user_profile_people_count_chk
      CHECK (default_people_count IS NULL OR default_people_count > 0);
  END IF;
END $$;

COMMIT;
