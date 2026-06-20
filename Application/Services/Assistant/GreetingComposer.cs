// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Composes one warm, grounded opening greeting via a cheap LLM call. Gathers ambient facts
/// (current weather, a public holiday today/tomorrow, and an optional light local note from the
/// existing web search) and lets the LLM weave only the suitable ones into a single localized
/// sentence, under the agent's persona/soul plus a hard groundedness + tone rule. The result is
/// cached once per user/daypart/day. Any failure returns null so the caller falls back to the
/// static template greeting.
/// </summary>

using System.Text;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SettingsConstants = Klacks.Api.Application.Constants.Settings;

namespace Klacks.Api.Application.Services.Assistant;

public class GreetingComposer : IGreetingComposer
{
    private const int CacheHours = 6;
    private const int GreetingMaxTokens = 180;
    private const double GreetingTemperature = 0.7;
    private const int LocalSearchResults = 3;
    private const int LocalNotesUsed = 2;

    private const string TaskInstructions =
        "=== TASK: OPENING GREETING ===\n" +
        "Compose Klacksy's opening greeting for the user. Write ONE short, warm greeting — at most two\n" +
        "sentences — in the language with code '{0}'. Sound like a friendly colleague, not a weather\n" +
        "report. A light, dry touch is welcome per your humor guidance, but never force it; most of the\n" +
        "time a plain warm line is best. Weave in only the facts below that are genuinely light and\n" +
        "pleasant, and do so naturally — do not list them.\n" +
        "HARD RULES:\n" +
        "- Use ONLY the facts provided. Never invent weather, holidays, traffic, events or news.\n" +
        "- If a local note is heavy, negative, tragic, political, or about an accident or crime, DO NOT\n" +
        "  mention it — skip it silently and just greet warmly.\n" +
        "- Address the user by name only if a name is given.\n" +
        "- Output ONLY the greeting text: no quotes, no preamble, no explanation.";

    private readonly ILLMProviderFactory _providerFactory;
    private readonly ILLMRepository _llmRepository;
    private readonly IOpenMeteoClient _weatherClient;
    private readonly IPublicHolidayProvider _holidayProvider;
    private readonly IWebSearchProviderFactory _searchProviderFactory;
    private readonly IIdentityContextProvider _identityProvider;
    private readonly IAgentRepository _agentRepository;
    private readonly ICompanyLocationProvider _companyLocationProvider;
    private readonly ISettingsReader _settingsReader;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GreetingComposer> _logger;

    public GreetingComposer(
        ILLMProviderFactory providerFactory,
        ILLMRepository llmRepository,
        IOpenMeteoClient weatherClient,
        IPublicHolidayProvider holidayProvider,
        IWebSearchProviderFactory searchProviderFactory,
        IIdentityContextProvider identityProvider,
        IAgentRepository agentRepository,
        ICompanyLocationProvider companyLocationProvider,
        ISettingsReader settingsReader,
        IMemoryCache cache,
        ILogger<GreetingComposer> logger)
    {
        _providerFactory = providerFactory;
        _llmRepository = llmRepository;
        _weatherClient = weatherClient;
        _holidayProvider = holidayProvider;
        _searchProviderFactory = searchProviderFactory;
        _identityProvider = identityProvider;
        _agentRepository = agentRepository;
        _companyLocationProvider = companyLocationProvider;
        _settingsReader = settingsReader;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string?> ComposeAsync(GreetingContext context, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"greeting_{context.UserId}_{context.Daypart}_{DateTime.UtcNow:yyyyMMdd}";
        if (_cache.TryGetValue<string>(cacheKey, out var cached) && !string.IsNullOrWhiteSpace(cached))
        {
            return cached;
        }

        try
        {
            var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
            if (agent == null)
            {
                return null;
            }

            var (latitude, longitude) = await ResolveCoordinatesAsync(context, cancellationToken);

            var weatherTask = latitude is not null && longitude is not null
                ? SafeAsync(() => _weatherClient.GetCurrentWeatherAsync(latitude.Value, longitude.Value, 1, cancellationToken))
                : Task.FromResult<WeatherSnapshot?>(null);
            var holidayTask = SafeAsync(() => _holidayProvider.GetUpcomingHolidayAsync(context.CountryCode, cancellationToken));
            var localTask = GetLocalNoteAsync(cancellationToken);

            await Task.WhenAll(weatherTask, holidayTask, localTask);

            var weather = weatherTask.Result;
            var holiday = holidayTask.Result;
            var localNote = localTask.Result;

            if (weather is null && holiday is null && string.IsNullOrWhiteSpace(localNote))
            {
                return null;
            }

            var identityPrompt = await _identityProvider.GetIdentityPromptAsync(agent.Id, context.Language, cancellationToken);
            var systemPrompt = identityPrompt + "\n\n" + string.Format(TaskInstructions, context.Language ?? "en");
            var facts = BuildFacts(context, weather, holiday, localNote);

            var greeting = await CallLlmAsync(systemPrompt, facts, cancellationToken);
            if (string.IsNullOrWhiteSpace(greeting))
            {
                return null;
            }

            greeting = greeting.Trim().Trim('"');
            _cache.Set(cacheKey, greeting, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(CacheHours))
                .SetSize(1));
            return greeting;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Greeting composition failed — falling back to template");
            return null;
        }
    }

