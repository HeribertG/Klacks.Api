using Klacks.Api.Domain.Models.CalendarSelections;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Klacks.Api.Data.Seed
{
    public static class SwissCantonCalendarSeed
    {
        public static void SeedCalendarSelections(MigrationBuilder migrationBuilder)
        {
            var calendarSelections = GetSwissCantonCalendarSelections();
            var selectedCalendars = GetSwissCantonSelectedCalendars(calendarSelections);

            var calendarSelectionsScript = GenerateInsertScriptForCalendarSelections(calendarSelections);
            var selectedCalendarsScript = GenerateInsertScriptForSelectedCalendars(selectedCalendars);

            migrationBuilder.Sql(calendarSelectionsScript);
            migrationBuilder.Sql(selectedCalendarsScript);
        }

        private static List<CalendarSelection> GetSwissCantonCalendarSelections()
        {
            var swissCantons = new Dictionary<string, string>
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

            var calendarSelections = new List<CalendarSelection>();
            var now = DateTime.UtcNow;

            foreach (var canton in swissCantons)
            {
                calendarSelections.Add(new CalendarSelection
                {
                    Id = Guid.NewGuid(),
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
            var cantonAbbreviations = new Dictionary<string, string>
            {
                { "Kanton Aargau", "AG" },
                { "Kanton Appenzell Innerrhoden", "AI" },
                { "Kanton Appenzell Ausserrhoden", "AR" },
                { "Kanton Bern", "BE" },
                { "Kanton Basel-Landschaft", "BL" },
                { "Kanton Basel-Stadt", "BS" },
                { "Kanton Freiburg", "FR" },
                { "Kanton Genf", "GE" },
                { "Kanton Glarus", "GL" },
                { "Kanton Graubünden", "GR" },
                { "Kanton Jura", "JU" },
                { "Kanton Luzern", "LU" },
                { "Kanton Neuenburg", "NE" },
                { "Kanton Nidwalden", "NW" },
                { "Kanton Obwalden", "OW" },
                { "Kanton St. Gallen", "SG" },
                { "Kanton Schaffhausen", "SH" },
                { "Kanton Solothurn", "SO" },
                { "Kanton Schwyz", "SZ" },
                { "Kanton Thurgau", "TG" },
                { "Kanton Tessin", "TI" },
                { "Kanton Uri", "UR" },
                { "Kanton Waadt", "VD" },
                { "Kanton Wallis", "VS" },
                { "Kanton Zug", "ZG" },
                { "Kanton Zürich", "ZH" }
            };

            var selectedCalendars = new List<SelectedCalendar>();
            var now = DateTime.UtcNow;

            foreach (var calendarSelection in calendarSelections)
            {
                if (cantonAbbreviations.TryGetValue(calendarSelection.Name, out var abbreviation))
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