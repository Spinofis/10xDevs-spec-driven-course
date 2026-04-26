ALTER TABLE trips
ADD COLUMN IF NOT EXISTS deleted_at timestamptz NULL;
