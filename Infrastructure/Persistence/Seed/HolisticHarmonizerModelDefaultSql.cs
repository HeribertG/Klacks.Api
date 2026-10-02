// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Installs gemini-25-flash as the Holistic Harmonizer (Wizard 3) model and replaces the former demo
/// choice gemini-35-flash. gemini-3.5-flash is a thinking model that spent the pre-flight ping's whole
/// output budget on reasoning and failed the run; gemini-2.5-flash reads the schedule image, follows the
/// JSON-only instruction, accepts thinkingBudget 0 and is part of the fresh-install catalog.
/// </summary>
/// <remarks>
/// The insert runs only where no model was chosen yet and the default is callable (enabled model, enabled
/// provider with a key); a fresh install without a Google key therefore keeps reporting "no model
/// configured" and skips stage 3 instead of failing it. The update touches the exact legacy value only, so
/// every other customer choice stays. WHERE NOT EXISTS instead of ON CONFLICT: ix_settings_type is unique,
/// and only a Sql(...) migration reaches existing databases. The values are frozen literals: a migration
/// must keep doing what it did when it shipped.
/// </remarks>

using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class HolisticHarmonizerModelDefaultSql
    {
        public const string SettingType = "WIZARD3_LLM_MODEL";
        public const string DefaultModelId = "gemini-25-flash";
        public const string LegacyDefaultModelId = "gemini-35-flash";

        private const string DefaultModelEnabled =
            "SELECT 1 FROM llm_models m " +
            "WHERE m.model_id = '" + DefaultModelId + "' AND m.is_enabled AND NOT m.is_deleted";

        private const string DefaultModelCallable =
            "SELECT 1 FROM llm_models m " +
            "JOIN llm_providers p ON p.provider_id = m.provider_id " +
            "WHERE m.model_id = '" + DefaultModelId + "' AND m.is_enabled AND NOT m.is_deleted " +
            "AND p.is_enabled AND NOT p.is_deleted " +
            "AND (NOT p.requires_api_key OR COALESCE(p.api_key, '') <> '')";

        public static void Apply(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "INSERT INTO settings (id, type, value) " +
                "SELECT gen_random_uuid(), '" + SettingType + "', '" + DefaultModelId + "' " +
                "WHERE NOT EXISTS (SELECT 1 FROM settings WHERE type = '" + SettingType + "') " +
                "AND EXISTS (" + DefaultModelCallable + ")");

            migrationBuilder.Sql(
                "UPDATE settings SET value = '" + DefaultModelId + "' " +
                "WHERE type = '" + SettingType + "' AND value = '" + LegacyDefaultModelId + "' " +
                "AND EXISTS (" + DefaultModelEnabled + ")");
        }
    }
}
