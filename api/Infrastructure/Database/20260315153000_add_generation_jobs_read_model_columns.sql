-- Extend generation_jobs read model columns used by GET /generation-jobs/{jobId}

ALTER TABLE generation_jobs
  ADD COLUMN IF NOT EXISTS user_id uuid;

UPDATE generation_jobs AS gj
SET user_id = t.user_id
FROM trips AS t
WHERE gj.trip_id = t.id
  AND gj.user_id IS NULL;

ALTER TABLE generation_jobs
  ALTER COLUMN user_id SET NOT NULL;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'generation_jobs_user_id_fk') THEN
    ALTER TABLE generation_jobs
      ADD CONSTRAINT generation_jobs_user_id_fk
      FOREIGN KEY (user_id) REFERENCES users(id)
      ON DELETE CASCADE;
  END IF;
END $$;

ALTER TABLE generation_jobs
  ADD COLUMN IF NOT EXISTS attempt_no integer NOT NULL DEFAULT 0;

ALTER TABLE generation_jobs
  ADD COLUMN IF NOT EXISTS error_code text NULL;

ALTER TABLE generation_jobs
  ADD COLUMN IF NOT EXISTS discarded boolean NOT NULL DEFAULT false;

ALTER TABLE generation_jobs
  ADD COLUMN IF NOT EXISTS discard_reason text NULL;
