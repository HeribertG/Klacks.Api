// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Centralized i18n key constants for Klacksy's welcome payload. The backend hands these out
/// to the frontend, which resolves them via TranslateService. No localized text lives in C# code.
/// </summary>

namespace Klacks.Api.Application.Constants;

public static class WelcomeI18nKeys
{
    public const string KeyPrefix = "klacksy.welcome";

    public static class Daypart
    {
        public const string Morning = "morning";
        public const string Afternoon = "afternoon";
        public const string Evening = "evening";
    }

    public static class Greeting
    {
        public const int VariantsPerDaypart = 9;

        public static string Build(string daypart, int variantIndex)
            => $"{KeyPrefix}.greeting.{daypart}.{variantIndex}";
    }

    public static class Weather
    {
        public const string Sunny = $"{KeyPrefix}.weather.sunny";
        public const string Clear = $"{KeyPrefix}.weather.clear";
        public const string Cloudy = $"{KeyPrefix}.weather.cloudy";
        public const string Overcast = $"{KeyPrefix}.weather.overcast";
        public const string Fog = $"{KeyPrefix}.weather.fog";
        public const string Drizzle = $"{KeyPrefix}.weather.drizzle";
        public const string Rainy = $"{KeyPrefix}.weather.rainy";
        public const string Snowy = $"{KeyPrefix}.weather.snowy";
        public const string Stormy = $"{KeyPrefix}.weather.stormy";
        public const string Thunder = $"{KeyPrefix}.weather.thunder";
    }

    public static class Weekday
    {
        private static readonly string[] ByIndex =
        {
            "sunday",
            "monday",
            "tuesday",
            "wednesday",
            "thursday",
            "friday",
            "saturday",
        };

        public static string Get(int weekday)
        {
            var idx = ((weekday % 7) + 7) % 7;
            return ByIndex[idx];
        }
    }

    public static class Suggestion
    {
        public const string CreateEmployee = $"{KeyPrefix}.suggestion.create_employee";
        public const string FindPerson = $"{KeyPrefix}.suggestion.find_person";
        public const string ShowHelp = $"{KeyPrefix}.suggestion.show_help";
        public const string ReviewWeek = $"{KeyPrefix}.suggestion.review_week";
        public const string ViewSchedule = $"{KeyPrefix}.suggestion.view_schedule";
        public const string EditSettings = $"{KeyPrefix}.suggestion.edit_settings";
        public const string ManageProviders = $"{KeyPrefix}.suggestion.manage_providers";
        public const string ReviewAbsences = $"{KeyPrefix}.suggestion.review_absences";
        public const string WeekendRecap = $"{KeyPrefix}.suggestion.weekend_recap";
        public const string EndOfDay = $"{KeyPrefix}.suggestion.end_of_day";
    }
}
