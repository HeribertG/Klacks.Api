// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

/// <summary>
/// Welcome payload for Klacksy. Contains only i18n keys + slot values — no localized strings.
/// The frontend resolves keys via TranslateService with the slots as interpolation parameters.
/// </summary>
public class WelcomeResource
{
    /// <summary>
    /// i18n key of the chosen greeting template, e.g. "klacksy.welcome.greeting.morning.0".
    /// Template uses {{name}}, {{weekday}}, {{weather}} slots.
    /// </summary>
    public string GreetingKey { get; set; } = string.Empty;

    /// <summary>
    /// Fully composed, already-localized greeting sentence from the LLM greeting composer, or null
    /// when the feature is off or composition fell back. When set, the frontend shows this verbatim
    /// instead of resolving GreetingKey + slots.
    /// </summary>
    public string? GreetingText { get; set; }

    /// <summary>
    /// Variant index of the chosen greeting (0..VariantsPerDaypart-1). The FE persists this and
    /// sends it back as ExcludeVariantIndex on the next call to suppress direct repetition.
    /// </summary>
    public int GreetingVariantIndex { get; set; }

    /// <summary>
    /// i18n key for the localized weekday name, e.g. "monday" (reuses existing keys).
    /// </summary>
    public string WeekdayKey { get; set; } = string.Empty;

    /// <summary>
    /// i18n key for the localized weather sentence, e.g. "klacksy.welcome.weather.rainy".
    /// Empty string when weather is unavailable.
    /// </summary>
    public string WeatherKey { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the user. Empty string when unknown.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// i18n key for an optional ambient note appended after the greeting (e.g. a public holiday
    /// today or tomorrow). Empty string when there is nothing notable to mention.
    /// </summary>
    public string AmbientKey { get; set; } = string.Empty;

    /// <summary>
    /// Slot value for the ambient note, currently the localized public-holiday name ({{holiday}}).
    /// Empty string when not applicable.
    /// </summary>
    public string AmbientHolidayName { get; set; } = string.Empty;

    /// <summary>
    /// i18n keys for the suggestion labels, e.g. "klacksy.welcome.suggestion.create_employee".
    /// Already ranked and capped to the desired count.
    /// </summary>
    public List<string> SuggestionKeys { get; set; } = new();

    /// <summary>
    /// Maps each navigation-type suggestion key to its Angular route, e.g.
    /// "klacksy.welcome.suggestion.edit_settings" → "/workplace/settings".
    /// The frontend uses the route as the option value so clicking navigates directly
    /// without going through the LLM.
    /// </summary>
    public Dictionary<string, string> SuggestionRoutes { get; set; } = new();

    /// <summary>
    /// First-run setup-tour state, or null when onboarding is not relevant for this user
    /// (not a fresh install, or the user is not an admin).
    /// </summary>
    public OnboardingResource? Onboarding { get; set; }
}
