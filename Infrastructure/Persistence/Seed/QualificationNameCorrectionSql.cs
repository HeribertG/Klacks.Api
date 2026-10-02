// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// SQL that repairs the seeded qualification names which older builds shipped with errors: four French names
/// and one Italian name carried two apostrophes where one was meant (the SQL literal had four), and the Italian
/// name of the cold chain qualification was French text. QualificationsSeed inserts with ON CONFLICT DO NOTHING,
/// so existing databases never received the corrected seed values. Each correction rewrites one language key of
/// the name jsonb of one seed row and only while that key still equals the faulty seed text exactly, so a name an
/// administrator changed is never touched and a second run finds nothing. Only the name column is affected; the
/// seed writes no description. The faulty and corrected texts are frozen here on purpose: a migration is history.
/// </summary>
using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class QualificationNameCorrectionSql
    {
        private const string French = "fr";
        private const string Italian = "it";

        public static readonly IReadOnlyList<QualificationNameCorrection> Corrections =
        [
            new("a8bb8129-a47b-4b7b-be2c-4c3b918b19ea", French, "Administration d''injections", "Administration d'injections"),
            new("1190a2fd-c313-457f-b186-3b189ca59344", French, "Sonde d''alimentation (PEG)", "Sonde d'alimentation (PEG)"),
            new("cb3b1969-9c8b-4b51-b9d0-a534d3073f87", French, "Autorisation de port d''arme", "Autorisation de port d'arme"),
            new("cb3b1969-9c8b-4b51-b9d0-a534d3073f87", Italian, "Autorizzazione all''uso di armi da fuoco", "Autorizzazione all'uso di armi da fuoco"),
            new("022a55a6-122e-4b6e-be57-8d9a3a173c75", French, "Contrôle d''accès", "Contrôle d'accès"),
            new("0c562163-931d-4982-bfcc-0388bec2ef9f", Italian, "Gestion de la chaîne du froid", "Gestione della catena del freddo"),
        ];

        private const string QualificationTable = "qualification";

        public static void Apply(MigrationBuilder migrationBuilder)
        {
            foreach (var correction in Corrections)
            {
                migrationBuilder.Sql(BuildStatement(correction));
            }
        }

        public static string BuildStatement(QualificationNameCorrection correction)
        {
            return SeededNameCorrectionSql.BuildStatement(new SeededNameCorrection(
                QualificationTable,
                correction.QualificationId,
                correction.Language,
                correction.FaultyName,
                correction.CorrectedName));
        }
    }
}
