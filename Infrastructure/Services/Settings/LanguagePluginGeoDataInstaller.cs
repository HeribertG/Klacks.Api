// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Installs and uninstalls geographic data (countries, states, calendar rules, calendar selections)
/// for language plugins.
/// </summary>
/// <param name="pluginDirectory">Base directory of the language plugins</param>
/// <param name="manifests">Registry aller entdeckten Plugin-Manifeste</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Collections.Concurrent;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class LanguagePluginGeoDataInstaller
{
    private readonly string _pluginDirectory;
    private readonly ConcurrentDictionary<string, LanguagePluginManifest> _manifests;
    private readonly ILogger _logger;

    public LanguagePluginGeoDataInstaller(
        string pluginDirectory,
        ConcurrentDictionary<string, LanguagePluginManifest> manifests,
        ILogger logger)
    {
        _pluginDirectory = pluginDirectory;
        _manifests = manifests;
        _logger = logger;
    }

    public async Task InstallGeoDataAsync(IServiceScope scope, string code)
    {
        var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();

        var countries = LanguagePluginFileLoader.LoadPluginDataFile<Countries>(_pluginDirectory, code, LanguagePluginConstants.CountriesFileName, _logger);
        if (countries != null)
        {
            foreach (var country in countries)
            {
                var existing = await db.Countries.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(c => c.Id == country.Id);

                if (existing != null)
                {
                    existing.IsDeleted = false;
                    existing.DeletedTime = null;
                }
                else
                {
                    db.Countries.Add(country);
                }
            }
        }

        var states = LanguagePluginFileLoader.LoadPluginDataFile<State>(_pluginDirectory, code, LanguagePluginConstants.StatesFileName, _logger);
        if (states != null)
        {
            foreach (var state in states)
            {
                var existing = await db.State.IgnoreQueryFilters()
                    .FirstOrDefaultAsync(s => s.Id == state.Id);

                if (existing != null)
                {
                    existing.IsDeleted = false;
                    existing.DeletedTime = null;
                }
                else
                {
                    db.State.Add(state);
                }
            }
        }

        var rules = LanguagePluginFileLoader.LoadPluginDataFile<CalendarRule>(_pluginDirectory, code, LanguagePluginConstants.CalendarRulesFileName, _logger);
        if (rules != null)
        {
            foreach (var rule in rules)
            {
                if (!await db.CalendarRule.AnyAsync(r => r.Id == rule.Id))
                {
                    db.CalendarRule.Add(rule);
                }
            }
        }

        _logger.LogInformation(
            "Installed geo data for language plugin '{Code}': {Countries} countries, {States} states, {Rules} calendar rules",
            code, countries?.Count ?? 0, states?.Count ?? 0, rules?.Count ?? 0);

        if (rules is { Count: > 0 } && states is { Count: > 0 } && countries is { Count: > 0 })
        {
            await InstallCalendarSelectionsAsync(db, code, countries, states, rules);
        }
    }

    public async Task UninstallGeoDataAsync(IServiceScope scope, string code)
    {
        var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();

        var calendarSelections = await db.CalendarSelection
            .Include(cs => cs.SelectedCalendars)
            .Where(cs => cs.PluginCode == code)
            .ToListAsync();

        if (calendarSelections.Count > 0)
        {
            db.CalendarSelection.RemoveRange(calendarSelections);
            _logger.LogInformation(
                "Uninstalled {Count} calendar selection(s) for language plugin '{Code}'",
                calendarSelections.Count, code);
        }

        var countries = LanguagePluginFileLoader.LoadPluginDataFile<Countries>(_pluginDirectory, code, LanguagePluginConstants.CountriesFileName, _logger);
        if (countries == null || countries.Count == 0)
            return;

        var abbreviations = countries.Select(c => c.Abbreviation).ToList();

        var calendarRules = await db.CalendarRule
            .Where(r => abbreviations.Contains(r.Country))
            .ToListAsync();
        db.CalendarRule.RemoveRange(calendarRules);

        var statesToRemove = await db.State
            .Where(s => abbreviations.Contains(s.CountryPrefix))
            .ToListAsync();
        db.State.RemoveRange(statesToRemove);

        var countriesToRemove = await db.Countries
            .Where(c => abbreviations.Contains(c.Abbreviation))
            .ToListAsync();
        db.Countries.RemoveRange(countriesToRemove);

        _logger.LogInformation(
            "Uninstalled geo data for language plugin '{Code}': {Countries} countries, {States} states, {Rules} calendar rules",
            code, countriesToRemove.Count, statesToRemove.Count, calendarRules.Count);
    }

    private async Task InstallCalendarSelectionsAsync(
        DataBaseContext db,
        string code,
        List<Countries> countries,
        List<State> states,
        List<CalendarRule> rules)
    {
        var manifest = _manifests.GetValueOrDefault(code);
        var languageCode = manifest?.Code ?? "en";

        var stateNameOverrides = LoadStateNamesForLanguage(code, languageCode);

        var regionalStateCodes = rules
            .Where(r => r.State != r.Country)
            .Select(r => r.State)
            .Distinct()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var countryAbbreviations = countries
            .Select(c => c.Abbreviation)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var count = 0;

        foreach (var state in states)
        {
            if (!countryAbbreviations.Contains(state.CountryPrefix))
                continue;

            var stateName = stateNameOverrides.GetValueOrDefault(state.Abbreviation)
                ?? state.Name.GetValue(languageCode)
                ?? state.Name.En
                ?? state.Abbreviation;

            var existing = await db.CalendarSelection.IgnoreQueryFilters()
                .FirstOrDefaultAsync(cs => cs.PluginCode == code && cs.Name == stateName);

            if (existing != null)
            {
                existing.IsDeleted = false;
                existing.DeletedTime = null;
                continue;
            }

            var calendarSelection = new CalendarSelection
            {
                Id = Guid.NewGuid(),
                Name = stateName,
                PluginCode = code,
                CreateTime = DateTime.UtcNow,
                CurrentUserCreated = "System",
                SelectedCalendars = new List<SelectedCalendar>()
            };

            calendarSelection.SelectedCalendars.Add(new SelectedCalendar
            {
                Id = Guid.NewGuid(),
                CalendarSelectionId = calendarSelection.Id,
                Country = state.CountryPrefix,
                State = state.CountryPrefix,
                CreateTime = DateTime.UtcNow,
                CurrentUserCreated = "System"
            });

            if (regionalStateCodes.Contains(state.Abbreviation))
            {
                calendarSelection.SelectedCalendars.Add(new SelectedCalendar
                {
                    Id = Guid.NewGuid(),
                    CalendarSelectionId = calendarSelection.Id,
                    Country = state.CountryPrefix,
                    State = state.Abbreviation,
                    CreateTime = DateTime.UtcNow,
                    CurrentUserCreated = "System"
                });
            }

            db.CalendarSelection.Add(calendarSelection);
            count++;
        }

        _logger.LogInformation(
            "Installed {Count} calendar selection(s) for language plugin '{Code}'",
            count, code);
    }

    private Dictionary<string, string> LoadStateNamesForLanguage(string code, string languageCode)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var filePath = Path.Combine(_pluginDirectory, code, LanguagePluginConstants.StatesFileName);

        if (!File.Exists(filePath))
            return result;

        try
        {
            var json = File.ReadAllText(filePath);
            using var doc = JsonDocument.Parse(json);

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                var abbreviation = element.GetProperty("abbreviation").GetString();
                if (string.IsNullOrEmpty(abbreviation))
                    continue;

                if (element.TryGetProperty("name", out var nameObj)
                    && nameObj.TryGetProperty(languageCode, out var langValue))
                {
                    var name = langValue.GetString();
                    if (!string.IsNullOrEmpty(name))
                    {
                        result[abbreviation] = name;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load state names for language '{Code}' in plugin '{PluginCode}'",
                languageCode, code);
        }

        return result;
    }
}
