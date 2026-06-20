// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves today's/tomorrow's public holiday from the free Nager.Date API (no API key required).
/// The yearly holiday list per country is cached in-memory because it is effectively static for the
/// year, so concurrent welcomes don't spam the upstream API. Any failure yields null — the holiday
/// note is always optional for the greeting.
/// </summary>

namespace Klacks.Api.Infrastructure.Services;

using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Caching.Memory;

public sealed class NagerDateHolidayProvider : IPublicHolidayProvider
{
    public const string HttpClientName = "NagerDate";

    private const string EndpointTemplate = "api/v3/PublicHolidays/{0}/{1}";
    private const int CacheHours = 12;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<NagerDateHolidayProvider> _logger;

    public NagerDateHolidayProvider(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        ILogger<NagerDateHolidayProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<UpcomingHoliday?> GetUpcomingHolidayAsync(
        string countryCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tomorrow = today.AddDays(1);

        var holidays = await GetHolidaysAsync(countryCode, today.Year, cancellationToken);
        if (tomorrow.Year != today.Year)
        {
            holidays = holidays
                .Concat(await GetHolidaysAsync(countryCode, tomorrow.Year, cancellationToken))
                .ToList();
        }

        var todayMatch = holidays.FirstOrDefault(h => h.Date == today);
        if (todayMatch is not null)
        {
            return new UpcomingHoliday(todayMatch.Name, IsToday: true);
        }

        var tomorrowMatch = holidays.FirstOrDefault(h => h.Date == tomorrow);
        if (tomorrowMatch is not null)
        {
            return new UpcomingHoliday(tomorrowMatch.Name, IsToday: false);
        }

        return null;
    }

    private async Task<List<HolidayEntry>> GetHolidaysAsync(
        string countryCode, int year, CancellationToken cancellationToken)
    {
        var cacheKey = $"nager_holidays_{countryCode}_{year}";
        if (_cache.TryGetValue<List<HolidayEntry>>(cacheKey, out var cached) && cached is not null)
        {
            return cached;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            var path = string.Format(EndpointTemplate, year, countryCode);
            var raw = await client.GetFromJsonAsync<List<NagerHoliday>>(path, cancellationToken);

            var entries = (raw ?? [])
                .Where(h => !string.IsNullOrWhiteSpace(h.Date) && !string.IsNullOrWhiteSpace(h.LocalName))
                .Select(h => DateOnly.TryParse(h.Date, out var date)
                    ? new HolidayEntry(date, h.LocalName!)
                    : null)
                .Where(e => e is not null)
                .Select(e => e!)
                .ToList();

            _cache.Set(cacheKey, entries, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(CacheHours))
                .SetSize(1));

            return entries;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Public holiday lookup failed for {Country} {Year}", countryCode, year);
            return [];
        }
    }

    private sealed record HolidayEntry(DateOnly Date, string Name);

    private sealed class NagerHoliday
    {
        [JsonPropertyName("date")]
        public string? Date { get; set; }

        [JsonPropertyName("localName")]
        public string? LocalName { get; set; }
    }
}
