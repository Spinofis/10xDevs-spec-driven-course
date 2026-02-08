-- 20260116170705_vibe_trvelers.sql
-- VibeTravels MVP: initial schema (users, trips, preferences, tags, generation jobs, plans)
--
-- Idempotency note:
-- - Tables/extensions/indexes are created with IF NOT EXISTS.
-- - Enums and constraints are created via DO blocks guarded by catalog checks.

BEGIN;

-- -----------------------------------------------------------------------------
-- Extensions
-- -----------------------------------------------------------------------------
CREATE EXTENSION IF NOT EXISTS pgcrypto; -- gen_random_uuid()
CREATE EXTENSION IF NOT EXISTS citext;   -- case-insensitive text (for emails, tag slugs)

-- -----------------------------------------------------------------------------
-- Enums
-- -----------------------------------------------------------------------------
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'generation_job_status') THEN
    CREATE TYPE generation_job_status AS ENUM (
      'pending',
      'running',
      'succeeded',
      'failed',
      'canceled'
    );
  END IF;
END $$;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'place_type') THEN
    CREATE TYPE place_type AS ENUM (
      'attraction',
      'restaurant',
      'hotel'
    );
  END IF;
END $$;

-- -----------------------------------------------------------------------------
-- Core tables
-- -----------------------------------------------------------------------------

-- Users of the system.
CREATE TABLE IF NOT EXISTS users (
  id            uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  email         citext      NOT NULL,
  password_hash text        NOT NULL,
  display_name  text        NULL,
  created_at    timestamptz NOT NULL DEFAULT now(),
  updated_at    timestamptz NOT NULL DEFAULT now()
);

-- Unique email (case-insensitive via citext).
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'users_email_uk'
  ) THEN
    ALTER TABLE users
      ADD CONSTRAINT users_email_uk UNIQUE (email);
  END IF;
END $$;

-- Global dictionary of tags.
CREATE TABLE IF NOT EXISTS tags (
  id         uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  slug       citext      NOT NULL,
  label      text        NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);

DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'tags_slug_uk'
  ) THEN
    ALTER TABLE tags
      ADD CONSTRAINT tags_slug_uk UNIQUE (slug);
  END IF;
END $$;

-- Trips/notes: a single logical entity in MVP.
CREATE TABLE IF NOT EXISTS trips (
  id                uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id           uuid        NOT NULL,

  title             text        NOT NULL,
  notes             text        NULL,

  -- Input fields (range provided by user).
  input_date_from   date        NULL,
  input_date_to     date        NULL,
  input_days_min    integer     NULL,
  input_days_max    integer     NULL,

  -- Output/selected fields (chosen by AI / final plan selection).
  selected_date_from date       NULL,
  selected_date_to   date       NULL,
  selected_days      integer    NULL,

  created_at         timestamptz NOT NULL DEFAULT now(),
  updated_at         timestamptz NOT NULL DEFAULT now()
);

-- FK: trips -> users (hard delete cascade in MVP).
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM pg_constraint
    WHERE conname = 'trips_user_id_fk'
  ) THEN
    ALTER TABLE trips
      ADD CONSTRAINT trips_user_id_fk
      FOREIGN KEY (user_id) REFERENCES users(id)
      ON DELETE CASCADE;
  END IF;
END $$;

-- Basic sanity checks for ranges.
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'trips_input_days_chk') THEN
    ALTER TABLE trips
      ADD CONSTRAINT trips_input_days_chk
      CHECK (
        (input_days_min IS NULL OR input_days_min > 0)
        AND (input_days_max IS NULL OR input_days_max > 0)
        AND (input_days_min IS NULL OR input_days_max IS NULL OR input_days_min <= input_days_max)
      );
  END IF;

  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'trips_selected_days_chk') THEN
    ALTER TABLE trips
      ADD CONSTRAINT trips_selected_days_chk
      CHECK (selected_days IS NULL OR selected_days > 0);
  END IF;
END $$;

