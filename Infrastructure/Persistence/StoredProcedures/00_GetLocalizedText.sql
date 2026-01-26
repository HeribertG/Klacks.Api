-- GetLocalizedText: Returns localized text with fallback support
-- Parameters:
--   translations: JSONB object with language codes as keys (e.g., {"de": "Text", "en": "Text"})
--   requested_language: The preferred language code
--   fallback_order: Array of language codes for fallback (from MultiLanguage.SupportedLanguages)
-- Returns: The localized text, using fallback if requested language is empty

DROP FUNCTION IF EXISTS get_localized_text(JSONB, TEXT, TEXT[]);

CREATE OR REPLACE FUNCTION get_localized_text(
    translations JSONB,
    requested_language TEXT,
    fallback_order TEXT[]
)
RETURNS TEXT
LANGUAGE plpgsql
IMMUTABLE
AS $$
DECLARE
    result TEXT;
    lang TEXT;
BEGIN
    IF translations IS NULL THEN
        RETURN '';
    END IF;

    -- Try requested language first
    result := translations ->> requested_language;
    IF result IS NOT NULL AND result != '' THEN
        RETURN result;
    END IF;

    -- Try fallback languages in order
    FOREACH lang IN ARRAY fallback_order
    LOOP
        IF lang != requested_language THEN
            result := translations ->> lang;
            IF result IS NOT NULL AND result != '' THEN
                RETURN result;
            END IF;
        END IF;
    END LOOP;

    RETURN '';
END;
$$;
