// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the database row a language pack's country, state or calendar rule entry belongs to. A row
/// found by id only counts when its natural key matches the entry; a row whose id another pack claimed
/// first is never touched, the entry then falls back to its natural key. Keys compare exactly (ordinal), as the
/// database does. Soft-deleted rows are returned too, so their id is never reused; each caller decides whether to
/// revive or skip them. A calendar rule without an English name is only matched by id.
/// </summary>
/// <param name="db">Database context the lookups run against</param>
/// <param name="id">Id the pack file ships for the entry</param>
/// <param name="code">Pack code, used for the collision warning only</param>
/// <param name="logger">Logger receiving the collision warning</param>

using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Settings;

public static class LanguagePluginGeoRowResolver
{
    private const string CountryKind = "country";
    private const string StateKind = "state";
    private const string CalendarRuleKind = "calendar rule";
    private const string KeySeparator = "/";

    public static async Task<LanguagePluginGeoRowMatch<Countries>> FindCountryAsync(
        DataBaseContext db, Guid id, string abbreviation, string code, ILogger logger)
    {
        var byId = await db.Countries.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == id);
        if (byId != null && IsSameKey(byId.Abbreviation, abbreviation))
        {
            return new LanguagePluginGeoRowMatch<Countries>(byId, IdTaken: true);
        }

        if (byId != null)
        {
            LogCollision(logger, code, CountryKind, id, byId.Abbreviation, abbreviation);
        }

        var byKey = await db.Countries.IgnoreQueryFilters()
            .Where(c => c.Abbreviation == abbreviation)
            .OrderBy(c => c.IsDeleted)
            .FirstOrDefaultAsync();

        return new LanguagePluginGeoRowMatch<Countries>(byKey, IdTaken: byId != null);
    }

    public static async Task<LanguagePluginGeoRowMatch<State>> FindStateAsync(
        DataBaseContext db, Guid id, string countryPrefix, string abbreviation, string code, ILogger logger)
    {
        var byId = await db.State.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id);
        if (byId != null && IsSameKey(byId.CountryPrefix, countryPrefix) && IsSameKey(byId.Abbreviation, abbreviation))
        {
            return new LanguagePluginGeoRowMatch<State>(byId, IdTaken: true);
        }

        if (byId != null)
        {
            LogCollision(logger, code, StateKind, id,
                Key(byId.CountryPrefix, byId.Abbreviation), Key(countryPrefix, abbreviation));
        }

        var byKey = await db.State.IgnoreQueryFilters()
            .Where(s => s.CountryPrefix == countryPrefix && s.Abbreviation == abbreviation)
            .OrderBy(s => s.IsDeleted)
            .FirstOrDefaultAsync();

        return new LanguagePluginGeoRowMatch<State>(byKey, IdTaken: byId != null);
    }

    public static async Task<LanguagePluginGeoRowMatch<CalendarRule>> FindCalendarRuleAsync(
        DataBaseContext db, CalendarRule entry, string code, ILogger logger)
    {
        var byId = await db.CalendarRule.FirstOrDefaultAsync(r => r.Id == entry.Id);
        if (byId != null && IsSameCalendarRule(byId, entry.Country, entry.State, entry.Name.En))
        {
            return new LanguagePluginGeoRowMatch<CalendarRule>(byId, IdTaken: true);
        }

        if (byId != null)
        {
            LogCollision(logger, code, CalendarRuleKind, entry.Id,
                Key(byId.Country, byId.State, byId.Name.En), Key(entry.Country, entry.State, entry.Name.En));
        }

        if (string.IsNullOrEmpty(entry.Name.En))
        {
            return new LanguagePluginGeoRowMatch<CalendarRule>(null, IdTaken: byId != null);
        }

        var candidates = await db.CalendarRule
            .Where(r => r.Country == entry.Country && r.State == entry.State)
            .ToListAsync();
        var byKey = candidates.FirstOrDefault(r => IsSameKey(r.Name.En, entry.Name.En));

        return new LanguagePluginGeoRowMatch<CalendarRule>(byKey, IdTaken: byId != null);
    }

    private static bool IsSameCalendarRule(CalendarRule row, string country, string state, string? englishName) =>
        IsSameKey(row.Country, country) && IsSameKey(row.State, state) && IsSameKey(row.Name.En, englishName);

    private static bool IsSameKey(string? left, string? right) =>
        string.Equals(left, right, StringComparison.Ordinal);

    private static string Key(params string?[] parts) => string.Join(KeySeparator, parts);

    private static void LogCollision(ILogger logger, string code, string kind, Guid id, string existingKey, string packKey)
    {
        logger.LogWarning(
            "Language plugin '{Code}' ships {Kind} id {Id} for '{PackKey}', but that id already belongs to '{ExistingKey}'; the existing row is left untouched",
            code.ForLog(), kind, id, packKey.ForLog(), existingKey.ForLog());
    }
}
