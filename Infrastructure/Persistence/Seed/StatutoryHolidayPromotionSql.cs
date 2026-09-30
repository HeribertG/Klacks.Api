// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// SQL that marks seeded calendar rules as statutory (is_mandatory = true) which older builds seeded as
/// non-statutory: the cantonal Josefstag (NW, SZ, TI, UR, VS), Peter und Paul in TI and the US federal
/// holidays of 5 U.S.C. 6103. The US rows also lose the "UsFederalHoliday" unofficial description, but only
/// while it still carries exactly that generated text, so an administrator's own text is never touched.
/// Up only changes rows still at the shipped is_mandatory = false. Down resets the promoted rows to false and refills
/// that description only where the description is empty; it cannot tell a promoted row from one an administrator
/// had already set to true, so such a row also ends up false.
/// is_paid is left unchanged.
/// </summary>
using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class StatutoryHolidayPromotionSql
    {
        public static readonly IReadOnlyList<string> SwissCantonalHolidayIds =
        [
            "00319001-0001-0001-0001-000000000003",
            "00319001-0001-0001-0001-000000000005",
            "00319001-0001-0001-0001-000000000006",
            "00319001-0001-0001-0001-000000000007",
            "00319001-0001-0001-0001-000000000008",
            "00629001-0001-0001-0001-000000000002",
        ];

        public static readonly IReadOnlyList<string> UsFederalHolidayIds =
        [
            "05a00001-0001-0001-0001-000000000002",
            "05a00001-0001-0001-0001-000000000003",
            "05a00001-0001-0001-0001-000000000004",
            "05a00001-0001-0001-0001-000000000006",
            "05a00001-0001-0001-0001-000000000007",
            "05a00001-0001-0001-0001-000000000008",
        ];

        public static IReadOnlyList<string> PromotedIds => [.. SwissCantonalHolidayIds, .. UsFederalHolidayIds];

        public static void Apply(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"UPDATE public.calendar_rule SET is_mandatory = true WHERE id IN ({CalendarRuleDescriptionSql.IdList(PromotedIds)}) AND is_mandatory = false;");
            migrationBuilder.Sql(
                $"UPDATE public.calendar_rule SET description = '{CalendarRuleDescriptionSql.SeedEmptyDescription}'::jsonb " +
                $"WHERE id IN ({CalendarRuleDescriptionSql.IdList(UsFederalHolidayIds)}) " +
                $"AND description = '{UsFederalHolidayDescriptionLiteral()}'::jsonb;");
        }

        public static void Remove(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"UPDATE public.calendar_rule SET is_mandatory = false WHERE id IN ({CalendarRuleDescriptionSql.IdList(PromotedIds)}) AND is_mandatory = true;");
            migrationBuilder.Sql(
                $"UPDATE public.calendar_rule SET description = '{UsFederalHolidayDescriptionLiteral()}'::jsonb " +
                $"WHERE id IN ({CalendarRuleDescriptionSql.IdList(UsFederalHolidayIds)}) " +
                $"AND {CalendarRuleDescriptionSql.DescriptionIsEmptyPredicate};");
        }

        private static string UsFederalHolidayDescriptionLiteral() =>
            CalendarRuleDescriptionSql.SqlLiteral(
                CalendarRuleDescriptionSql.DescriptionJson(UnofficialHolidayDescriptionTexts.UsFederalHoliday));
    }
}