    private async Task<(double? Latitude, double? Longitude)> ResolveCoordinatesAsync(
        GreetingContext context, CancellationToken cancellationToken)
    {
        if (context.Latitude is not null && context.Longitude is not null)
        {
            return (context.Latitude, context.Longitude);
        }

        var company = await _companyLocationProvider.GetCompanyLocationAsync(cancellationToken);
        return company is null ? (null, null) : (company.Value.Latitude, company.Value.Longitude);
    }

    private async Task<string?> GetLocalNoteAsync(CancellationToken cancellationToken)
    {
        try
        {
            var city = (await _settingsReader.GetSetting(SettingsConstants.APP_ADDRESS_PLACE))?.Value;
            if (string.IsNullOrWhiteSpace(city))
            {
                return null;
            }

            var provider = await _searchProviderFactory.CreateAsync(cancellationToken);
            if (provider is null)
            {
                return null;
            }

            var result = await provider.SearchAsync($"{city} aktuell Verkehr Veranstaltungen", LocalSearchResults, cancellationToken);
            if (!result.Success || result.Results.Count == 0)
            {
                return null;
            }

            var notes = result.Results
                .Take(LocalNotesUsed)
                .Select(r => $"{r.Title}: {r.Snippet}".Trim());
            return string.Join(" | ", notes);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Local note lookup failed — greeting will skip it");
            return null;
        }
    }

    private static string BuildFacts(
        GreetingContext context, WeatherSnapshot? weather, UpcomingHoliday? holiday, string? localNote)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Facts (use only what fits, write in language code '{context.Language ?? "en"}'):");
        sb.AppendLine($"- Time of day: {context.Daypart}");
        sb.AppendLine(string.IsNullOrWhiteSpace(context.DisplayName)
            ? "- User's name: (unknown — greet without a name)"
            : $"- User's name: {context.DisplayName}");
        if (weather is not null)
        {
            sb.AppendLine($"- Current weather: {weather.Condition}, {Math.Round(weather.TemperatureCelsius)}°C");
        }
        if (holiday is not null)
        {
            sb.AppendLine($"- Public holiday {(holiday.IsToday ? "today" : "tomorrow")}: {holiday.Name}");
        }
        if (!string.IsNullOrWhiteSpace(localNote))
        {
            sb.AppendLine($"- Local note (raw search results, judge if light enough to mention): {localNote}");
        }
        sb.AppendLine();
        sb.AppendLine("Write the greeting now.");
        return sb.ToString();
    }

    private async Task<string?> CallLlmAsync(string systemPrompt, string facts, CancellationToken cancellationToken)
    {
        var models = await _llmRepository.GetModelsAsync(onlyEnabled: true);
        var cheapest = models
            .OrderBy(m => m.CostPerInputToken + m.CostPerOutputToken)
            .FirstOrDefault();
        if (cheapest is null)
        {
            return null;
        }

        var provider = await _providerFactory.GetProviderForModelAsync(cheapest.ModelId);
        if (provider is null)
        {
            return null;
        }

        var request = new LLMProviderRequest
        {
            Message = facts,
            SystemPrompt = systemPrompt,
            ModelId = cheapest.ApiModelId,
            ConversationHistory = [],
            AvailableFunctions = [],
            Temperature = GreetingTemperature,
            MaxTokens = GreetingMaxTokens,
            CostPerInputToken = cheapest.CostPerInputToken,
            CostPerOutputToken = cheapest.CostPerOutputToken
        };

        var response = await provider.ProcessAsync(request);
        return response.Success ? response.Content : null;
    }

    private static async Task<T?> SafeAsync<T>(Func<Task<T?>> action) where T : class
    {
        try
        {
            return await action();
        }
        catch
        {
            return null;
        }
    }
}
