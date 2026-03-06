// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.CalendarSelections;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class CountryCalendarSelectionsSeed
    {
        public static readonly Dictionary<string, Guid> CalendarSelectionIds = new()
        {
            { "AT-B", Guid.Parse("a0a00001-0001-0001-0001-000000000001") },
            { "AT-K", Guid.Parse("a0a00001-0001-0001-0001-000000000002") },
            { "AT-NÖ", Guid.Parse("a0a00001-0001-0001-0001-000000000003") },
            { "AT-OÖ", Guid.Parse("a0a00001-0001-0001-0001-000000000004") },
            { "AT-S", Guid.Parse("a0a00001-0001-0001-0001-000000000005") },
            { "AT-ST", Guid.Parse("a0a00001-0001-0001-0001-000000000006") },
            { "AT-T", Guid.Parse("a0a00001-0001-0001-0001-000000000007") },
            { "AT-V", Guid.Parse("a0a00001-0001-0001-0001-000000000008") },
            { "AT-W", Guid.Parse("a0a00001-0001-0001-0001-000000000009") },

            { "USA-AL", Guid.Parse("a0b00001-0001-0001-0001-000000000001") },
            { "USA-AK", Guid.Parse("a0b00001-0001-0001-0001-000000000002") },
            { "USA-AZ", Guid.Parse("a0b00001-0001-0001-0001-000000000003") },
            { "USA-AR", Guid.Parse("a0b00001-0001-0001-0001-000000000004") },
            { "USA-CA", Guid.Parse("a0b00001-0001-0001-0001-000000000005") },
            { "USA-CO", Guid.Parse("a0b00001-0001-0001-0001-000000000006") },
            { "USA-CT", Guid.Parse("a0b00001-0001-0001-0001-000000000007") },
            { "USA-DE", Guid.Parse("a0b00001-0001-0001-0001-000000000008") },
            { "USA-FL", Guid.Parse("a0b00001-0001-0001-0001-000000000009") },
            { "USA-GA", Guid.Parse("a0b00001-0001-0001-0001-000000000010") },
            { "USA-HI", Guid.Parse("a0b00001-0001-0001-0001-000000000011") },
            { "USA-ID", Guid.Parse("a0b00001-0001-0001-0001-000000000012") },
            { "USA-IL", Guid.Parse("a0b00001-0001-0001-0001-000000000013") },
            { "USA-IN", Guid.Parse("a0b00001-0001-0001-0001-000000000014") },
            { "USA-IA", Guid.Parse("a0b00001-0001-0001-0001-000000000015") },
            { "USA-KS", Guid.Parse("a0b00001-0001-0001-0001-000000000016") },
            { "USA-KY", Guid.Parse("a0b00001-0001-0001-0001-000000000017") },
            { "USA-LA", Guid.Parse("a0b00001-0001-0001-0001-000000000018") },
            { "USA-ME", Guid.Parse("a0b00001-0001-0001-0001-000000000019") },
            { "USA-MD", Guid.Parse("a0b00001-0001-0001-0001-000000000020") },
            { "USA-MA", Guid.Parse("a0b00001-0001-0001-0001-000000000021") },
            { "USA-MI", Guid.Parse("a0b00001-0001-0001-0001-000000000022") },
            { "USA-MN", Guid.Parse("a0b00001-0001-0001-0001-000000000023") },
            { "USA-MS", Guid.Parse("a0b00001-0001-0001-0001-000000000024") },
            { "USA-MO", Guid.Parse("a0b00001-0001-0001-0001-000000000025") },
            { "USA-MT", Guid.Parse("a0b00001-0001-0001-0001-000000000026") },
            { "USA-NE", Guid.Parse("a0b00001-0001-0001-0001-000000000027") },
            { "USA-NV", Guid.Parse("a0b00001-0001-0001-0001-000000000028") },
            { "USA-NH", Guid.Parse("a0b00001-0001-0001-0001-000000000029") },
            { "USA-NJ", Guid.Parse("a0b00001-0001-0001-0001-000000000030") },
            { "USA-NM", Guid.Parse("a0b00001-0001-0001-0001-000000000031") },
            { "USA-NY", Guid.Parse("a0b00001-0001-0001-0001-000000000032") },
            { "USA-NC", Guid.Parse("a0b00001-0001-0001-0001-000000000033") },
            { "USA-ND", Guid.Parse("a0b00001-0001-0001-0001-000000000034") },
            { "USA-OH", Guid.Parse("a0b00001-0001-0001-0001-000000000035") },
            { "USA-OK", Guid.Parse("a0b00001-0001-0001-0001-000000000036") },
            { "USA-OR", Guid.Parse("a0b00001-0001-0001-0001-000000000037") },
            { "USA-PA", Guid.Parse("a0b00001-0001-0001-0001-000000000038") },
            { "USA-RI", Guid.Parse("a0b00001-0001-0001-0001-000000000039") },
            { "USA-SC", Guid.Parse("a0b00001-0001-0001-0001-000000000040") },
            { "USA-SD", Guid.Parse("a0b00001-0001-0001-0001-000000000041") },
            { "USA-TN", Guid.Parse("a0b00001-0001-0001-0001-000000000042") },
            { "USA-TX", Guid.Parse("a0b00001-0001-0001-0001-000000000043") },
            { "USA-UT", Guid.Parse("a0b00001-0001-0001-0001-000000000044") },
            { "USA-VT", Guid.Parse("a0b00001-0001-0001-0001-000000000045") },
            { "USA-VA", Guid.Parse("a0b00001-0001-0001-0001-000000000046") },
            { "USA-WA", Guid.Parse("a0b00001-0001-0001-0001-000000000047") },
            { "USA-WV", Guid.Parse("a0b00001-0001-0001-0001-000000000048") },
            { "USA-WI", Guid.Parse("a0b00001-0001-0001-0001-000000000049") },
            { "USA-WY", Guid.Parse("a0b00001-0001-0001-0001-000000000050") },

            { "DE-BW", Guid.Parse("d3333333-3333-3333-3333-333333333333") },
            { "DE-BY", Guid.Parse("d4444444-4444-4444-4444-444444444444") },
            { "DE-BE", Guid.Parse("d5555555-5555-5555-5555-555555555555") },
            { "DE-BB", Guid.Parse("d6666666-6666-6666-6666-666666666666") },
            { "DE-HB", Guid.Parse("d7777777-7777-7777-7777-777777777777") },
            { "DE-HH", Guid.Parse("d8888888-8888-8888-8888-888888888888") },
            { "DE-HE", Guid.Parse("d9999999-9999-9999-9999-999999999999") },
            { "DE-MV", Guid.Parse("e1111111-1111-1111-1111-111111111111") },
            { "DE-NI", Guid.Parse("e2222222-2222-2222-2222-222222222222") },
            { "DE-NW", Guid.Parse("e3333333-3333-3333-3333-333333333333") },
            { "DE-RP", Guid.Parse("e4444444-4444-4444-4444-444444444444") },
            { "DE-SL", Guid.Parse("e5555555-5555-5555-5555-555555555555") },
            { "DE-SN", Guid.Parse("e6666666-6666-6666-6666-666666666666") },
            { "DE-ST", Guid.Parse("e7777777-7777-7777-7777-777777777777") },
            { "DE-SH", Guid.Parse("e8888888-8888-8888-8888-888888888888") },
            { "DE-TH", Guid.Parse("e9999999-9999-9999-9999-999999999999") },

            { "IT-ABR", Guid.Parse("a0c00001-0001-0001-0001-000000000001") },
            { "IT-BAS", Guid.Parse("a0c00001-0001-0001-0001-000000000002") },
            { "IT-CAL", Guid.Parse("a0c00001-0001-0001-0001-000000000003") },
            { "IT-CAM", Guid.Parse("a0c00001-0001-0001-0001-000000000004") },
            { "IT-EMR", Guid.Parse("a0c00001-0001-0001-0001-000000000005") },
            { "IT-FVG", Guid.Parse("a0c00001-0001-0001-0001-000000000006") },
            { "IT-LAZ", Guid.Parse("a0c00001-0001-0001-0001-000000000007") },
            { "IT-LIG", Guid.Parse("a0c00001-0001-0001-0001-000000000008") },
            { "IT-LOM", Guid.Parse("a0c00001-0001-0001-0001-000000000009") },
            { "IT-MAR", Guid.Parse("a0c00001-0001-0001-0001-000000000010") },
            { "IT-MOL", Guid.Parse("a0c00001-0001-0001-0001-000000000011") },
            { "IT-PIE", Guid.Parse("a0c00001-0001-0001-0001-000000000012") },
            { "IT-PUG", Guid.Parse("a0c00001-0001-0001-0001-000000000013") },
            { "IT-SAR", Guid.Parse("a0c00001-0001-0001-0001-000000000014") },
            { "IT-SIC", Guid.Parse("a0c00001-0001-0001-0001-000000000015") },
            { "IT-TAA", Guid.Parse("a0c00001-0001-0001-0001-000000000016") },
            { "IT-TOS", Guid.Parse("a0c00001-0001-0001-0001-000000000017") },
            { "IT-UMB", Guid.Parse("a0c00001-0001-0001-0001-000000000018") },
            { "IT-VDA", Guid.Parse("a0c00001-0001-0001-0001-000000000019") },
            { "IT-VEN", Guid.Parse("a0c00001-0001-0001-0001-000000000020") },

            { "LI", Guid.Parse("f1111111-1111-1111-1111-111111111111") },

            { "FR", Guid.Parse("f3333333-3333-3333-3333-333333333333") },
            { "FR-57", Guid.Parse("f4444444-4444-4444-4444-444444444444") },
            { "FR-67", Guid.Parse("f5555555-5555-5555-5555-555555555555") },
            { "FR-68", Guid.Parse("f6666666-6666-6666-6666-666666666666") }
        };

        private static readonly List<(string Key, string Name, string Country, string State)> SelectionConfig = new()
        {
            ("AT-B", "Burgenland", "AT", "B"),
            ("AT-K", "Kärnten", "AT", "K"),
            ("AT-NÖ", "Niederösterreich", "AT", "NÖ"),
            ("AT-OÖ", "Oberösterreich", "AT", "OÖ"),
            ("AT-S", "Salzburg", "AT", "S"),
            ("AT-ST", "Steiermark", "AT", "ST"),
            ("AT-T", "Tirol", "AT", "T"),
            ("AT-V", "Vorarlberg", "AT", "V"),
            ("AT-W", "Wien", "AT", "W"),

            ("USA-AL", "Alabama", "USA", "AL"),
            ("USA-AK", "Alaska", "USA", "AK"),
            ("USA-AZ", "Arizona", "USA", "AZ"),
            ("USA-AR", "Arkansas", "USA", "AR"),
            ("USA-CA", "California", "USA", "CA"),
            ("USA-CO", "Colorado", "USA", "CO"),
            ("USA-CT", "Connecticut", "USA", "CT"),
            ("USA-DE", "Delaware", "USA", "DE"),
            ("USA-FL", "Florida", "USA", "FL"),
            ("USA-GA", "Georgia", "USA", "GA"),
            ("USA-HI", "Hawaii", "USA", "HI"),
            ("USA-ID", "Idaho", "USA", "ID"),
            ("USA-IL", "Illinois", "USA", "IL"),
            ("USA-IN", "Indiana", "USA", "IN"),
            ("USA-IA", "Iowa", "USA", "IA"),
            ("USA-KS", "Kansas", "USA", "KS"),
            ("USA-KY", "Kentucky", "USA", "KY"),
            ("USA-LA", "Louisiana", "USA", "LA"),
            ("USA-ME", "Maine", "USA", "ME"),
            ("USA-MD", "Maryland", "USA", "MD"),
            ("USA-MA", "Massachusetts", "USA", "MA"),
            ("USA-MI", "Michigan", "USA", "MI"),
            ("USA-MN", "Minnesota", "USA", "MN"),
            ("USA-MS", "Mississippi", "USA", "MS"),
            ("USA-MO", "Missouri", "USA", "MO"),
            ("USA-MT", "Montana", "USA", "MT"),
            ("USA-NE", "Nebraska", "USA", "NE"),
            ("USA-NV", "Nevada", "USA", "NV"),
            ("USA-NH", "New Hampshire", "USA", "NH"),
            ("USA-NJ", "New Jersey", "USA", "NJ"),
            ("USA-NM", "New Mexico", "USA", "NM"),
            ("USA-NY", "New York", "USA", "NY"),
            ("USA-NC", "North Carolina", "USA", "NC"),
            ("USA-ND", "North Dakota", "USA", "ND"),
            ("USA-OH", "Ohio", "USA", "OH"),
            ("USA-OK", "Oklahoma", "USA", "OK"),
            ("USA-OR", "Oregon", "USA", "OR"),
            ("USA-PA", "Pennsylvania", "USA", "PA"),
            ("USA-RI", "Rhode Island", "USA", "RI"),
            ("USA-SC", "South Carolina", "USA", "SC"),
            ("USA-SD", "South Dakota", "USA", "SD"),
            ("USA-TN", "Tennessee", "USA", "TN"),
            ("USA-TX", "Texas", "USA", "TX"),
            ("USA-UT", "Utah", "USA", "UT"),
            ("USA-VT", "Vermont", "USA", "VT"),
            ("USA-VA", "Virginia", "USA", "VA"),
            ("USA-WA", "Washington", "USA", "WA"),
            ("USA-WV", "West Virginia", "USA", "WV"),
            ("USA-WI", "Wisconsin", "USA", "WI"),
            ("USA-WY", "Wyoming", "USA", "WY"),

            ("DE-BW", "Baden-Württemberg", "DE", "BW"),
            ("DE-BY", "Bayern", "DE", "BY"),
            ("DE-BE", "Berlin", "DE", "BE"),
            ("DE-BB", "Brandenburg", "DE", "BB"),
            ("DE-HB", "Bremen", "DE", "HB"),
            ("DE-HH", "Hamburg", "DE", "HH"),
            ("DE-HE", "Hessen", "DE", "HE"),
            ("DE-MV", "Mecklenburg-Vorpommern", "DE", "MV"),
            ("DE-NI", "Niedersachsen", "DE", "NI"),
            ("DE-NW", "Nordrhein-Westfalen", "DE", "NW"),
            ("DE-RP", "Rheinland-Pfalz", "DE", "RP"),
            ("DE-SL", "Saarland", "DE", "SL"),
            ("DE-SN", "Sachsen", "DE", "SN"),
            ("DE-ST", "Sachsen-Anhalt", "DE", "ST"),
            ("DE-SH", "Schleswig-Holstein", "DE", "SH"),
            ("DE-TH", "Thüringen", "DE", "TH"),

            ("IT-ABR", "Abruzzen", "IT", "ABR"),
            ("IT-BAS", "Basilikata", "IT", "BAS"),
            ("IT-CAL", "Kalabrien", "IT", "CAL"),
            ("IT-CAM", "Kampanien", "IT", "CAM"),
            ("IT-EMR", "Emilia-Romagna", "IT", "EMR"),
            ("IT-FVG", "Friaul-Julisch Venetien", "IT", "FVG"),
            ("IT-LAZ", "Latium", "IT", "LAZ"),
            ("IT-LIG", "Ligurien", "IT", "LIG"),
            ("IT-LOM", "Lombardei", "IT", "LOM"),
            ("IT-MAR", "Marken", "IT", "MAR"),
            ("IT-MOL", "Molise", "IT", "MOL"),
            ("IT-PIE", "Piemont", "IT", "PIE"),
            ("IT-PUG", "Apulien", "IT", "PUG"),
            ("IT-SAR", "Sardinien", "IT", "SAR"),
            ("IT-SIC", "Sizilien", "IT", "SIC"),
            ("IT-TAA", "Trentino-Südtirol", "IT", "TAA"),
            ("IT-TOS", "Toskana", "IT", "TOS"),
            ("IT-UMB", "Umbrien", "IT", "UMB"),
            ("IT-VDA", "Aostatal", "IT", "VDA"),
            ("IT-VEN", "Venetien", "IT", "VEN"),

            ("LI", "Liechtenstein", "LI", "LI"),

            ("FR", "Frankreich", "FR", "FR"),
            ("FR-57", "Département Moselle", "FR", "57"),
            ("FR-67", "Département Bas-Rhin", "FR", "67"),
            ("FR-68", "Département Haut-Rhin", "FR", "68")
        };

        public static void SeedCalendarSelections(MigrationBuilder migrationBuilder)
        {
            var calendarSelections = GetCalendarSelections();
            var selectedCalendars = GetSelectedCalendars();

            migrationBuilder.Sql(GenerateInsertScriptForCalendarSelections(calendarSelections));
            migrationBuilder.Sql(GenerateInsertScriptForSelectedCalendars(selectedCalendars));
        }

        private static List<CalendarSelection> GetCalendarSelections()
        {
            var now = DateTime.UtcNow;
            return SelectionConfig.Select(c => new CalendarSelection
            {
                Id = CalendarSelectionIds[c.Key],
                Name = c.Name,
                CreateTime = now,
                UpdateTime = now,
                IsDeleted = false,
                CurrentUserCreated = "System",
                CurrentUserUpdated = "System"
            }).ToList();
        }

        private static List<SelectedCalendar> GetSelectedCalendars()
        {
            var selectedCalendars = new List<SelectedCalendar>();
            var now = DateTime.UtcNow;

            foreach (var config in SelectionConfig)
            {
                var calendarSelectionId = CalendarSelectionIds[config.Key];
                var isNationalOnly = config.State == config.Country;

                if (!isNationalOnly)
                {
                    selectedCalendars.Add(new SelectedCalendar
                    {
                        Id = Guid.NewGuid(),
                        CalendarSelectionId = calendarSelectionId,
                        Country = config.Country,
                        State = config.Country,
                        CreateTime = now,
                        UpdateTime = now,
                        IsDeleted = false,
                        CurrentUserCreated = "System",
                        CurrentUserUpdated = "System"
                    });
                }

                selectedCalendars.Add(new SelectedCalendar
                {
                    Id = Guid.NewGuid(),
                    CalendarSelectionId = calendarSelectionId,
                    Country = config.Country,
                    State = config.State,
                    CreateTime = now,
                    UpdateTime = now,
                    IsDeleted = false,
                    CurrentUserCreated = "System",
                    CurrentUserUpdated = "System"
                });
            }

            return selectedCalendars;
        }

        private static string GenerateInsertScriptForCalendarSelections(List<CalendarSelection> calendarSelections)
        {
            var script = "INSERT INTO calendar_selection (id, name, create_time, update_time, is_deleted, current_user_created, current_user_updated) VALUES ";
            var values = calendarSelections.Select(s =>
                $"('{s.Id}', '{s.Name}', '{s.CreateTime:yyyy-MM-dd HH:mm:ss}', '{s.UpdateTime:yyyy-MM-dd HH:mm:ss}', {s.IsDeleted.ToString().ToLower()}, '{s.CurrentUserCreated}', '{s.CurrentUserUpdated}')");
            return script + string.Join(", ", values) + ";";
        }

        private static string GenerateInsertScriptForSelectedCalendars(List<SelectedCalendar> selectedCalendars)
        {
            var script = "INSERT INTO selected_calendar (id, calendar_selection_id, country, state, create_time, update_time, is_deleted, current_user_created, current_user_updated) VALUES ";
            var values = selectedCalendars.Select(c =>
                $"('{c.Id}', '{c.CalendarSelectionId}', '{c.Country}', '{c.State}', '{c.CreateTime:yyyy-MM-dd HH:mm:ss}', '{c.UpdateTime:yyyy-MM-dd HH:mm:ss}', {c.IsDeleted.ToString().ToLower()}, '{c.CurrentUserCreated}', '{c.CurrentUserUpdated}')");
            return script + string.Join(", ", values) + ";";
        }
    }
}
