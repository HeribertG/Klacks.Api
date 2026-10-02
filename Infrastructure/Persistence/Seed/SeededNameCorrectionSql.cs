// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the guarded UPDATE that repairs one faulty seeded value of a localized name column on existing
/// databases. The seeds insert with ON CONFLICT DO NOTHING, so a corrected seed text never reaches rows that
/// older builds already inserted. The statement rewrites one language key of the name jsonb of one seed row
/// and only while that key still equals the faulty seed text exactly, so a name an administrator changed is
/// never touched, the other language keys (installed language packs) stay as they are and a second run finds
/// nothing.
/// </summary>
using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class SeededNameCorrectionSql
    {
        public static void Apply(MigrationBuilder migrationBuilder, IEnumerable<SeededNameCorrection> corrections)
        {
            foreach (var correction in corrections)
            {
                migrationBuilder.Sql(BuildStatement(correction));
            }
        }

        public static string BuildStatement(SeededNameCorrection correction)
        {
            return $"UPDATE {correction.Table} "
                + $"SET name = jsonb_set(name, '{{{correction.Language}}}', to_jsonb({Literal(correction.CorrectedName)}::text)) "
                + $"WHERE id = '{correction.RowId}' "
                + $"AND name ->> '{correction.Language}' = {Literal(correction.FaultyName)};";
        }

        private static string Literal(string value)
        {
            return "'" + value.Replace("'", "''") + "'";
        }
    }
}
