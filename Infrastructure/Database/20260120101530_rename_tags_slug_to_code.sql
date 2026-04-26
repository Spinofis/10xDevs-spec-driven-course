-- ============================================================
-- Migration: rename tags.slug -> tags.code
-- Purpose  : Replace misleading "slug" naming with "code"
-- Author   : vibe_travels
-- ============================================================

BEGIN;

-- 1. Dodaj kolumnę `code`, jeśli jeszcze nie istnieje
ALTER TABLE tags
ADD COLUMN IF NOT EXISTS code VARCHAR(100);

-- 2. Skopiuj dane z `slug` do `code` (tylko jeśli slug istnieje)
DO $$
BEGIN
    IF EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_name = 'tags'
          AND column_name = 'slug'
    ) THEN
        UPDATE tags
        SET code = slug
        WHERE code IS NULL;
    END IF;
END $$;

-- 3. Ustaw NOT NULL (tylko jeśli kolumna istnieje)
ALTER TABLE tags
ALTER COLUMN code SET NOT NULL;

-- 4. Usuń stary unikalny indeks na slug (jeśli istnieje)
DROP INDEX IF EXISTS idx_tags_slug;

-- 5. Dodaj unikalny indeks na code
CREATE UNIQUE INDEX IF NOT EXISTS idx_tags_code
ON tags (code);

-- 6. Usuń kolumnę slug (jeśli istnieje)
ALTER TABLE tags
DROP COLUMN IF EXISTS slug;

COMMIT;
