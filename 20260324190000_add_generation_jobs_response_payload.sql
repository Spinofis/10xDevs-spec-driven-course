-- Extend generation_jobs with raw OpenAI response payload for diagnostics.

ALTER TABLE generation_jobs
  ADD COLUMN IF NOT EXISTS response_payload jsonb;
