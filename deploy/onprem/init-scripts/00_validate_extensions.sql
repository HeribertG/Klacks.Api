-- Validates that required PostgreSQL extensions are available in the Docker image.
-- Runs only on first container start (empty database initialization).
-- Fails fast if pgvector is missing, preventing silent data issues.

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_available_extensions WHERE name = 'vector') THEN
    RAISE EXCEPTION 'FATAL: pgvector extension not available in this Docker image! Use ghcr.io/heribertg/klacks-postgres:19 (PostgreSQL with pgvector) instead of a plain postgres image.';
  END IF;

  RAISE NOTICE 'Extension validation passed: pgvector is available.';
END $$;
