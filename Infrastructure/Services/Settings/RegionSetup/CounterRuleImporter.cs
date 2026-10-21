// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// K20 entity import of CounterRule rows from compliance.counterRules and industry-bound counterRules:
/// validates the profile entries into desired rows, plans insert/update/skip against the stored rows and
/// applies the decisions inside the import transaction. Enforcement is deliberately neither hashed nor
/// written on Update, so a customer enforcement override survives a re-import. Keep the hashed fields
/// (ComputeContentHash), the applied fields (Apply) and CounterRuleImportValues in sync.
/// </summary>
/// <param name="counterRuleRepository">Reads and stages CounterRule rows</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Application.Services.Setup;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed class CounterRuleImporter
{
    private const string FixedDecimalFormat = "F4";
    private const string HoursKeyFormat = "0.##";

    private readonly ICounterRuleRepository _counterRuleRepository;
    private readonly ILogger<CounterRuleImporter> _logger;

    public CounterRuleImporter(ICounterRuleRepository counterRuleRepository, ILogger<CounterRuleImporter> logger)
    {
        _counterRuleRepository = counterRuleRepository;
        _logger = logger;
    }

    internal static List<EntityImportDesired<CounterRuleImportValues>> BuildDesired(
        List<RegionSetupCounterRule>? counterRules, string sectionPath, string sourceKeyPrefix, HashSet<string>? sharedSeenKeys = null)
    {
        var seenKeys = sharedSeenKeys ?? new HashSet<string>();
        var desired = new List<EntityImportDesired<CounterRuleImportValues>>();
        if (counterRules == null || counterRules.Count == 0)
        {
            return desired;
        }

        foreach (var counter in counterRules)
        {
            if (string.IsNullOrWhiteSpace(counter.Event) || counter.Threshold == null)
            {
                throw new InvalidRequestException($"Region setup: {sectionPath} entry requires 'event' and 'threshold'.");
            }

            var eventType = ParseCounterEvent(counter.Event, $"{sectionPath}.event");
            var period = ParseCounterPeriod(counter.Period, eventType, $"{sectionPath}.period");

            if (counter.Threshold <= 0)
            {
                throw new InvalidRequestException($"Region setup: {sectionPath}.threshold must be greater than zero.");
            }

            if (eventType == CounterEventType.ShiftExceedingHours)
            {
                if (counter.HoursThreshold is not > 0)
                {
                    throw new InvalidRequestException(
                        $"Region setup: {sectionPath} entry with event 'shiftExceedingHours' requires 'hoursThreshold' greater than zero.");
                }
            }
            else if (counter.HoursThreshold != null)
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath}.hoursThreshold is only allowed with event 'shiftExceedingHours'.");
            }

            var hoursSuffix = counter.HoursThreshold.HasValue
                ? $":{counter.HoursThreshold.Value.ToString(HoursKeyFormat, CultureInfo.InvariantCulture)}h"
                : string.Empty;
            var sourceKey = $"{sourceKeyPrefix}:{eventType.ToString().ToLowerInvariant()}:{period.ToString().ToLowerInvariant()}{hoursSuffix}";
            if (!seenKeys.Add(sourceKey))
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath} contains more than one entry resolving to key '{sourceKey}'.");
            }

            var values = new CounterRuleImportValues(eventType, period, counter.Threshold.Value, counter.HoursThreshold);
            desired.Add(new EntityImportDesired<CounterRuleImportValues>(
                sourceKey,
                ComputeContentHash(values.EventType, values.Period, values.Threshold, values.HoursThreshold),
                values));
        }

        return desired;
    }

    public async Task<EntityImportFamilyPlan<CounterRuleImportValues, CounterRule>> PlanAsync(
        IReadOnlyList<EntityImportDesired<CounterRuleImportValues>> desired)
    {
        if (desired.Count == 0)
        {
            return EntityImportFamilyPlan<CounterRuleImportValues, CounterRule>.Empty;
        }

        var sourceKeys = desired.Select(d => d.SourceKey).ToList();
        var existingRows = await _counterRuleRepository.GetBySourceKeysAsync(sourceKeys);
        var existingBySourceKey = existingRows.ToDictionary(r => r.ImportSourceKey);

        var existingUneditedBySourceKey = existingRows.ToDictionary(
            r => r.ImportSourceKey,
            r => ComputeContentHash(r.EventType, r.Period, r.Threshold, r.HoursThreshold) == r.ImportContentHash);

        var decisions = EntityImportPlanner.Plan(existingUneditedBySourceKey, desired);
        return new EntityImportFamilyPlan<CounterRuleImportValues, CounterRule>(decisions, existingBySourceKey);
    }

    public void Apply(
        EntityImportFamilyPlan<CounterRuleImportValues, CounterRule> plan,
        IReadOnlyDictionary<string, string> ruleSourceKeyByBoundEntityKey,
        IReadOnlyDictionary<string, Guid> ruleIdBySourceKey)
    {
        foreach (var decision in plan.Decisions)
        {
            switch (decision.Action)
            {
                case EntityImportAction.Insert:
                    _counterRuleRepository.Add(decision.Values.ToNewImportedRule(
                        ImportBoundRuleResolver.ResolveBoundRuleId(decision.SourceKey, ruleSourceKeyByBoundEntityKey, ruleIdBySourceKey),
                        decision.SourceKey,
                        decision.ContentHash));
                    break;
                case EntityImportAction.Update:
                    var existing = plan.ExistingBySourceKey[decision.SourceKey];
                    existing.EventType = decision.Values.EventType;
                    existing.Period = decision.Values.Period;
                    existing.Threshold = decision.Values.Threshold;
                    existing.HoursThreshold = decision.Values.HoursThreshold;
                    existing.SchedulingRuleId = ImportBoundRuleResolver.ResolveBoundRuleId(decision.SourceKey, ruleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
                    existing.Origin = RuleOrigin.Import;
                    existing.ImportContentHash = decision.ContentHash;
                    _counterRuleRepository.Update(existing);
                    break;
                case EntityImportAction.SkipEdited:
                    _logger.LogInformation(
                        "Region setup: counter rule '{SourceKey}' was edited by the customer since the last import, skipping re-apply",
                        decision.SourceKey);
                    break;
            }
        }
    }

    private static string ComputeContentHash(CounterEventType eventType, CounterPeriod period, int threshold, decimal? hoursThreshold)
    {
        return ImportContentHasher.ComputeHash(
            eventType.ToString(),
            period.ToString(),
            threshold.ToString(CultureInfo.InvariantCulture),
            hoursThreshold?.ToString(FixedDecimalFormat, CultureInfo.InvariantCulture) ?? string.Empty);
    }

    private static CounterEventType ParseCounterEvent(string value, string fieldName)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "nightshift" => CounterEventType.NightShift,
            "workeddayinweek" => CounterEventType.WorkedDayInWeek,
            "shiftexceedinghours" => CounterEventType.ShiftExceedingHours,
            _ => throw new InvalidRequestException(
                $"Region setup: '{value}' in {fieldName} must be 'nightShift', 'workedDayInWeek' or 'shiftExceedingHours'."),
        };
    }

    private static CounterPeriod ParseCounterPeriod(string? value, CounterEventType eventType, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return eventType == CounterEventType.WorkedDayInWeek ? CounterPeriod.Week : CounterPeriod.Year;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "week" => CounterPeriod.Week,
            "month" => CounterPeriod.Month,
            "year" => CounterPeriod.Year,
            _ => throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} must be 'week', 'month' or 'year'."),
        };
    }
}
