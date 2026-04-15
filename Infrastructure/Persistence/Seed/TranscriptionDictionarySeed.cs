// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed;

/// <summary>
/// Seeds default transcription dictionary entries used by the deterministic pre-pass
/// that corrects phonetic variants before the LLM is invoked.
/// </summary>
public static class TranscriptionDictionarySeed
{
    public static void SeedData(MigrationBuilder migrationBuilder)
    {
        var now = DateTime.UtcNow;
        var timestamp = now.ToString("yyyy-MM-dd HH:mm:ss");

        migrationBuilder.Sql($@"
            INSERT INTO transcription_dictionary_entries (id, correct_term, category, phonetic_variants, description, create_time, update_time, is_deleted) VALUES
            (gen_random_uuid(), 'Klacksy', NULL, '[""Klack Sie"", ""Klamp"", ""Kluxsy"", ""Kluxi"", ""Kluxie"", ""Knaxie"", ""Klux!"", ""Larksee""]'::jsonb, NULL, '{timestamp}', '{timestamp}', false);
        ");
    }
}
