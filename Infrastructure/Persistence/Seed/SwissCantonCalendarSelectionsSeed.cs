using Klacks.Api.Domain.Models.CalendarSelections;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class SwissCantonCalendarSelectionsSeed
    {
        public static readonly Dictionary<string, Guid> CalendarSelectionIds = new Dictionary<string, Guid>
        {
            { "AG", Guid.Parse("a1111111-1111-1111-1111-111111111111") },
            { "AI", Guid.Parse("a2222222-2222-2222-2222-222222222222") },
            { "AR", Guid.Parse("a3333333-3333-3333-3333-333333333333") },
            { "BE", Guid.Parse("a4444444-4444-4444-4444-444444444444") },
            { "BL", Guid.Parse("a5555555-5555-5555-5555-555555555555") },
            { "BS", Guid.Parse("a6666666-6666-6666-6666-666666666666") },
            { "FR", Guid.Parse("a7777777-7777-7777-7777-777777777777") },
            { "GE", Guid.Parse("a8888888-8888-8888-8888-888888888888") },
            { "GL", Guid.Parse("a9999999-9999-9999-9999-999999999999") },
            { "GR", Guid.Parse("b1111111-1111-1111-1111-111111111111") },
            { "JU", Guid.Parse("b2222222-2222-2222-2222-222222222222") },
            { "LU", Guid.Parse("b3333333-3333-3333-3333-333333333333") },
            { "NE", Guid.Parse("b4444444-4444-4444-4444-444444444444") },
            { "NW", Guid.Parse("b5555555-5555-5555-5555-555555555555") },
            { "OW", Guid.Parse("b6666666-6666-6666-6666-666666666666") },
            { "SG", Guid.Parse("b7777777-7777-7777-7777-777777777777") },
            { "SH", Guid.Parse("b8888888-8888-8888-8888-888888888888") },
            { "SO", Guid.Parse("b9999999-9999-9999-9999-999999999999") },
            { "SZ", Guid.Parse("c1111111-1111-1111-1111-111111111111") },
            { "TG", Guid.Parse("c2222222-2222-2222-2222-222222222222") },
            { "TI", Guid.Parse("c3333333-3333-3333-3333-333333333333") },
            { "UR", Guid.Parse("c4444444-4444-4444-4444-444444444444") },
            { "VD", Guid.Parse("c5555555-5555-5555-5555-555555555555") },
            { "VS", Guid.Parse("c6666666-6666-6666-6666-666666666666") },
            { "ZG", Guid.Parse("c7777777-7777-7777-7777-777777777777") },
            { "ZH", Guid.Parse("c8888888-8888-8888-8888-888888888888") }
        };

        public static void SeedCalendarSelections(MigrationBuilder migrationBuilder)
        {
            var calendarSelections = GetSwissCantonCalendarSelections();
            var selectedCalendars = GetSwissCantonSelectedCalendars(calendarSelections);

            var calendarSelectionsScript = GenerateInsertScriptForCalendarSelections(calendarSelections);
            var selectedCalendarsScript = GenerateInsertScriptForSelectedCalendars(selectedCalendars);

            migrationBuilder.Sql(calendarSelectionsScript);
            migrationBuilder.Sql(selectedCalendarsScript);
        }

        public static readonly Dictionary<string, string> SwissCantons = new Dictionary<string, string>
        {
            { "AG", "Kanton Aargau" },
            { "AI", "Kanton Appenzell Innerrhoden" },
            { "AR", "Kanton Appenzell Ausserrhoden" },
            { "BE", "Kanton Bern" },
            { "BL", "Kanton Basel-Landschaft" },
            { "BS", "Kanton Basel-Stadt" },
            { "FR", "Kanton Freiburg" },
            { "GE", "Kanton Genf" },
            { "GL", "Kanton Glarus" },
            { "GR", "Kanton Graubünden" },
            { "JU", "Kanton Jura" },
            { "LU", "Kanton Luzern" },
            { "NE", "Kanton Neuenburg" },
            { "NW", "Kanton Nidwalden" },
            { "OW", "Kanton Obwalden" },
            { "SG", "Kanton St. Gallen" },
            { "SH", "Kanton Schaffhausen" },
            { "SO", "Kanton Solothurn" },
            { "SZ", "Kanton Schwyz" },
            { "TG", "Kanton Thurgau" },
            { "TI", "Kanton Tessin" },
            { "UR", "Kanton Uri" },
            { "VD", "Kanton Waadt" },
            { "VS", "Kanton Wallis" },
            { "ZG", "Kanton Zug" },
            { "ZH", "Kanton Zürich" }
        };

        private static List<CalendarSelection> GetSwissCantonCalendarSelections()
        {
            var calendarSelections = new List<CalendarSelection>();
            var now = DateTime.UtcNow;

            foreach (var canton in SwissCantons)
            {
                var id = CalendarSelectionIds[canton.Key]; 
                calendarSelections.Add(new CalendarSelection
                {
                    Id = id,
                    Name = canton.Value,
                    CreateTime = now,
                    UpdateTime = now,
                    IsDeleted = false,
                    CurrentUserCreated = "System",
                    CurrentUserUpdated = "System"
                });
            }

            return calendarSelections;
        }

        private static List<SelectedCalendar> GetSwissCantonSelectedCalendars(List<CalendarSelection> calendarSelections)
        {
            var cantonNameToAbbreviation = SwissCantons.ToDictionary(x => x.Value, x => x.Key);

            var selectedCalendars = new List<SelectedCalendar>();
            var now = DateTime.UtcNow;

            foreach (var calendarSelection in calendarSelections)
            {
                if (cantonNameToAbbreviation.TryGetValue(calendarSelection.Name, out var abbreviation))
                {
                    selectedCalendars.Add(new SelectedCalendar
                    {
                        Id = Guid.NewGuid(),
                        CalendarSelectionId = calendarSelection.Id,
                        Country = "CH",
                        State = "CH",
                        CreateTime = now,
                        UpdateTime = now,
                        IsDeleted = false,
                        CurrentUserCreated = "System",
                        CurrentUserUpdated = "System"
                    });

                    selectedCalendars.Add(new SelectedCalendar
                    {
                        Id = Guid.NewGuid(),
                        CalendarSelectionId = calendarSelection.Id,
                        Country = "CH",
                        State = abbreviation,
                        CreateTime = now,
                        UpdateTime = now,
                        IsDeleted = false,
                        CurrentUserCreated = "System",
                        CurrentUserUpdated = "System"
                    });
                }
            }

            return selectedCalendars;
        }

        private static string GenerateInsertScriptForCalendarSelections(List<CalendarSelection> calendarSelections)
        {
            var script = "INSERT INTO calendar_selection (id, name, create_time, update_time, is_deleted, current_user_created, current_user_updated) VALUES ";
            var values = new List<string>();

            foreach (var selection in calendarSelections)
            {
                values.Add($"('{selection.Id}', '{selection.Name}', '{selection.CreateTime:yyyy-MM-dd HH:mm:ss}', '{selection.UpdateTime:yyyy-MM-dd HH:mm:ss}', {selection.IsDeleted.ToString().ToLower()}, '{selection.CurrentUserCreated}', '{selection.CurrentUserUpdated}')");
            }

            return script + string.Join(", ", values) + ";";
        }

        private static string GenerateInsertScriptForSelectedCalendars(List<SelectedCalendar> selectedCalendars)
        {
            var script = "INSERT INTO selected_calendar (id, calendar_selection_id, country, state, create_time, update_time, is_deleted, current_user_created, current_user_updated) VALUES ";
            var values = new List<string>();

            foreach (var calendar in selectedCalendars)
            {
                values.Add($"('{calendar.Id}', '{calendar.CalendarSelectionId}', '{calendar.Country}', '{calendar.State}', '{calendar.CreateTime:yyyy-MM-dd HH:mm:ss}', '{calendar.UpdateTime:yyyy-MM-dd HH:mm:ss}', {calendar.IsDeleted.ToString().ToLower()}, '{calendar.CurrentUserCreated}', '{calendar.CurrentUserUpdated}')");
            }

            return script + string.Join(", ", values) + ";";
        }
    }
}