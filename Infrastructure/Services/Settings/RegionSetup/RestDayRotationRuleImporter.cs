// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// K20 entity import of RestDayRotationRule rows from compliance.restDayRotations and industry-bound
/// restDayRotations: validates the profile entries into desired rows, plans insert/update/skip against
/// the stored rows and applies the decisions inside the import transaction. Keep the hashed fields
/// (ComputeContentHash), the applied fields (Apply) and RestDayRotationImportValues in sync.
/// </summary>
/// <param name="restDayRotationRuleRepository">Reads and stages RestDayRotationRule rows</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Application.Services.Setup;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed class RestDayRotationRuleImporter
{
    private readonly IRestDayRotationRuleRepository _restDayRotationRuleRepository;
    private readonly ILogger<RestDayRotationRuleImporter> _logger;

    public RestDayRotationRuleImporter(IRestDayRotationRuleRepository restDayRotationRuleRepository, ILogger<RestDayRotationRuleImporter> logger)
    {
        _restDayRotationRuleRepository = restDayRotationRuleRepository;
        _logger = logger;
    }

    internal static List<EntityImportDesired<RestDayRotationImportValues>> BuildDesired(
        List<RegionSetupRestDayRotation>? rotations, string sectionPath, string sourceKeyPrefix, HashSet<string>? sharedSeenKeys = null)
    {
        if (rotations == null || rotations.Count == 0)
        {
            return [];
        }

        var seenKeys = sharedSeenKeys ?? new HashSet<string>();
        var desired = new List<EntityImportDesired<RestDayRotationImportValues>>();

        foreach (var rotation in rotations)
        {
            if (string.IsNullOrWhiteSpace(rotation.DayOfWeek) || rotation.MinFree == null || rotation.WindowWeeks == null)
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath} entry requires 'dayOfWeek', 'minFree' and 'windowWeeks'.");
            }

            var dayOfWeek = RegionSetupFieldParser.ParseDayOfWeek(rotation.DayOfWeek.Trim(), $"{sectionPath}.dayOfWeek");

            if (rotation.MinFree <= 0)
            {
                throw new InvalidRequestException($"Region setup: {sectionPath}.minFree must be greater than zero.");
            }

            if (rotation.WindowWeeks <= 0)
            {
                throw new InvalidRequestException($"Region setup: {sectionPath}.windowWeeks must be greater than zero.");
            }

            if (rotation.MinFree > rotation.WindowWeeks)
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath}.minFree must not exceed windowWeeks - the rule would be impossible to satisfy.");
            }

            var sourceKey = $"{sourceKeyPrefix}:{dayOfWeek.ToString().ToLowerInvariant()}:{rotation.WindowWeeks.Value}w";
            if (!seenKeys.Add(sourceKey))
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath} contains more than one entry for day '{rotation.DayOfWeek}' and windowWeeks '{rotation.WindowWeeks}'.");
            }

            var values = new RestDayRotationImportValues(dayOfWeek, rotation.MinFree.Value, rotation.WindowWeeks.Value);
            desired.Add(new EntityImportDesired<RestDayRotationImportValues>(
                sourceKey,
                ComputeContentHash(values.DayOfWeek, values.MinFree, values.WindowWeeks),
                values));
        }

        return desired;
    }

    public async Task<EntityImportFamilyPlan<RestDayRotationImportValues, RestDayRotationRule>> PlanAsync(
        IReadOnlyList<EntityImportDesired<RestDayRotationImportValues>> desired)
    {
        if (desired.Count == 0)
        {
            return EntityImportFamilyPlan<RestDayRotationImportValues, RestDayRotationRule>.Empty;
        }

        var sourceKeys = desired.Select(d => d.SourceKey).ToList();
        var existingRows = await _restDayRotationRuleRepository.GetBySourceKeysAsync(sourceKeys);
        var existingBySourceKey = existingRows.ToDictionary(r => r.ImportSourceKey);

        var existingUneditedBySourceKey = existingRows.ToDictionary(
            r => r.ImportSourceKey,
            r => ComputeContentHash(r.DayOfWeek, r.MinFreeCount, r.WindowWeeks) == r.ImportContentHash);

        var decisions = EntityImportPlanner.Plan(existingUneditedBySourceKey, desired);
        return new EntityImportFamilyPlan<RestDayRotationImportValues, RestDayRotationRule>(decisions, existingBySourceKey);
    }

    public void Apply(
        EntityImportFamilyPlan<RestDayRotationImportValues, RestDayRotationRule> plan,
        IReadOnlyDictionary<string, string> ruleSourceKeyByBoundEntityKey,
        IReadOnlyDictionary<string, Guid> ruleIdBySourceKey)
    {
        foreach (var decision in plan.Decisions)
        {
            switch (decision.Action)
            {
                case EntityImportAction.Insert:
                    _restDayRotationRuleRepository.Add(new RestDayRotationRule
                    {
                        Id = Guid.NewGuid(),
                        DayOfWeek = decision.Values.DayOfWeek,
                        MinFreeCount = decision.Values.MinFree,
                        WindowWeeks = decision.Values.WindowWeeks,
                        SchedulingRuleId = ImportBoundRuleResolver.ResolveBoundRuleId(decision.SourceKey, ruleSourceKeyByBoundEntityKey, ruleIdBySourceKey),
                        ImportSourceKey = decision.SourceKey,
                        ImportContentHash = decision.ContentHash,
                    });
                    break;
                case EntityImportAction.Update:
                    var existing = plan.ExistingBySourceKey[decision.SourceKey];
                    existing.DayOfWeek = decision.Values.DayOfWeek;
                    existing.MinFreeCount = decision.Values.MinFree;
                    existing.WindowWeeks = decision.Values.WindowWeeks;
                    existing.SchedulingRuleId = ImportBoundRuleResolver.ResolveBoundRuleId(decision.SourceKey, ruleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
                    existing.ImportContentHash = decision.ContentHash;
                    _restDayRotationRuleRepository.Update(existing);
                    break;
                case EntityImportAction.SkipEdited:
                    _logger.LogInformation(
                        "Region setup: rest-day rotation '{SourceKey}' was edited by the customer since the last import, skipping re-apply",
                        decision.SourceKey);
                    break;
            }
        }
    }

    private static string ComputeContentHash(DayOfWeek dayOfWeek, int minFree, int windowWeeks)
    {
        return ImportContentHasher.ComputeHash(
            dayOfWeek.ToString(),
            minFree.ToString(CultureInfo.InvariantCulture),
            windowWeeks.ToString(CultureInfo.InvariantCulture));
    }
}
