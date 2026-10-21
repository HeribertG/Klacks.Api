// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// K20 entity import of PeriodCapRule rows from compliance.periodCaps and industry-bound periodCaps:
/// validates the profile entries into desired rows (fixed-period or rolling-average mode), plans
/// insert/update/skip against the stored rows and applies the decisions inside the import transaction.
/// Keep the hashed fields (ComputeContentHash), the applied fields (Apply) and PeriodCapRuleImportValues
/// in sync - a field present in only two of the three silently breaks the customer-edit detection.
/// </summary>
/// <param name="periodCapRuleRepository">Reads and stages PeriodCapRule rows</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Application.Services.Setup;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed class PeriodCapRuleImporter
{
    private const int MinCustomPeriodWeeks = 1;
    private const int MaxCustomPeriodWeeks = 104;
    private const string FixedDecimalFormat = "F4";

    private readonly IPeriodCapRuleRepository _periodCapRuleRepository;
    private readonly ILogger<PeriodCapRuleImporter> _logger;

    public PeriodCapRuleImporter(IPeriodCapRuleRepository periodCapRuleRepository, ILogger<PeriodCapRuleImporter> logger)
    {
        _periodCapRuleRepository = periodCapRuleRepository;
        _logger = logger;
    }

    internal static List<EntityImportDesired<PeriodCapRuleImportValues>> BuildDesired(
        List<RegionSetupPeriodCap>? periodCaps, string sectionPath, string sourceKeyPrefix, HashSet<string>? sharedSeenKeys = null)
    {
        if (periodCaps == null || periodCaps.Count == 0)
        {
            return [];
        }

        var seenKeys = sharedSeenKeys ?? new HashSet<string>();
        var desired = new List<EntityImportDesired<PeriodCapRuleImportValues>>();

        foreach (var cap in periodCaps)
        {
            var hasFixedPeriodFields = cap.Period != null || cap.Scope != null || cap.CapHours != null || cap.CustomPeriodWeeks != null;
            var hasRollingAverageFields = cap.WindowWeeks != null || cap.MaxAverageWeeklyHours != null;

            if (hasFixedPeriodFields && hasRollingAverageFields)
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath} entry mixes fixed-period fields ('period'/'scope'/'capHours'/'customPeriodWeeks') with rolling-average fields ('windowWeeks'/'maxAverageWeeklyHours'); each entry must use exactly one mode.");
            }

            if (cap.WarnAtPercent is < 1 or > 100)
            {
                throw new InvalidRequestException($"Region setup: {sectionPath}.warnAtPercent must be between 1 and 100.");
            }

            if (hasRollingAverageFields)
            {
                desired.Add(BuildRollingAverageDesired(cap, seenKeys, sectionPath, sourceKeyPrefix));
            }
            else if (hasFixedPeriodFields)
            {
                desired.Add(BuildFixedPeriodCapDesired(cap, seenKeys, sectionPath, sourceKeyPrefix));
            }
            else
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath} entry must specify either fixed-period fields ('period'/'scope'/'capHours') or rolling-average fields ('windowWeeks'/'maxAverageWeeklyHours').");
            }
        }

        return desired;
    }

    public async Task<EntityImportFamilyPlan<PeriodCapRuleImportValues, PeriodCapRule>> PlanAsync(
        IReadOnlyList<EntityImportDesired<PeriodCapRuleImportValues>> desired)
    {
        if (desired.Count == 0)
        {
            return EntityImportFamilyPlan<PeriodCapRuleImportValues, PeriodCapRule>.Empty;
        }

        var sourceKeys = desired.Select(d => d.SourceKey).ToList();
        var existingRows = await _periodCapRuleRepository.GetBySourceKeysAsync(sourceKeys);
        var existingBySourceKey = existingRows.ToDictionary(r => r.ImportSourceKey);

        // "Unedited" means the row's CURRENT live values still hash to what was recorded at the last
        // import - NOT whether the file's new desired value happens to equal the old one. Comparing
        // against the file's new hash instead would make it impossible to ever apply a legitimate value
        // change from the file (every changed value would look "edited" even though the customer never
        // touched the row).
        var existingUneditedBySourceKey = existingRows.ToDictionary(
            r => r.ImportSourceKey,
            r => ComputeContentHash(r.Period, r.Scope, r.CapHours, r.WarnAtPercent, r.CustomPeriodWeeks, r.RollingWindowWeeks, r.MaxAverageWeeklyHours) == r.ImportContentHash);

        var decisions = EntityImportPlanner.Plan(existingUneditedBySourceKey, desired);
        return new EntityImportFamilyPlan<PeriodCapRuleImportValues, PeriodCapRule>(decisions, existingBySourceKey);
    }

    public void Apply(
        EntityImportFamilyPlan<PeriodCapRuleImportValues, PeriodCapRule> plan,
        IReadOnlyDictionary<string, string> ruleSourceKeyByBoundEntityKey,
        IReadOnlyDictionary<string, Guid> ruleIdBySourceKey)
    {
        foreach (var decision in plan.Decisions)
        {
            switch (decision.Action)
            {
                case EntityImportAction.Insert:
                    _periodCapRuleRepository.Add(new PeriodCapRule
                    {
                        Id = Guid.NewGuid(),
                        Period = decision.Values.Period,
                        Scope = decision.Values.Scope,
                        CapHours = decision.Values.CapHours,
                        WarnAtPercent = decision.Values.WarnAtPercent,
                        CustomPeriodWeeks = decision.Values.CustomPeriodWeeks,
                        RollingWindowWeeks = decision.Values.RollingWindowWeeks,
                        MaxAverageWeeklyHours = decision.Values.MaxAverageWeeklyHours,
                        SchedulingRuleId = ImportBoundRuleResolver.ResolveBoundRuleId(decision.SourceKey, ruleSourceKeyByBoundEntityKey, ruleIdBySourceKey),
                        ImportSourceKey = decision.SourceKey,
                        ImportContentHash = decision.ContentHash,
                    });
                    break;
                case EntityImportAction.Update:
                    var existing = plan.ExistingBySourceKey[decision.SourceKey];
                    existing.Period = decision.Values.Period;
                    existing.Scope = decision.Values.Scope;
                    existing.CapHours = decision.Values.CapHours;
                    existing.WarnAtPercent = decision.Values.WarnAtPercent;
                    existing.CustomPeriodWeeks = decision.Values.CustomPeriodWeeks;
                    existing.RollingWindowWeeks = decision.Values.RollingWindowWeeks;
                    existing.MaxAverageWeeklyHours = decision.Values.MaxAverageWeeklyHours;
                    existing.SchedulingRuleId = ImportBoundRuleResolver.ResolveBoundRuleId(decision.SourceKey, ruleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
                    existing.ImportContentHash = decision.ContentHash;
                    _periodCapRuleRepository.Update(existing);
                    break;
                case EntityImportAction.SkipEdited:
                    _logger.LogInformation(
                        "Region setup: period cap '{SourceKey}' was edited by the customer since the last import, skipping re-apply",
                        decision.SourceKey);
                    break;
            }
        }
    }

    // Used BOTH when building the desired hash from the profile file (above) AND when recomputing a
    // stored row's live-value hash (PlanAsync) - the two calls must format every field
    // identically, or an unedited row would falsely look edited (or vice versa) on every re-apply. F4
    // pins the decimal scale so a value read back from Postgres (which can normalize trailing zeros
    // differently to how System.Text.Json deserialized it) still hashes to the same string. Period/Scope/
    // CapHours are unused placeholder defaults for a rolling-average row and RollingWindowWeeks/
    // MaxAverageWeeklyHours are unused (null) for a fixed-period row - hashing every field regardless of
    // mode keeps the two call sites trivially in sync and still changes the hash if a row is ever
    // migrated between modes.
    // Backward compatibility: customPeriodWeeks contributes to the hash material ONLY when non-null
    // (no extra empty-string field for null, unlike the older nullable fields above). Every row imported
    // before customWeeks existed was hashed from exactly six fields; appending a seventh empty field for
    // null would change the recomputed live hash of ALL those rows, every one would falsely look
    // customer-edited (SkipEdited) and file updates would never be applied again. A null-vs-set ambiguity
    // cannot arise because the period name ("CustomWeeks" vs "Month"/...) is part of the material and
    // customPeriodWeeks is only ever set together with period CustomWeeks (validated on import).
    internal static string ComputeContentHash(
        PeriodCapPeriod period,
        PeriodCapScope scope,
        decimal capHours,
        int? warnAtPercent,
        int? customPeriodWeeks,
        int? rollingWindowWeeks,
        decimal? maxAverageWeeklyHours)
    {
        var fields = new List<string>
        {
            period.ToString(),
            scope.ToString(),
            capHours.ToString(FixedDecimalFormat, CultureInfo.InvariantCulture),
            warnAtPercent?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            rollingWindowWeeks?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            maxAverageWeeklyHours?.ToString(FixedDecimalFormat, CultureInfo.InvariantCulture) ?? string.Empty,
        };

        if (customPeriodWeeks != null)
        {
            fields.Add(customPeriodWeeks.Value.ToString(CultureInfo.InvariantCulture));
        }

        return ImportContentHasher.ComputeHash(fields.ToArray());
    }

    private static EntityImportDesired<PeriodCapRuleImportValues> BuildFixedPeriodCapDesired(
        RegionSetupPeriodCap cap, HashSet<string> seenKeys, string sectionPath, string sourceKeyPrefix)
    {
        if (cap.Period == null || cap.Scope == null || cap.CapHours == null)
        {
            throw new InvalidRequestException(
                $"Region setup: {sectionPath} fixed-period entry requires 'period', 'scope' and 'capHours'.");
        }

        var period = ValidatePeriod(cap.Period, $"{sectionPath}.period");
        var scope = ValidateScope(cap.Scope, $"{sectionPath}.scope");

        if (cap.CapHours <= 0)
        {
            throw new InvalidRequestException($"Region setup: {sectionPath}.capHours must be greater than zero.");
        }

        if (period == PeriodCapPeriod.CustomWeeks)
        {
            if (cap.CustomPeriodWeeks == null)
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath} entry with period 'customWeeks' requires 'customPeriodWeeks'.");
            }

            if (cap.CustomPeriodWeeks is < MinCustomPeriodWeeks or > MaxCustomPeriodWeeks)
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath}.customPeriodWeeks must be between {MinCustomPeriodWeeks} and {MaxCustomPeriodWeeks}.");
            }
        }
        else if (cap.CustomPeriodWeeks != null)
        {
            throw new InvalidRequestException(
                $"Region setup: {sectionPath}.customPeriodWeeks is only allowed when period is 'customWeeks', not '{cap.Period}'.");
        }

        // Month/Quarter/Year keys MUST keep the historic "{prefix}:{period}:{scope}" shape byte-identical -
        // existing installations match their rows via ImportSourceKey, a changed key would orphan every
        // previously imported row and re-insert a duplicate. CustomWeeks (new, no legacy rows) embeds the
        // window width so that two customWeeks caps with different window lengths (e.g. 4w and 52w, NO)
        // can coexist under distinct keys.
        var sourceKey = period == PeriodCapPeriod.CustomWeeks
            ? $"{sourceKeyPrefix}:{period.ToString().ToLowerInvariant()}{cap.CustomPeriodWeeks!.Value}w:{scope.ToString().ToLowerInvariant()}"
            : $"{sourceKeyPrefix}:{period.ToString().ToLowerInvariant()}:{scope.ToString().ToLowerInvariant()}";
        if (!seenKeys.Add(sourceKey))
        {
            throw new InvalidRequestException(
                period == PeriodCapPeriod.CustomWeeks
                    ? $"Region setup: {sectionPath} contains more than one entry for period 'customWeeks' with {cap.CustomPeriodWeeks} week(s) and scope '{cap.Scope}'."
                    : $"Region setup: {sectionPath} contains more than one entry for period '{cap.Period}' and scope '{cap.Scope}'.");
        }

        var contentHash = ComputeContentHash(period, scope, cap.CapHours.Value, cap.WarnAtPercent, cap.CustomPeriodWeeks, null, null);

        return new EntityImportDesired<PeriodCapRuleImportValues>(
            sourceKey,
            contentHash,
            new PeriodCapRuleImportValues(period, scope, cap.CapHours.Value, cap.WarnAtPercent, cap.CustomPeriodWeeks, null, null));
    }

    private static EntityImportDesired<PeriodCapRuleImportValues> BuildRollingAverageDesired(
        RegionSetupPeriodCap cap, HashSet<string> seenKeys, string sectionPath, string sourceKeyPrefix)
    {
        if (cap.WindowWeeks == null || cap.MaxAverageWeeklyHours == null)
        {
            throw new InvalidRequestException(
                $"Region setup: {sectionPath} rolling-average entry requires 'windowWeeks' and 'maxAverageWeeklyHours'.");
        }

        if (cap.WindowWeeks <= 0)
        {
            throw new InvalidRequestException($"Region setup: {sectionPath}.windowWeeks must be greater than zero.");
        }

        if (cap.MaxAverageWeeklyHours <= 0)
        {
            throw new InvalidRequestException($"Region setup: {sectionPath}.maxAverageWeeklyHours must be greater than zero.");
        }

        var sourceKey = $"{sourceKeyPrefix}:rolling:{cap.WindowWeeks.Value}w";
        if (!seenKeys.Add(sourceKey))
        {
            throw new InvalidRequestException(
                $"Region setup: {sectionPath} contains more than one rolling-average entry for windowWeeks '{cap.WindowWeeks}'.");
        }

        var contentHash = ComputeContentHash(
            PeriodCapPeriod.Month,
            PeriodCapScope.TotalHours,
            0m,
            cap.WarnAtPercent,
            null,
            cap.WindowWeeks,
            cap.MaxAverageWeeklyHours);

        return new EntityImportDesired<PeriodCapRuleImportValues>(
            sourceKey,
            contentHash,
            new PeriodCapRuleImportValues(PeriodCapPeriod.Month, PeriodCapScope.TotalHours, 0m, cap.WarnAtPercent, null, cap.WindowWeeks, cap.MaxAverageWeeklyHours));
    }

    private static PeriodCapPeriod ValidatePeriod(string value, string fieldName)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "month" => PeriodCapPeriod.Month,
            "quarter" => PeriodCapPeriod.Quarter,
            "year" => PeriodCapPeriod.Year,
            "customweeks" => PeriodCapPeriod.CustomWeeks,
            _ => throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} must be 'month', 'quarter', 'year' or 'customWeeks'."),
        };
    }

    private static PeriodCapScope ValidateScope(string value, string fieldName)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "totalhours" => PeriodCapScope.TotalHours,
            "overtimehours" => PeriodCapScope.OvertimeHours,
            _ => throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} must be 'totalHours' or 'overtimeHours'."),
        };
    }
}
