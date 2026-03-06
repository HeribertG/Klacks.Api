// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class AdditionalCalendarRulesSeed
    {
        private const string EmptyDesc = @"'{""de"":"""",""en"":"""",""fr"":"""",""it"":""""}'";
        private const string InsertPrefix = "INSERT INTO public.calendar_rule (id,rule,sub_rule,is_mandatory,is_paid,state,country,description,name) VALUES";

        public static void SeedData(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(GetAustriaRegionalRules());
            migrationBuilder.Sql(GetGermanyMissingRules());
            migrationBuilder.Sql(GetLiechtensteinMissingRules());
            migrationBuilder.Sql(GetItalyAdditionalRules());
            migrationBuilder.Sql(GetUSAJuneteenthRule());
            migrationBuilder.Sql(GetUSAGoodFridayRules());
            migrationBuilder.Sql(GetUSADayAfterThanksgivingRules());
            migrationBuilder.Sql(GetUSAChristmasEveRules());
            migrationBuilder.Sql(GetUSADayAfterChristmasRules());
            migrationBuilder.Sql(GetUSAPatriotsDayRules());
            migrationBuilder.Sql(GetUSAMardiGrasRules());
            migrationBuilder.Sql(GetUSAConfederateMemorialDayRules());
            migrationBuilder.Sql(GetUSAJeffersonDavisBirthdayRules());
            migrationBuilder.Sql(GetUSAHawaiiRules());
            migrationBuilder.Sql(GetUSAAlaskaRules());
            migrationBuilder.Sql(GetUSAVermontRules());
            migrationBuilder.Sql(GetUSANevadaDayRules());
            migrationBuilder.Sql(GetUSAWestVirginiaDayRules());
            migrationBuilder.Sql(GetUSALincolnBirthdayRules());
            migrationBuilder.Sql(GetUSATrumanDayRules());
            migrationBuilder.Sql(GetUSANewYearsEveRules());
        }

        private static string Esc(string s) => s.Replace("'", "''");

        private static string NameJson(string de, string en, string fr, string it) =>
            $"'{{\"de\":\"{Esc(de)}\",\"en\":\"{Esc(en)}\",\"fr\":\"{Esc(fr)}\",\"it\":\"{Esc(it)}\"}}'";

        private static string Rule(string id, string rule, string subRule, bool mandatory, bool paid, string state, string country, string de, string en, string fr, string it) =>
            $"('{id}', '{rule}', '{subRule}', {mandatory.ToString().ToLower()}, {paid.ToString().ToLower()}, '{state}', '{country}', {EmptyDesc}, {NameJson(de, en, fr, it)})";

        private static string BuildSql(IEnumerable<string> rules) =>
            $"{InsertPrefix}\n{string.Join(",\n", rules)};";

        private static string BuildStateRules(string idPrefix, string rule, string subRule, bool mandatory, bool paid, string country, string[] states, string de, string en, string fr, string it) =>
            BuildSql(states.Select((s, i) => Rule($"{idPrefix}-0001-{(i + 1):D12}", rule, subRule, mandatory, paid, s, country, de, en, fr, it)));

        private static string GetAustriaRegionalRules() => BuildSql(new[]
        {
            Rule("a00b0001-0001-0001-0001-000000000001", "03/19", "", false, false, "K", "AT", "Josefitag", "Saint Joseph's Day", "Fête de la Saint-Joseph", "Festa di San Giuseppe"),
            Rule("a00b0001-0001-0001-0001-000000000002", "03/19", "", false, false, "ST", "AT", "Josefitag", "Saint Joseph's Day", "Fête de la Saint-Joseph", "Festa di San Giuseppe"),
            Rule("a00b0001-0001-0001-0001-000000000003", "03/19", "", false, false, "T", "AT", "Josefitag", "Saint Joseph's Day", "Fête de la Saint-Joseph", "Festa di San Giuseppe"),
            Rule("a00b0001-0001-0001-0001-000000000004", "03/19", "", false, false, "V", "AT", "Josefitag", "Saint Joseph's Day", "Fête de la Saint-Joseph", "Festa di San Giuseppe"),
            Rule("a00c0001-0001-0001-0001-000000000001", "05/04", "", false, false, "OÖ", "AT", "Florianstag", "Saint Florian's Day", "Fête de Saint-Florian", "Festa di San Floriano"),
            Rule("a00d0001-0001-0001-0001-000000000001", "09/24", "", false, false, "S", "AT", "Rupertitag", "Saint Rupert's Day", "Fête de Saint-Rupert", "Festa di San Ruperto"),
            Rule("a00e0001-0001-0001-0001-000000000001", "10/10", "", false, false, "K", "AT", "Tag der Volksabstimmung", "Carinthian Plebiscite Day", "Jour du plébiscite de Carinthie", "Giorno del plebiscito della Carinzia"),
            Rule("a00f0001-0001-0001-0001-000000000001", "11/11", "", false, false, "B", "AT", "Martinstag", "Saint Martin's Day", "Fête de la Saint-Martin", "Festa di San Martino"),
            Rule("a00a0001-0001-0001-0001-000000000001", "11/15", "", false, false, "W", "AT", "Leopolditag", "Saint Leopold's Day", "Fête de Saint-Léopold", "Festa di San Leopoldo"),
            Rule("a00a0001-0001-0001-0001-000000000002", "11/15", "", false, false, "NÖ", "AT", "Leopolditag", "Saint Leopold's Day", "Fête de Saint-Léopold", "Festa di San Leopoldo")
        });

        private static string GetGermanyMissingRules() => BuildSql(new[]
        {
            Rule("de0e0001-0001-0001-0001-000000000001", "EASTER", "", true, true, "BB", "DE", "Ostersonntag", "Easter Sunday", "Dimanche de Pâques", "Domenica di Pasqua"),
            Rule("de0f0001-0001-0001-0001-000000000001", "EASTER+49", "", true, true, "BB", "DE", "Pfingstsonntag", "Whit Sunday", "Dimanche de Pentecôte", "Domenica di Pentecoste")
        });

        private static string GetLiechtensteinMissingRules() => BuildSql(new[]
        {
            Rule("01000001-0001-0001-0001-000000000019", "EASTER", "", true, true, "LI", "LI", "Ostersonntag", "Easter Sunday", "Dimanche de Pâques", "Domenica di Pasqua"),
            Rule("01000001-0001-0001-0001-000000000020", "EASTER+49", "", true, true, "LI", "LI", "Pfingstsonntag", "Whit Sunday", "Dimanche de Pentecôte", "Domenica di Pentecoste")
        });

        private static string GetItalyAdditionalRules() => BuildSql(new[]
        {
            Rule("10000001-0001-0001-0001-000000000015", "10/04", "", true, true, "IT", "IT", "San Francesco d'Assisi", "Feast of Saint Francis of Assisi", "Saint François d'Assise", "San Francesco d'Assisi"),
            Rule("100aa001-0001-0001-0001-000000000001", "EASTER+50", "", true, true, "TAA", "IT", "Pfingstmontag", "Whit Monday", "Lundi de Pentecôte", "Lunedì di Pentecoste"),
            Rule("100bb001-0001-0001-0001-000000000001", "04/28", "", false, false, "SAR", "IT", "Tag des sardischen Volkes", "Sardinia Day", "Jour du peuple sarde", "Sa Die de sa Sardigna")
        });

        private static string GetUSAJuneteenthRule() => BuildSql(new[]
        {
            Rule("05a00001-0001-0001-0001-000000000011", "06/19", "SA-1;SU+1", true, true, "USA", "USA", "Juneteenth", "Juneteenth National Independence Day", "Juneteenth", "Juneteenth")
        });

        private static string GetUSAGoodFridayRules() =>
            BuildStateRules("05b00001-0001-0001", "EASTER-02", "", true, true, "USA",
                new[] { "CT", "DE", "FL", "HI", "IN", "KY", "LA", "NC", "ND", "NJ", "TN", "TX" },
                "Karfreitag", "Good Friday", "Vendredi saint", "Venerdì Santo");

        private static string GetUSADayAfterThanksgivingRules() =>
            BuildStateRules("05c00001-0001-0001", "11/01+22+FR", "", true, true, "USA",
                new[] { "CA", "DE", "FL", "GA", "IA", "IL", "KS", "KY", "LA", "ME", "MI", "MN", "NE", "NH", "NC", "NV", "OK", "PA", "SC", "TN", "TX", "VA", "WV" },
                "Tag nach Thanksgiving", "Day after Thanksgiving", "Lendemain de Thanksgiving", "Giorno dopo il Ringraziamento");

        private static string GetUSAChristmasEveRules() =>
            BuildStateRules("05d00001-0001-0001", "12/24", "SA-1;SU+1", true, true, "USA",
                new[] { "AR", "KY", "MI", "NC", "OK", "SC", "TN", "TX", "WV", "WI" },
                "Heiligabend", "Christmas Eve", "Veille de Noël", "Vigilia di Natale");

        private static string GetUSADayAfterChristmasRules() =>
            BuildStateRules("05e00001-0001-0001", "12/26", "SA-1;SU+1", true, true, "USA",
                new[] { "KY", "NC", "SC", "TN", "TX", "VA" },
                "Tag nach Weihnachten", "Day after Christmas", "Lendemain de Noël", "Giorno dopo Natale");

        private static string GetUSAPatriotsDayRules() => BuildSql(new[]
        {
            Rule("05f00001-0001-0001-0001-000000000001", "04/01+14+MO", "", true, true, "MA", "USA", "Tag der Patrioten", "Patriots' Day", "Jour des Patriotes", "Giorno dei Patrioti"),
            Rule("05f00001-0001-0001-0001-000000000002", "04/01+14+MO", "", true, true, "ME", "USA", "Tag der Patrioten", "Patriots' Day", "Jour des Patriotes", "Giorno dei Patrioti")
        });

        private static string GetUSAMardiGrasRules() => BuildSql(new[]
        {
            Rule("05fa0001-0001-0001-0001-000000000001", "EASTER-47", "", true, true, "LA", "USA", "Fasching", "Mardi Gras", "Mardi Gras", "Martedì Grasso")
        });

        private static string GetUSAConfederateMemorialDayRules() => BuildSql(new[]
        {
            Rule("05fb0001-0001-0001-0001-000000000001", "04/01+21+MO", "", true, true, "AL", "USA", "Gedenktag der Konföderierten", "Confederate Memorial Day", "Jour commémoratif confédéré", "Giorno commemorativo confederato"),
            Rule("05fb0001-0001-0001-0001-000000000002", "04/01+21+MO", "", true, true, "MS", "USA", "Gedenktag der Konföderierten", "Confederate Memorial Day", "Jour commémoratif confédéré", "Giorno commemorativo confederato"),
            Rule("05fb0001-0001-0001-0001-000000000003", "05/10", "SA-1;SU+1", true, true, "SC", "USA", "Gedenktag der Konföderierten", "Confederate Memorial Day", "Jour commémoratif confédéré", "Giorno commemorativo confederato")
        });

        private static string GetUSAJeffersonDavisBirthdayRules() => BuildSql(new[]
        {
            Rule("05fc0001-0001-0001-0001-000000000001", "06/01+00+MO", "", true, true, "AL", "USA", "Geburtstag von Jefferson Davis", "Jefferson Davis' Birthday", "Anniversaire de Jefferson Davis", "Compleanno di Jefferson Davis")
        });

        private static string GetUSAHawaiiRules() => BuildSql(new[]
        {
            Rule("05fd0001-0001-0001-0001-000000000001", "03/26", "SA-1;SU+1", true, true, "HI", "USA", "Prinz-Kuhio-Tag", "Prince Kuhio Day", "Jour du Prince Kuhio", "Giorno del Principe Kuhio"),
            Rule("05fd0001-0001-0001-0001-000000000002", "06/11", "SA-1;SU+1", true, true, "HI", "USA", "King-Kamehameha-Tag", "King Kamehameha Day", "Jour du Roi Kamehameha", "Giorno del Re Kamehameha"),
            Rule("05fd0001-0001-0001-0001-000000000003", "08/01+14+FR", "", true, true, "HI", "USA", "Tag der Eigenstaatlichkeit", "Statehood Day", "Jour de l'admission", "Giorno dell'ammissione")
        });

        private static string GetUSAAlaskaRules() => BuildSql(new[]
        {
            Rule("05fe0001-0001-0001-0001-000000000001", "03/25+00+MO", "", true, true, "AK", "USA", "Seward-Tag", "Seward's Day", "Jour de Seward", "Giorno di Seward"),
            Rule("05fe0001-0001-0001-0001-000000000002", "10/18", "SA-1;SU+1", true, true, "AK", "USA", "Alaska-Tag", "Alaska Day", "Jour de l'Alaska", "Giorno dell'Alaska")
        });

        private static string GetUSAVermontRules() => BuildSql(new[]
        {
            Rule("05ff0001-0001-0001-0001-000000000001", "03/01+00+TU", "", true, true, "VT", "USA", "Gemeindeversammlungstag", "Town Meeting Day", "Jour de l'assemblée communale", "Giorno dell'assemblea comunale"),
            Rule("05ff0001-0001-0001-0001-000000000002", "08/16", "SA-1;SU+1", true, true, "VT", "USA", "Tag der Schlacht von Bennington", "Bennington Battle Day", "Jour de la bataille de Bennington", "Giorno della battaglia di Bennington")
        });

        private static string GetUSANevadaDayRules() => BuildSql(new[]
        {
            Rule("050a0001-0001-0001-0001-000000000001", "10/25+00+FR", "", true, true, "NV", "USA", "Nevada-Tag", "Nevada Day", "Jour du Nevada", "Giorno del Nevada")
        });

        private static string GetUSAWestVirginiaDayRules() => BuildSql(new[]
        {
            Rule("050b0001-0001-0001-0001-000000000001", "06/20", "SA-1;SU+1", true, true, "WV", "USA", "West-Virginia-Tag", "West Virginia Day", "Jour de la Virginie-Occidentale", "Giorno della Virginia Occidentale")
        });

        private static string GetUSALincolnBirthdayRules() => BuildSql(new[]
        {
            Rule("050c0001-0001-0001-0001-000000000001", "02/12", "SA-1;SU+1", true, true, "CT", "USA", "Lincolns Geburtstag", "Lincoln's Birthday", "Anniversaire de Lincoln", "Compleanno di Lincoln"),
            Rule("050c0001-0001-0001-0001-000000000002", "02/12", "SA-1;SU+1", true, true, "IL", "USA", "Lincolns Geburtstag", "Lincoln's Birthday", "Anniversaire de Lincoln", "Compleanno di Lincoln")
        });

        private static string GetUSATrumanDayRules() => BuildSql(new[]
        {
            Rule("050d0001-0001-0001-0001-000000000001", "05/08", "SA-1;SU+1", true, true, "MO", "USA", "Truman-Tag", "Truman Day", "Jour de Truman", "Giorno di Truman")
        });

        private static string GetUSANewYearsEveRules() => BuildSql(new[]
        {
            Rule("050e0001-0001-0001-0001-000000000001", "12/31", "SA-1;SU+1", true, true, "KY", "USA", "Silvester", "New Year's Eve", "Saint-Sylvestre", "Vigilia di Capodanno")
        });
    }
}
