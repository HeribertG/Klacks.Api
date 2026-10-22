// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// K20 entity import of dated SchedulingRuleRateRevision rows of the scheduling rule presets: validates
/// the rateRevisions into desired rows keyed by preset and ValidFrom, plans insert/update/skip against
/// the stored rows (accepting the legacy six-field hash as unedited) and applies the decisions inside the
/// import transaction, reporting the revisions whose content actually changed. Keep
/// SchedulingRuleRateRevisionImportValues, ComputeContentHash and CopyRateRevisionValues/ToImportValues
/// here in sync - a field present in only some of them silently breaks the customer-edit detection.
/// </summary>
/// <param name="schedulingRuleRateRevisionImportRepository">Reads and stages imported rate revision rows</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Application.Services.Setup;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed class SchedulingRuleRateRevisionImporter
{
    private readonly ISchedulingRuleRateRevisionImportRepository _schedulingRuleRateRevisionImportRepository;
    private readonly ILogger<SchedulingRuleRateRevisionImporter> _logger;

    public SchedulingRuleRateRevisionImporter(
        ISchedulingRuleRateRevisionImportRepository schedulingRuleRateRevisionImportRepository,
        ILogger<SchedulingRuleRateRevisionImporter> logger)
    {
        _schedulingRuleRateRevisionImportRepository = schedulingRuleRateRevisionImportRepository;
        _logger = logger;
    }

    // Each revision is a full snapshot of the five surcharge rates AND the overtime tier ladder effective
    // from validFrom onward, keyed by the parent preset source key plus the ISO date. validFrom must be
    // strictly ascending across a preset's revisions (this also guarantees the :rev:{date} keys are unique)
    // and at least one rate or an overtime tier must be set, otherwise the row would be dead configuration.
    // Overtime is validated by the same PresetOvertimeParser the preset uses (fixedPerShift rejected, at
    // most three strictly-ascending tiers).
    internal static List<EntityImportDesired<SchedulingRuleRateRevisionImportValues>> BuildDesired(
        string industry, RegionSetupSchedulingRulePreset preset, string presetSourceKey, HashSet<string> seenKeys)
    {
        var result = new List<EntityImportDesired<SchedulingRuleRateRevisionImportValues>>();
        if (preset.RateRevisions == null || preset.RateRevisions.Count == 0)
        {
            return result;
        }

        var fieldPrefix = $"industryProfiles.{industry}.schedulingRulePresets.rateRevisions";
        var previousValidFrom = DateOnly.MinValue;
        var hasPrevious = false;

        for (var i = 0; i < preset.RateRevisions.Count; i++)
        {
            var revision = preset.RateRevisions[i];
            if (string.IsNullOrWhiteSpace(revision.ValidFrom))
            {
                throw new InvalidRequestException($"Region setup: {fieldPrefix}[{i}] requires a 'validFrom' date ({DateTimeFormats.IsoDate}).");
            }

            if (!DateOnly.TryParseExact(revision.ValidFrom.Trim(), DateTimeFormats.IsoDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var validFrom))
            {
                throw new InvalidRequestException($"Region setup: {fieldPrefix}[{i}].validFrom '{revision.ValidFrom}' is not a valid {DateTimeFormats.IsoDate} date.");
            }

            if (hasPrevious && validFrom <= previousValidFrom)
            {
                throw new InvalidRequestException($"Region setup: {fieldPrefix}[{i}].validFrom must be strictly ascending across a preset's revisions.");
            }

            RegionSetupFieldParser.RequireNonNegative(revision.NightRate, $"{fieldPrefix}[{i}].nightRate");
            RegionSetupFieldParser.RequireNonNegative(revision.HolidayRate, $"{fieldPrefix}[{i}].holidayRate");
            RegionSetupFieldParser.RequireNonNegative(revision.We1Rate, $"{fieldPrefix}[{i}].we1Rate");
            RegionSetupFieldParser.RequireNonNegative(revision.We2Rate, $"{fieldPrefix}[{i}].we2Rate");
            RegionSetupFieldParser.RequireNonNegative(revision.We3Rate, $"{fieldPrefix}[{i}].we3Rate");

            var overtime = PresetOvertimeParser.Parse(revision.Overtime, $"{fieldPrefix}[{i}]");

            var hasAnyRate = revision.NightRate.HasValue || revision.HolidayRate.HasValue
                || revision.We1Rate.HasValue || revision.We2Rate.HasValue || revision.We3Rate.HasValue;
            if (!hasAnyRate && !overtime.Tier1AfterHours.HasValue)
            {
                throw new InvalidRequestException($"Region setup: {fieldPrefix}[{i}] must set at least one surcharge rate or an overtime tier.");
            }

            var sourceKey = $"{presetSourceKey}:rev:{validFrom.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)}";
            if (!seenKeys.Add(sourceKey))
            {
                throw new InvalidRequestException(
                    $"Region setup: industryProfiles contains more than one rate revision resolving to key '{sourceKey}'.");
            }

            var values = new SchedulingRuleRateRevisionImportValues(
                validFrom,
                revision.NightRate,
                revision.HolidayRate,
                revision.We1Rate,
                revision.We2Rate,
                revision.We3Rate,
                overtime.Basis,
                overtime.RateMode,
                overtime.Tier1AfterHours,
                overtime.Tier1Rate,
                overtime.Tier2AfterHours,
                overtime.Tier2Rate,
                overtime.Tier3AfterHours,
                overtime.Tier3Rate);

            result.Add(new EntityImportDesired<SchedulingRuleRateRevisionImportValues>(
                sourceKey,
                ComputeContentHash(values),
                values));

            previousValidFrom = validFrom;
            hasPrevious = true;
        }

        return result;
    }

    public async Task<EntityImportFamilyPlan<SchedulingRuleRateRevisionImportValues, SchedulingRuleRateRevision>> PlanAsync(
        IReadOnlyList<EntityImportDesired<SchedulingRuleRateRevisionImportValues>> desired)
    {
        if (desired.Count == 0)
        {
            return EntityImportFamilyPlan<SchedulingRuleRateRevisionImportValues, SchedulingRuleRateRevision>.Empty;
        }

        var sourceKeys = desired.Select(d => d.SourceKey).ToList();
        var existingRows = await _schedulingRuleRateRevisionImportRepository.GetBySourceKeysAsync(sourceKeys);
        var existingBySourceKey = existingRows.ToDictionary(r => r.ImportSourceKey);

        var existingUneditedBySourceKey = existingRows.ToDictionary(
            r => r.ImportSourceKey,
            r =>
            {
                var values = ToImportValues(r);
                return ComputeContentHash(values) == r.ImportContentHash
                    || ComputeLegacyContentHash(values) == r.ImportContentHash;
            });

        var decisions = EntityImportPlanner.Plan(existingUneditedBySourceKey, desired);
        return new EntityImportFamilyPlan<SchedulingRuleRateRevisionImportValues, SchedulingRuleRateRevision>(decisions, existingBySourceKey);
    }

    // Returns the (rule, ValidFrom) pairs whose revision content ACTUALLY changed. An Update whose
    // desired content hash equals the hash of the row's current live values is a no-op re-import (it
    // may still refresh a legacy stored hash) and is deliberately not reported, so a repeated import
    // of an unchanged file never triggers a surcharge recalculation.
    public List<RateRevisionChange> Apply(
        EntityImportFamilyPlan<SchedulingRuleRateRevisionImportValues, SchedulingRuleRateRevision> plan,
        IReadOnlyDictionary<string, string> ruleSourceKeyByBoundEntityKey,
        IReadOnlyDictionary<string, Guid> ruleIdBySourceKey)
    {
        var changes = new List<RateRevisionChange>();

        foreach (var decision in plan.Decisions)
        {
            switch (decision.Action)
            {
                case EntityImportAction.Insert:
                    var revision = new SchedulingRuleRateRevision
                    {
                        Id = Guid.NewGuid(),
                        SchedulingRuleId = ImportBoundRuleResolver.ResolveBoundRuleId(decision.SourceKey, ruleSourceKeyByBoundEntityKey, ruleIdBySourceKey) ?? throw new InvalidOperationException($"Region setup: unresolved scheduling rule for rate revision '{decision.SourceKey}'."),
                        ImportSourceKey = decision.SourceKey,
                    };
                    CopyRateRevisionValues(decision.Values, revision);
                    revision.ImportContentHash = decision.ContentHash;
                    _schedulingRuleRateRevisionImportRepository.Add(revision);
                    changes.Add(new RateRevisionChange(revision.SchedulingRuleId, revision.ValidFrom));
                    break;
                case EntityImportAction.Update:
                    var existing = plan.ExistingBySourceKey[decision.SourceKey];
                    var contentChanged = ComputeContentHash(ToImportValues(existing)) != decision.ContentHash;
                    var earliestValidFrom = existing.ValidFrom < decision.Values.ValidFrom ? existing.ValidFrom : decision.Values.ValidFrom;
                    existing.SchedulingRuleId = ImportBoundRuleResolver.ResolveBoundRuleId(decision.SourceKey, ruleSourceKeyByBoundEntityKey, ruleIdBySourceKey) ?? existing.SchedulingRuleId;
                    CopyRateRevisionValues(decision.Values, existing);
                    existing.ImportContentHash = decision.ContentHash;
                    _schedulingRuleRateRevisionImportRepository.Update(existing);
                    if (contentChanged)
                    {
                        changes.Add(new RateRevisionChange(existing.SchedulingRuleId, earliestValidFrom));
                    }
                    break;
                case EntityImportAction.SkipEdited:
                    _logger.LogInformation(
                        "Region setup: scheduling rule rate revision '{SourceKey}' was edited by the customer since the last import, skipping re-apply",
                        decision.SourceKey);
                    break;
            }
        }

        return changes;
    }

    // Used BOTH for the desired hash from the profile file AND for recomputing a stored row's live-value
    // hash (PlanAsync) - both calls must format every field identically.
    private static string ComputeContentHash(SchedulingRuleRateRevisionImportValues values)
    {
        return ImportContentHasher.ComputeHash(
            values.ValidFrom.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
            ImportFieldFormatter.Decimal(values.NightRate),
            ImportFieldFormatter.Decimal(values.HolidayRate),
            ImportFieldFormatter.Decimal(values.We1Rate),
            ImportFieldFormatter.Decimal(values.We2Rate),
            ImportFieldFormatter.Decimal(values.We3Rate),
            values.OvertimeBasis?.ToString() ?? string.Empty,
            values.OvertimeRateMode?.ToString() ?? string.Empty,
            ImportFieldFormatter.Decimal(values.OvertimeTier1AfterHours),
            ImportFieldFormatter.Decimal(values.OvertimeTier1Rate),
            ImportFieldFormatter.Decimal(values.OvertimeTier2AfterHours),
            ImportFieldFormatter.Decimal(values.OvertimeTier2Rate),
            ImportFieldFormatter.Decimal(values.OvertimeTier3AfterHours),
            ImportFieldFormatter.Decimal(values.OvertimeTier3Rate));
    }

    // Legacy fallback: Phase 1 hashed a rate revision over the six surcharge fields only, before Phase 2
    // appended the eight overtime fields to ComputeContentHash. A row imported under Phase 1 whose stored
    // hash still matches this six-field digest is unedited, not customer-edited - without this fallback
    // the widened hasher would freeze it as SkipEdited on the next import. Self-limiting: an unedited
    // match is planned as Update, which rewrites the stored hash with the full 14-field digest, so this
    // branch never fires again for that row.
    private static string ComputeLegacyContentHash(SchedulingRuleRateRevisionImportValues values)
    {
        return ImportContentHasher.ComputeHash(
            values.ValidFrom.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
            ImportFieldFormatter.Decimal(values.NightRate),
            ImportFieldFormatter.Decimal(values.HolidayRate),
            ImportFieldFormatter.Decimal(values.We1Rate),
            ImportFieldFormatter.Decimal(values.We2Rate),
            ImportFieldFormatter.Decimal(values.We3Rate));
    }

    private static SchedulingRuleRateRevisionImportValues ToImportValues(SchedulingRuleRateRevision revision) => new(
        revision.ValidFrom,
        revision.NightRate,
        revision.HolidayRate,
        revision.WE1Rate,
        revision.WE2Rate,
        revision.WE3Rate,
        revision.OvertimeBasis,
        revision.OvertimeRateMode,
        revision.OvertimeTier1AfterHours,
        revision.OvertimeTier1Rate,
        revision.OvertimeTier2AfterHours,
        revision.OvertimeTier2Rate,
        revision.OvertimeTier3AfterHours,
        revision.OvertimeTier3Rate);

    private static void CopyRateRevisionValues(SchedulingRuleRateRevisionImportValues values, SchedulingRuleRateRevision revision)
    {
        revision.ValidFrom = values.ValidFrom;
        revision.NightRate = values.NightRate;
        revision.HolidayRate = values.HolidayRate;
        revision.WE1Rate = values.We1Rate;
        revision.WE2Rate = values.We2Rate;
        revision.WE3Rate = values.We3Rate;
        revision.OvertimeBasis = values.OvertimeBasis;
        revision.OvertimeRateMode = values.OvertimeRateMode;
        revision.OvertimeTier1AfterHours = values.OvertimeTier1AfterHours;
        revision.OvertimeTier1Rate = values.OvertimeTier1Rate;
        revision.OvertimeTier2AfterHours = values.OvertimeTier2AfterHours;
        revision.OvertimeTier2Rate = values.OvertimeTier2Rate;
        revision.OvertimeTier3AfterHours = values.OvertimeTier3AfterHours;
        revision.OvertimeTier3Rate = values.OvertimeTier3Rate;
    }
}