-- User preferences (explicit columns; no JSONB).
-- Values are modeled as "weights" (INT) with default=5.
CREATE TABLE IF NOT EXISTS user_preferences (
  user_id           uuid        PRIMARY KEY,

  pace_weight       integer     NOT NULL DEFAULT 5,
  budget_weight     integer     NOT NULL DEFAULT 5,
  comfort_weight    integer     NOT NULL DEFAULT 5,
  adventure_weight  integer     NOT NULL DEFAULT 5,
  culture_weight    integer     NOT NULL DEFAULT 5,
  nature_weight     integer     NOT NULL DEFAULT 5,
  food_weight       integer     NOT NULL DEFAULT 5,

  created_at        timestamptz NOT NULL DEFAULT now(),
  updated_at        timestamptz NOT NULL DEFAULT now()
);

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'user_preferences_user_id_fk') THEN
    ALTER TABLE user_preferences
      ADD CONSTRAINT user_preferences_user_id_fk
      FOREIGN KEY (user_id) REFERENCES users(id)
      ON DELETE CASCADE;
  END IF;

  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'user_preferences_weights_chk') THEN
    ALTER TABLE user_preferences
      ADD CONSTRAINT user_preferences_weights_chk
      CHECK (
        pace_weight BETWEEN 1 AND 10
        AND budget_weight BETWEEN 1 AND 10
        AND comfort_weight BETWEEN 1 AND 10
        AND adventure_weight BETWEEN 1 AND 10
        AND culture_weight BETWEEN 1 AND 10
        AND nature_weight BETWEEN 1 AND 10
        AND food_weight BETWEEN 1 AND 10
      );
  END IF;
END $$;

-- -----------------------------------------------------------------------------
-- Generation jobs (one trip -> many jobs, but max one active at a time)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS generation_jobs (
  id            uuid                 PRIMARY KEY DEFAULT gen_random_uuid(),
  trip_id       uuid                 NOT NULL,
  status        generation_job_status NOT NULL DEFAULT 'pending',

  -- Snapshot of inputs used for generation.
  input_snapshot jsonb               NOT NULL,
  input_hash     text                NOT NULL,

  requested_at   timestamptz          NOT NULL DEFAULT now(),
  started_at     timestamptz          NULL,
  finished_at    timestamptz          NULL,
  canceled_at    timestamptz          NULL,

  error_message  text                 NULL
);

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'generation_jobs_trip_id_fk') THEN
    ALTER TABLE generation_jobs
      ADD CONSTRAINT generation_jobs_trip_id_fk
      FOREIGN KEY (trip_id) REFERENCES trips(id)
      ON DELETE CASCADE;
  END IF;
END $$;

-- Only one "active" job per trip at a time.
-- Active statuses are treated as: pending/running.
CREATE UNIQUE INDEX IF NOT EXISTS generation_jobs_one_active_per_trip_ux
  ON generation_jobs (trip_id)
  WHERE status IN ('pending', 'running');

-- Helpful indexes for listing / filtering jobs.
CREATE INDEX IF NOT EXISTS generation_jobs_trip_id_requested_at_idx
  ON generation_jobs (trip_id, requested_at DESC);

CREATE INDEX IF NOT EXISTS generation_jobs_status_requested_at_idx
  ON generation_jobs (status, requested_at DESC);

CREATE INDEX IF NOT EXISTS generation_jobs_input_hash_idx
  ON generation_jobs (input_hash);

-- -----------------------------------------------------------------------------
-- Trip plans (current plan header) + plan items (overwritten atomically)
-- -----------------------------------------------------------------------------

-- 1:1 with trip.
CREATE TABLE IF NOT EXISTS trip_plans (
  trip_id            uuid        PRIMARY KEY,

  -- Latest successful job that produced this plan (optional).
  generation_job_id  uuid        NULL,

  title              text        NULL,
  summary            text        NULL,

  created_at         timestamptz NOT NULL DEFAULT now(),
  updated_at         timestamptz NOT NULL DEFAULT now()
);

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'trip_plans_trip_id_fk') THEN
    ALTER TABLE trip_plans
      ADD CONSTRAINT trip_plans_trip_id_fk
      FOREIGN KEY (trip_id) REFERENCES trips(id)
      ON DELETE CASCADE;
  END IF;

  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'trip_plans_generation_job_id_fk') THEN
    ALTER TABLE trip_plans
      ADD CONSTRAINT trip_plans_generation_job_id_fk
      FOREIGN KEY (generation_job_id) REFERENCES generation_jobs(id)
      ON DELETE SET NULL;
  END IF;
