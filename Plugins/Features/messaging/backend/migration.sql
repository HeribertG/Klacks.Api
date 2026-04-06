-- Messaging Plugin Migration (idempotent)
-- Creates messaging_providers and messages tables if they don't exist.

CREATE TABLE IF NOT EXISTS messaging_providers (
    id UUID NOT NULL,
    name VARCHAR(100) NOT NULL,
    display_name VARCHAR(200) NOT NULL,
    provider_type VARCHAR(50) NOT NULL,
    is_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    config_json TEXT NOT NULL DEFAULT '{}',
    webhook_secret VARCHAR(256) NOT NULL DEFAULT '',
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    CONSTRAINT pk_messaging_providers PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_messaging_providers_name
    ON messaging_providers (name);

CREATE TABLE IF NOT EXISTS messages (
    id UUID NOT NULL,
    provider_id UUID NOT NULL,
    external_message_id VARCHAR(256) NOT NULL DEFAULT '',
    sender VARCHAR(100) NOT NULL DEFAULT '',
    sender_display_name VARCHAR(200) NOT NULL DEFAULT '',
    recipient VARCHAR(100) NOT NULL DEFAULT '',
    recipient_display_name VARCHAR(200) NOT NULL DEFAULT '',
    content TEXT NOT NULL DEFAULT '',
    content_type VARCHAR(50) NOT NULL DEFAULT 'text',
    direction INTEGER NOT NULL DEFAULT 0,
    status INTEGER NOT NULL DEFAULT 0,
    "timestamp" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    error_message TEXT,
    media_url TEXT,
    CONSTRAINT pk_messages PRIMARY KEY (id),
    CONSTRAINT fk_messages_messaging_providers_provider_id
        FOREIGN KEY (provider_id)
        REFERENCES messaging_providers (id)
        ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS ix_messages_direction
    ON messages (direction);

CREATE INDEX IF NOT EXISTS ix_messages_provider_id_timestamp
    ON messages (provider_id, "timestamp" DESC);

CREATE TABLE IF NOT EXISTS messenger_contact (
    id UUID NOT NULL,
    client_id UUID NOT NULL,
    type INTEGER NOT NULL,
    value VARCHAR(200) NOT NULL,
    description VARCHAR(200),
    is_deleted BOOLEAN NOT NULL DEFAULT FALSE,
    create_time TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    update_time TIMESTAMP WITH TIME ZONE,
    CONSTRAINT pk_messenger_contact PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_messenger_contact_client_id
    ON messenger_contact (client_id);

CREATE INDEX IF NOT EXISTS ix_messenger_contact_client_id_type
    ON messenger_contact (client_id, type);
