// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// K16 entity import of RestrictedTimeWindowRule rows from compliance.restrictedTimeWindows: validates the
/// profile entries into desired rows with a content-addressed source key, plans insert/update/skip
/// against the stored rows, applies the decisions inside the import transaction and warns about group
/// tags that match no existing group. Keep the hashed fields (ComputeContentHash), the applied fields
/// (Apply) and RestrictedTimeWindowImportValues in sync.
/// </summary>
/// <param name="restrictedTimeWindowRuleRepository">Reads and stages RestrictedTimeWindowRule rows</param>
/// <param name="groupRepository">Lists the groups a group tag is matched against</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Setup;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed class RestrictedTimeWindowRuleImporter
{
    private const string TimeOfDayFormat = "HH:mm";
    private const string SourceKeyTimeFormat = "HHmm";
    private const string AnyGroupTagKey = "*";
    private const char MonthDaySeparator = '-';
    private const int MonthDayPartCount = 2;
    private const int FirstMonth = 1;
    private const int LastMonth = 12;
    private const int FirstDay = 1;
    private const int LastDay = 31;

    private readonly IRestrictedTimeWindowRuleRepository _restrictedTimeWindowRuleRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly ILogger<RestrictedTimeWindowRuleImporter> _logger;

    public RestrictedTimeWindowRuleImporter(
        IRestrictedTimeWindowRuleRepository restrictedTimeWindowRuleRepository,
        IGroupRepository groupRepository,
        ILogger<RestrictedTimeWindowRuleImporter> logger)
    {
        _restrictedTimeWindowRuleRepository = restrictedTimeWindowRuleRepository;
        _groupRepository = groupRepository;
        _logger = logger;
    }

    // K16 restricted time windows. The source key is content-addressed (season + daily times + group tag)
    // like the plan specifies: re-applying the same file finds each row by its stored ImportSourceKey and
    // updates it to identical values (a no-op), while a customer edit to a live value fails the recomputed
    // hash and is left untouched (SkipEdited). KNOWN LIMITATION shared with every K20 rule type: editing an
    // identity field in the profile (here any of season/times/tag) produces a NEW key and leaves the prior
    // import row active - there is no orphan cleanup, so such a change must be paired with removing the old
    // row. No industry (SchedulingRuleId) binding: the group tag is K16's only scope axis.
    internal static List<EntityImportDesired<RestrictedTimeWindowImportValues>> BuildDesired(
        List<RegionSetupRestrictedTimeWindow>? windows, string sectionPath, string sourceKeyPrefix)
    {
        var seenKeys = new HashSet<string>();
        var desired = new List<EntityImportDesired<RestrictedTimeWindowImportValues>>();
        if (windows == null || windows.Count == 0)
        {
            return desired;
        }

        foreach (var window in windows)
        {
            if (string.IsNullOrWhiteSpace(window.SeasonFrom) || string.IsNullOrWhiteSpace(window.SeasonTo)
                || string.IsNullOrWhiteSpace(window.DailyStart) || string.IsNullOrWhiteSpace(window.DailyEnd))
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath} entry requires 'seasonFrom', 'seasonTo', 'dailyStart' and 'dailyEnd'.");
            }

            var (fromMonth, fromDay) = ParseMonthDay(window.SeasonFrom, $"{sectionPath}.seasonFrom");
            var (toMonth, toDay) = ParseMonthDay(window.SeasonTo, $"{sectionPath}.seasonTo");
            var dailyStart = ParseTimeOfDay(window.DailyStart, $"{sectionPath}.dailyStart");
            var dailyEnd = ParseTimeOfDay(window.DailyEnd, $"{sectionPath}.dailyEnd");

            var tag = window.AppliesToGroupTag?.Trim() ?? string.Empty;
            var values = new RestrictedTimeWindowImportValues(fromMonth, fromDay, toMonth, toDay, dailyStart, dailyEnd, tag);

            var sourceKey = BuildSourceKey(sourceKeyPrefix, values);
            if (!seenKeys.Add(sourceKey))
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath} contains more than one entry resolving to key '{sourceKey}'.");
            }

            desired.Add(new EntityImportDesired<RestrictedTimeWindowImportValues>(
                sourceKey,
                ComputeContentHash(values),
                values));
        }

        return desired;
    }

    public async Task<EntityImportFamilyPlan<RestrictedTimeWindowImportValues, RestrictedTimeWindowRule>> PlanAsync(
        IReadOnlyList<EntityImportDesired<RestrictedTimeWindowImportValues>> desired)
    {
        if (desired.Count == 0)
        {
            return EntityImportFamilyPlan<RestrictedTimeWindowImportValues, RestrictedTimeWindowRule>.Empty;
        }

        var sourceKeys = desired.Select(d => d.SourceKey).ToList();
        var existingRows = await _restrictedTimeWindowRuleRepository.GetBySourceKeysAsync(sourceKeys);
        var existingBySourceKey = existingRows.ToDictionary(r => r.ImportSourceKey);

        var existingUneditedBySourceKey = existingRows.ToDictionary(
            r => r.ImportSourceKey,
            r => ComputeContentHash(new RestrictedTimeWindowImportValues(
                r.SeasonFromMonth, r.SeasonFromDay, r.SeasonToMonth, r.SeasonToDay,
                r.DailyStart, r.DailyEnd, r.AppliesToGroupTag)) == r.ImportContentHash);

        var decisions = EntityImportPlanner.Plan(existingUneditedBySourceKey, desired);
        return new EntityImportFamilyPlan<RestrictedTimeWindowImportValues, RestrictedTimeWindowRule>(decisions, existingBySourceKey);
    }

    public void Apply(EntityImportFamilyPlan<RestrictedTimeWindowImportValues, RestrictedTimeWindowRule> plan)
    {
        foreach (var decision in plan.Decisions)
        {
            switch (decision.Action)
            {
                case EntityImportAction.Insert:
                    _restrictedTimeWindowRuleRepository.Add(new RestrictedTimeWindowRule
                    {
                        Id = Guid.NewGuid(),
                        SeasonFromMonth = decision.Values.SeasonFromMonth,
                        SeasonFromDay = decision.Values.SeasonFromDay,
                        SeasonToMonth = decision.Values.SeasonToMonth,
                        SeasonToDay = decision.Values.SeasonToDay,
                        DailyStart = decision.Values.DailyStart,
                        DailyEnd = decision.Values.DailyEnd,
                        AppliesToGroupTag = decision.Values.AppliesToGroupTag,
                        ImportSourceKey = decision.SourceKey,
                        ImportContentHash = decision.ContentHash,
                    });
                    break;
                case EntityImportAction.Update:
                    var existing = plan.ExistingBySourceKey[decision.SourceKey];
                    existing.SeasonFromMonth = decision.Values.SeasonFromMonth;
                    existing.SeasonFromDay = decision.Values.SeasonFromDay;
                    existing.SeasonToMonth = decision.Values.SeasonToMonth;
                    existing.SeasonToDay = decision.Values.SeasonToDay;
                    existing.DailyStart = decision.Values.DailyStart;
                    existing.DailyEnd = decision.Values.DailyEnd;
                    existing.AppliesToGroupTag = decision.Values.AppliesToGroupTag;
                    existing.ImportContentHash = decision.ContentHash;
                    _restrictedTimeWindowRuleRepository.Update(existing);
                    break;
                case EntityImportAction.SkipEdited:
                    _logger.LogInformation(
                        "Region setup: restricted time window '{SourceKey}' was edited by the customer since the last import, skipping re-apply",
                        decision.SourceKey);
                    break;
            }
        }
    }

    // Best-effort heads-up (never a hard failure): the group tag is a plain Group.Name convention, so a rule
    // whose tag matches no existing group stays inert until a group of that name is created and shifts are
    // linked into it. The group may legitimately be created later, so this only logs a warning.
    public async Task WarnOnMissingGroupTagsAsync(
        IReadOnlyList<EntityImportDesired<RestrictedTimeWindowImportValues>> desired)
    {
        var tags = desired
            .Select(d => d.Values.AppliesToGroupTag)
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (tags.Count == 0)
        {
            return;
        }

        var groups = await _groupRepository.List();
        var groupNames = groups
            .Select(g => g.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var tag in tags.Where(tag => !groupNames.Contains(tag)))
        {
            _logger.LogWarning(
                "Region setup: restricted-time-window group tag '{Tag}' matches no existing group; the rule stays inert until a group with that name is created and shifts are linked to it",
                tag);
        }
    }

    private static string BuildSourceKey(string sourceKeyPrefix, RestrictedTimeWindowImportValues values)
    {
        var tagSuffix = values.AppliesToGroupTag.Length == 0
            ? AnyGroupTagKey
            : values.AppliesToGroupTag.ToLowerInvariant();
        var season = string.Create(CultureInfo.InvariantCulture,
            $"{values.SeasonFromMonth:00}-{values.SeasonFromDay:00}:{values.SeasonToMonth:00}-{values.SeasonToDay:00}");
        var times = $"{values.DailyStart.ToString(SourceKeyTimeFormat, CultureInfo.InvariantCulture)}-{values.DailyEnd.ToString(SourceKeyTimeFormat, CultureInfo.InvariantCulture)}";
        return $"{sourceKeyPrefix}:{season}:{times}:{tagSuffix}";
    }

    private static string ComputeContentHash(RestrictedTimeWindowImportValues values)
    {
        return ImportContentHasher.ComputeHash(
            values.SeasonFromMonth.ToString(CultureInfo.InvariantCulture),
            values.SeasonFromDay.ToString(CultureInfo.InvariantCulture),
            values.SeasonToMonth.ToString(CultureInfo.InvariantCulture),
            values.SeasonToDay.ToString(CultureInfo.InvariantCulture),
            values.DailyStart.ToString(TimeOfDayFormat, CultureInfo.InvariantCulture),
            values.DailyEnd.ToString(TimeOfDayFormat, CultureInfo.InvariantCulture),
            values.AppliesToGroupTag);
    }

    private static (int Month, int Day) ParseMonthDay(string value, string fieldName)
    {
        var parts = value.Split(MonthDaySeparator);
        if (parts.Length != MonthDayPartCount
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var month)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var day)
            || month < FirstMonth || month > LastMonth || day < FirstDay || day > LastDay)
        {
            throw new InvalidRequestException(
                $"Region setup: '{value}' in {fieldName} must be a valid 'MM-DD' (month 1-12, day 1-31).");
        }

        return (month, day);
    }

    private static TimeOnly ParseTimeOfDay(string value, string fieldName)
    {
        if (!TimeOnly.TryParseExact(value, TimeOfDayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            throw new InvalidRequestException(
                $"Region setup: '{value}' in {fieldName} must be a valid 'HH:mm' time.");
        }

        return time;
    }
}
