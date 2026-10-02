// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Heals, once per installed language pack, the calendar rules the pre-v1.0.36 installer skipped because the
/// pack's rule id was already held by another pack's rule. Only inserts, never updates or deletes. A pack rule is
/// inserted only when no row carries its natural key (country, state, English name) AND its current id or its
/// pre-renumbering id (calendar-rule-legacy-ids.json) is held by a rule of another country - the footprint of the
/// skip. A rule the customer deleted without such a footprint stays deleted. A rule the customer deleted BEFORE the
/// heal whose id is held by another country's rule cannot be told apart from a skipped one and comes back once, on
/// the first start with this heal. In the same run and transaction, every rule of another country that holds the
/// current or pre-renumbering id of a pack rule gets the foreign translations the old id-only merge wrote into it
/// repaired (see LanguagePluginCalendarRuleTranslationRepair). A per-pack setting marks the heal as done, so a rule deleted after the heal is
/// never revived; a fresh install sets the marker right away. A pack file that exists but cannot be read, or a
/// legacy map with duplicate ids, logs an error and sets no marker, so the next start retries.
/// </summary>
/// <param name="pluginDirectory">Base directory of the language plugins</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class LanguagePluginCalendarRuleBackfiller
{
    private const string NaturalKeySeparator = "/";

    private readonly string _pluginDirectory;
    private readonly ILogger _logger;

    public LanguagePluginCalendarRuleBackfiller(string pluginDirectory, ILogger logger)
    {
        _pluginDirectory = pluginDirectory;
        _logger = logger;
    }

    public async Task BackfillMissingCalendarRulesAsync(IServiceScope scope, string code)
    {
        var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();

        try
        {
            var markerKey = MarkerKey(code);
            if (await IsCalendarRuleBackfillMarkedAsync(db, markerKey))
            {
                return;
            }

            if (!TryLoadRules(code, out var rules) || !TryLoadLegacyIds(code, out var legacyIds))
            {
                return;
            }

            var (healed, repaired) = await StageRepairsAsync(db, code, rules, legacyIds);
            StageMarker(db, markerKey);
            await db.SaveChangesAsync();

            if (healed > 0 || repaired > 0)
            {
                _logger.LogInformation(
                    "Language plugin '{Code}': backfilled {Healed} calendar rule(s) an earlier install skipped and repaired {Repaired} foreign translation(s) it merged into other countries' rules",
                    code.ForLog(), healed, repaired);
            }
        }
        catch (Exception ex)
        {
            db.ChangeTracker.Clear();
            _logger.LogError(ex, "Failed to backfill calendar rules for language plugin '{Code}'", code.ForLog());
        }
    }

    public async Task MarkCalendarRulesInstalledAsync(IServiceScope scope, string code)
    {
        var db = scope.ServiceProvider.GetRequiredService<DataBaseContext>();
        var markerKey = MarkerKey(code);

        if (await IsCalendarRuleBackfillMarkedAsync(db, markerKey))
        {
            return;
        }

        StageMarker(db, markerKey);
    }

    private async Task<(int Healed, int Repaired)> StageRepairsAsync(
        DataBaseContext db, string code, List<CalendarRule> rules, IReadOnlyDictionary<Guid, Guid> legacyIds)
    {
        var healed = 0;
        var repaired = 0;
        Dictionary<string, CalendarRule>? ownSources = null;

        foreach (var rule in rules)
        {
            if (string.IsNullOrEmpty(rule.Name.En))
            {
                continue;
            }

            var foreignHolders = await FindForeignHoldersAsync(db, rule, legacyIds);
            if (foreignHolders.Count == 0)
            {
                continue;
            }

            ownSources ??= LoadOwnSourcesOfAllPacks();
            foreach (var holder in foreignHolders)
            {
                var ownSource = ownSources.GetValueOrDefault(NaturalKey(holder.Country, holder.State, holder.Name.En));
                repaired += LanguagePluginCalendarRuleTranslationRepair.Repair(holder, rule, ownSource);
            }

            var match = await LanguagePluginGeoRowResolver.FindCalendarRuleAsync(db, rule, code, _logger);
            if (match.Row != null)
            {
                continue;
            }

            rule.Id = match.IdForInsert(rule.Id);
            db.CalendarRule.Add(rule);
            healed++;
        }

        return (healed, repaired);
    }

    private static Task<List<CalendarRule>> FindForeignHoldersAsync(
        DataBaseContext db, CalendarRule rule, IReadOnlyDictionary<Guid, Guid> legacyIds)
    {
        var ids = new List<Guid> { rule.Id };
        if (legacyIds.TryGetValue(rule.Id, out var legacyId))
        {
            ids.Add(legacyId);
        }

        return db.CalendarRule
            .Where(r => ids.Contains(r.Id) && r.Country != rule.Country)
            .ToListAsync();
    }

    private Dictionary<string, CalendarRule> LoadOwnSourcesOfAllPacks()
    {
        var sources = new Dictionary<string, CalendarRule>(StringComparer.Ordinal);
        if (!Directory.Exists(_pluginDirectory))
        {
            return sources;
        }

        foreach (var packDirectory in Directory.GetDirectories(_pluginDirectory))
        {
            var entries = LanguagePluginFileLoader.LoadPluginDataFile<CalendarRule>(
                _pluginDirectory, Path.GetFileName(packDirectory), LanguagePluginConstants.CalendarRulesFileName, _logger);

            foreach (var entry in entries ?? [])
            {
                if (!string.IsNullOrEmpty(entry.Name.En))
                {
                    sources.TryAdd(NaturalKey(entry.Country, entry.State, entry.Name.En), entry);
                }
            }
        }

        return sources;
    }

    private static string NaturalKey(string country, string state, string? englishName) =>
        string.Join(NaturalKeySeparator, country, state, englishName);

    private bool TryLoadRules(string code, out List<CalendarRule> rules)
    {
        rules = [];
        if (!PackFileExists(code, LanguagePluginConstants.CalendarRulesFileName))
        {
            return true;
        }

        var loaded = LanguagePluginFileLoader.LoadPluginDataFile<CalendarRule>(
            _pluginDirectory, code, LanguagePluginConstants.CalendarRulesFileName, _logger);
        if (loaded == null)
        {
            LogUnreadable(code, LanguagePluginConstants.CalendarRulesFileName);
            return false;
        }

        rules = loaded;
        return true;
    }

    private bool TryLoadLegacyIds(string code, out Dictionary<Guid, Guid> legacyIds)
    {
        legacyIds = new Dictionary<Guid, Guid>();
        if (!PackFileExists(code, LanguagePluginConstants.CalendarRuleLegacyIdsFileName))
        {
            return true;
        }

        var entries = LanguagePluginFileLoader.LoadPluginDataFile<LanguagePluginCalendarRuleLegacyId>(
            _pluginDirectory, code, LanguagePluginConstants.CalendarRuleLegacyIdsFileName, _logger);
        if (entries == null)
        {
            LogUnreadable(code, LanguagePluginConstants.CalendarRuleLegacyIdsFileName);
            return false;
        }

        foreach (var entry in entries)
        {
            if (!legacyIds.TryAdd(entry.Id, entry.LegacyId))
            {
                _logger.LogError(
                    "Language plugin '{Code}' lists calendar rule id {Id} twice in '{FileName}'; the calendar rule backfill is skipped and retried on the next start",
                    code.ForLog(), entry.Id, LanguagePluginConstants.CalendarRuleLegacyIdsFileName);
                return false;
            }
        }

        return true;
    }

    private bool PackFileExists(string code, string fileName) =>
        File.Exists(Path.Combine(_pluginDirectory, code, fileName));

    private void LogUnreadable(string code, string fileName)
    {
        _logger.LogError(
            "Language plugin '{Code}' ships '{FileName}' but it cannot be read; the calendar rule backfill is skipped and retried on the next start",
            code.ForLog(), fileName);
    }

    private static void StageMarker(DataBaseContext db, string markerKey)
    {
        db.Settings.Add(new Domain.Models.Settings.Settings
        {
            Id = Guid.NewGuid(),
            Type = markerKey,
            Value = LanguagePluginConstants.CalendarRuleBackfillDoneValue
        });
    }

    private static async Task<bool> IsCalendarRuleBackfillMarkedAsync(DataBaseContext db, string markerKey) =>
        await db.Settings.FirstOrDefaultAsync(s => s.Type == markerKey) != null;

    private static string MarkerKey(string code) =>
        LanguagePluginConstants.CalendarRuleBackfillSettingPrefix + code.ToUpperInvariant();
}
