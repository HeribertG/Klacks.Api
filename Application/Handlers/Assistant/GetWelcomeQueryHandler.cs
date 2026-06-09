// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds Klacksy's personalized welcome payload from runtime context (user, time, location).
/// Returns only i18n keys and slot values — never localized strings. The frontend resolves
/// the keys via TranslateService.
/// </summary>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class GetWelcomeQueryHandler : IRequestHandler<GetWelcomeQuery, WelcomeResource>
{
    private const int SuggestionCount = 4;
    private const int MorningEndHour = 12;
    private const int AfternoonEndHour = 18;

    private readonly ISuggestionsRanker _suggestionsRanker;
    private readonly IOpenMeteoClient _weatherClient;
    private readonly ICompanyLocationProvider _companyLocationProvider;
    private readonly IOnboardingService _onboardingService;

    public GetWelcomeQueryHandler(
        ISuggestionsRanker suggestionsRanker,
        IOpenMeteoClient weatherClient,
        ICompanyLocationProvider companyLocationProvider,
        IOnboardingService onboardingService)
    {
        _suggestionsRanker = suggestionsRanker;
        _weatherClient = weatherClient;
        _companyLocationProvider = companyLocationProvider;
        _onboardingService = onboardingService;
    }

    public async Task<WelcomeResource> Handle(GetWelcomeQuery request, CancellationToken cancellationToken)
    {
        var greetingKey = string.Empty;
        var weekdayKey = string.Empty;
        var weatherKey = string.Empty;
        int variantIndex;

        if (request.IsReopen)
        {
            variantIndex = PickVariantIndex(request.ExcludeVariantIndex, WelcomeI18nKeys.Reopen.VariantsCount);
            greetingKey = WelcomeI18nKeys.Reopen.Build(variantIndex);
        }
        else
        {
            var daypart = ResolveDaypart(request.LocalHour);
            variantIndex = PickVariantIndex(request.ExcludeVariantIndex, WelcomeI18nKeys.Greeting.VariantsPerDaypart);
            greetingKey = WelcomeI18nKeys.Greeting.Build(daypart, variantIndex);
            weekdayKey = WelcomeI18nKeys.Weekday.Get(request.Weekday);
            weatherKey = await ResolveWeatherKeyAsync(request, cancellationToken);
        }

        var userGuid = Guid.TryParse(request.UserId, out var parsed) ? parsed : Guid.Empty;
        var suggestionKeys = await _suggestionsRanker.RankAsync(
            userGuid,
            request.UserRights,
            request.Route,
            request.LocalHour,
            request.Weekday,
            SuggestionCount,
            cancellationToken);

        var onboarding = await _onboardingService.GetStateAsync(request.UserRights, cancellationToken);

        return new WelcomeResource
        {
            GreetingKey = greetingKey,
            GreetingVariantIndex = variantIndex,
            WeekdayKey = weekdayKey,
            WeatherKey = weatherKey,
            DisplayName = request.DisplayName ?? string.Empty,
            SuggestionKeys = suggestionKeys.ToList(),
            Onboarding = onboarding,
        };
    }

    private static int PickVariantIndex(int? excludeIndex, int total)
    {
        if (total <= 0) return 0;
        if (total == 1) return 0;
        if (excludeIndex is null || excludeIndex < 0 || excludeIndex >= total)
        {
            return Random.Shared.Next(total);
        }

        var picked = Random.Shared.Next(total - 1);
        return picked >= excludeIndex.Value ? picked + 1 : picked;
    }

    private static string ResolveDaypart(int localHour)
    {
        if (localHour < MorningEndHour) return WelcomeI18nKeys.Daypart.Morning;
        if (localHour < AfternoonEndHour) return WelcomeI18nKeys.Daypart.Afternoon;
        return WelcomeI18nKeys.Daypart.Evening;
    }

    private async Task<string> ResolveWeatherKeyAsync(GetWelcomeQuery request, CancellationToken cancellationToken)
    {
        var latitude = request.Latitude;
        var longitude = request.Longitude;

        if (latitude is null || longitude is null)
        {
            var companyLocation = await _companyLocationProvider.GetCompanyLocationAsync(cancellationToken);
            if (companyLocation is null)
            {
                return string.Empty;
            }

            latitude = companyLocation.Value.Latitude;
            longitude = companyLocation.Value.Longitude;
        }

        return await _weatherClient.GetWeatherKeyAsync(
            latitude.Value,
            longitude.Value,
            cancellationToken);
    }
}
