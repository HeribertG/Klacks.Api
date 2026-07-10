-- Registers the self-hosted Whisper container (docker-compose-server.yml service "whisper-stt")
-- as a custom STT provider. Idempotent: safe to run multiple times.
-- NOTE: Normal installations use the settings card (WhisperPluginController -> Klacks.Updater),
-- which maintains this row automatically (NpgsqlWhisperProviderStore). This script remains for
-- manual installs only; the provider id must stay in sync with WhisperPluginConstants.ProviderId.
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
