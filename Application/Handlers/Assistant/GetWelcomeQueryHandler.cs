// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds Klacksy's personalized welcome payload from runtime context (user, time, location).
/// Returns only i18n keys and slot values — never localized strings. The frontend resolves
/// the keys via TranslateService.
/// </summary>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetWelcomeQueryHandler : IRequestHandler<GetWelcomeQuery, WelcomeResource>
{
    private const int SuggestionCount = 4;
    private const int MorningEndHour = 12;
    private const int AfternoonEndHour = 18;

    private readonly ISuggestionsRanker _suggestionsRanker;
    private readonly IOpenMeteoClient _weatherClient;

    public GetWelcomeQueryHandler(
        ISuggestionsRanker suggestionsRanker,
        IOpenMeteoClient weatherClient)
    {
        _suggestionsRanker = suggestionsRanker;
        _weatherClient = weatherClient;
    }

    public async Task<WelcomeResource> Handle(GetWelcomeQuery request, CancellationToken cancellationToken)
    {
        var daypart = ResolveDaypart(request.LocalHour);
        var variantIndex = Random.Shared.Next(WelcomeI18nKeys.Greeting.VariantsPerDaypart);
        var greetingKey = WelcomeI18nKeys.Greeting.Build(daypart, variantIndex);
        var weekdayKey = WelcomeI18nKeys.Weekday.Get(request.Weekday);

        var weatherKey = await ResolveWeatherKeyAsync(request, cancellationToken);

        var userGuid = Guid.TryParse(request.UserId, out var parsed) ? parsed : Guid.Empty;
        var suggestionKeys = await _suggestionsRanker.RankAsync(
            userGuid,
            request.UserRights,
            request.Route,
            request.LocalHour,
            request.Weekday,
            SuggestionCount,
            cancellationToken);

        return new WelcomeResource
        {
            GreetingKey = greetingKey,
            WeekdayKey = weekdayKey,
            WeatherKey = weatherKey,
            DisplayName = request.DisplayName ?? string.Empty,
            SuggestionKeys = suggestionKeys.ToList(),
        };
    }

    private static string ResolveDaypart(int localHour)
    {
        if (localHour < MorningEndHour) return WelcomeI18nKeys.Daypart.Morning;
        if (localHour < AfternoonEndHour) return WelcomeI18nKeys.Daypart.Afternoon;
        return WelcomeI18nKeys.Daypart.Evening;
    }

    private async Task<string> ResolveWeatherKeyAsync(GetWelcomeQuery request, CancellationToken cancellationToken)
    {
        if (request.Latitude is null || request.Longitude is null)
        {
            return string.Empty;
        }

        return await _weatherClient.GetWeatherKeyAsync(
            request.Latitude.Value,
            request.Longitude.Value,
            cancellationToken);
    }
}