END $$;

-- Plan items are tied directly to trip (simplifies overwrite: delete+insert within a transaction).
CREATE TABLE IF NOT EXISTS plan_items (
  id           uuid       PRIMARY KEY DEFAULT gen_random_uuid(),
  trip_id      uuid       NOT NULL,

  -- Date and time (time is optional); no time zones in MVP.
  item_date    date       NOT NULL,
  item_time    time       NULL,

  sort_order   integer    NOT NULL,

  place_type   place_type NOT NULL,
  place_name   text       NOT NULL,
  description  text       NULL,

  created_at   timestamptz NOT NULL DEFAULT now()
);

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'plan_items_trip_id_fk') THEN
    ALTER TABLE plan_items
      ADD CONSTRAINT plan_items_trip_id_fk
      FOREIGN KEY (trip_id) REFERENCES trips(id)
      ON DELETE CASCADE;
  END IF;

  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'plan_items_sort_order_chk') THEN
    ALTER TABLE plan_items
      ADD CONSTRAINT plan_items_sort_order_chk
      CHECK (sort_order >= 0);
  END IF;
END $$;

-- Indexes for reading the plan in correct order.
CREATE INDEX IF NOT EXISTS plan_items_trip_id_date_order_idx
  ON plan_items (trip_id, item_date, sort_order);

-- -----------------------------------------------------------------------------
-- Tag link tables
-- -----------------------------------------------------------------------------

-- Trip <-> Tag (N:M).
CREATE TABLE IF NOT EXISTS trip_tags (
  trip_id uuid NOT NULL,
  tag_id  uuid NOT NULL,
  PRIMARY KEY (trip_id, tag_id)
);

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'trip_tags_trip_id_fk') THEN
    ALTER TABLE trip_tags
      ADD CONSTRAINT trip_tags_trip_id_fk
      FOREIGN KEY (trip_id) REFERENCES trips(id)
      ON DELETE CASCADE;
  END IF;

  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'trip_tags_tag_id_fk') THEN
    ALTER TABLE trip_tags
      ADD CONSTRAINT trip_tags_tag_id_fk
      FOREIGN KEY (tag_id) REFERENCES tags(id)
      ON DELETE CASCADE;
  END IF;
END $$;

CREATE INDEX IF NOT EXISTS trip_tags_tag_id_idx
  ON trip_tags (tag_id);

-- User preference tags with weights.
CREATE TABLE IF NOT EXISTS user_preference_tags (
  user_id uuid    NOT NULL,
  tag_id  uuid    NOT NULL,
  weight  integer NOT NULL DEFAULT 5,
  PRIMARY KEY (user_id, tag_id)
);

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'user_preference_tags_user_id_fk') THEN
    ALTER TABLE user_preference_tags
      ADD CONSTRAINT user_preference_tags_user_id_fk
      FOREIGN KEY (user_id) REFERENCES users(id)
      ON DELETE CASCADE;
  END IF;

  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'user_preference_tags_tag_id_fk') THEN
    ALTER TABLE user_preference_tags
      ADD CONSTRAINT user_preference_tags_tag_id_fk
      FOREIGN KEY (tag_id) REFERENCES tags(id)
      ON DELETE CASCADE;
  END IF;

  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'user_preference_tags_weight_chk') THEN
    ALTER TABLE user_preference_tags
      ADD CONSTRAINT user_preference_tags_weight_chk
      CHECK (weight BETWEEN 1 AND 10);
  END IF;
END $$;

CREATE INDEX IF NOT EXISTS user_preference_tags_tag_id_idx
  ON user_preference_tags (tag_id);

-- -----------------------------------------------------------------------------
-- Query-performance indexes (MVP)
-- -----------------------------------------------------------------------------

-- Listing trips for a user.
CREATE INDEX IF NOT EXISTS trips_user_id_updated_at_idx
  ON trips (user_id, updated_at DESC);

-- Fast lookup of trip plan header.
CREATE INDEX IF NOT EXISTS trip_plans_generation_job_id_idx
  ON trip_plans (generation_job_id);

COMMIT;
