-- Registers the self-hosted Whisper container (docker-compose-server.yml service "whisper-stt")
-- as a custom STT provider. Idempotent: safe to run multiple times.
-- Run on the server against the Klacks database:
--   docker exec -i klacks-postgres psql -U admin -d Klacks < Klacks.Api/deploy/whisper-stt-provider.sql

INSERT INTO custom_stt_providers
    (id, name, connection_type, api_url, api_key, language_model, is_enabled, is_system, is_deleted, create_time)
VALUES
    ('7f3d2a10-0000-4000-8000-57705731a001',
     'Whisper (self-hosted)',
     'rest',
     'http://whisper-stt:8000',
     NULL,
     'deepdml/faster-whisper-large-v3-turbo-ct2',
     true,
     true,
     false,
     NOW())
ON CONFLICT (id) DO UPDATE
SET api_url = EXCLUDED.api_url,
    language_model = EXCLUDED.language_model,
    is_enabled = EXCLUDED.is_enabled,
    is_deleted = false,
    update_time = NOW();
