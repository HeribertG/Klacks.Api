// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default IOvertimeSurchargeCalculator (K3/K4). Runs as C# post-processing next to WorkMacroService's
/// macro execution, never inside the macro DSL itself (see WorkMacroService.ApplyRateModeAdjustments for
/// the analogous K19 precedent this follows). Cumulates a client's other worked hours in the configured
/// basis period (day or week) using a deterministic chronological order (see remarks), places this Work's
/// own hours into the configured tier bands and returns the resulting typed Overtime1/2/3 surcharge
/// portions.
/// </summary>
/// <param name="context">Database access for the OVERTIME_* settings and the client's other Work rows in the basis period</param>
/// <param name="contractDataProvider">Resolves OvertimeThreshold (tier 1's AfterHours fallback) via the existing SchedulingRule/Contract/Settings chain</param>
/// <param name="weekConfiguration">Resolves the configured week start for the Week basis</param>
/// <remarks>
/// Restricted to Work in this stage — WorkChange (corrections/replacements) is deliberately out of
/// scope: it does not represent a client's full worked hours for the day, so folding it into the same
/// cumulative-hours tier placement as Work would need its own "prior hours" concept and is not part of
/// this M/L-effort stage. "Prior hours" (the hours already accrued in the period before this Work) is
/// the sum of the client's OTHER Work rows in the period that sort BEFORE this Work under the
/// deterministic (CurrentDate, StartTime, Id) order — Id only breaks a tie between Works that share the
/// same CurrentDate and StartTime. This order is a partition key, not a claim about real chronological
/// causality: it guarantees the tier bands of every Work in the period tile [0, periodTotal] exactly once
/// with no gaps and no overlap, independent of which Work is (re-)computed first or in what order a bulk
/// recalculation visits them. The naive alternative — summing ALL other Works in the period regardless of
/// order — lets every Work in a multi-Work period place its own hours at the END of the period, so two or
/// more Works each individually reach into the same tier band and each gets billed the same overtime
/// hours twice. Whenever a Work is added, edited or deleted, GetSuccessorWorksAsync identifies the Works
/// that sort AFTER it (whose prior-hours sum just changed) so OvertimeCascadeService — invoked by the
/// Work command handlers AFTER the UnitOfWork has persisted, because both the prior-hours sum and the
/// successor lookup read committed database state, not the EF change tracker — can reprocess them and
/// keep the partition consistent.
/// </remarks>
using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public class OvertimeSurchargeCalculator : IOvertimeSurchargeCalculator
{
    private const string BasisWeek = "week";
    private const string RateModeFixedPerHour = "fixedperhour";
    private const int DaysInWeek = 7;

    private readonly DataBaseContext _context;
    private readonly IClientContractDataProvider _contractDataProvider;
    private readonly IWeekConfiguration _weekConfiguration;

    public OvertimeSurchargeCalculator(
        DataBaseContext context,
        IClientContractDataProvider contractDataProvider,
        IWeekConfiguration weekConfiguration)
    {
        _context = context;
        _contractDataProvider = contractDataProvider;
        _weekConfiguration = weekConfiguration;
    }

    public async Task<OvertimeCalculationResult> CalculateAsync(Work work)
    {
        var config = await LoadConfigAsync(work.ClientId, work.CurrentDate);
        if (config.Tiers.Count == 0)
        {
            return OvertimeCalculationResult.None();
        }

        var priorHours = await GetPriorHoursAsync(work, config.Basis);
        var items = SplitIntoTierBands(priorHours, work.WorkTime, config.Tiers);
        return new OvertimeCalculationResult(items);
    }

    private static List<MacroSurchargeItem> SplitIntoTierBands(
        decimal priorHours,
        decimal workHours,
        IReadOnlyList<OvertimeTierConfig> tiers)
    {
        var items = new List<MacroSurchargeItem>();
        if (workHours <= 0)
        {
            return items;
        }

        var periodEnd = priorHours + workHours;

        for (var i = 0; i < tiers.Count; i++)
        {
            var bandStart = tiers[i].AfterHours;
            var bandEnd = i + 1 < tiers.Count ? tiers[i + 1].AfterHours : decimal.MaxValue;

            var overlapStart = Math.Max(priorHours, bandStart);
            var overlapEnd = Math.Min(periodEnd, bandEnd);
            var bandHours = overlapEnd - overlapStart;
            if (bandHours <= 0)
            {
                continue;
            }

            items.Add(new MacroSurchargeItem(tiers[i].Type, bandHours * tiers[i].Rate));
        }

        return items;
    }

    private async Task<decimal> GetPriorHoursAsync(Work work, OvertimeBasis basis)
    {
        var periodWorks = await GetOtherWorksInPeriodAsync(work, basis);
        return periodWorks
            .Where(w => IsBeforeInOrder(w, work))
            .Sum(w => w.WorkTime);
    }

    public async Task<IReadOnlyList<Work>> GetSuccessorWorksAsync(Work work)
    {
        var config = await LoadConfigAsync(work.ClientId, work.CurrentDate);
        if (config.Tiers.Count == 0)
        {
            return Array.Empty<Work>();
        }

        // Dated revisions (Phase 2) can switch the overtime basis mid-period: the anchor may resolve a
        // day-basis ladder before a revision's ValidFrom while a later sibling in the same week already
        // resolves the revision's week basis. Deriving the successor window from the anchor's own basis
        // would then miss those later-week successors and leave their tier bands permanently stale.
        // Resolving per candidate date would need one config load per Work; instead, as soon as ANY dated
        // revision exists, widen the search to the WIDEST basis (week - see OvertimeBasis, no wider unit
        // exists). The window is at most seven days and re-running an unaffected successor is idempotent,
        // so the widening is cheap and self-healing. Known boundary left open deliberately: an anchor that
        // itself carries no ladder (short-circuited above) whose later same-week sibling first gains a
        // ladder via a mid-week revision is out of the reviewed scenario's scope.
        var searchBasis = config.Basis;
        if (searchBasis != OvertimeBasis.Week && await _context.SchedulingRuleRateRevisions.AnyAsync())
        {
            searchBasis = OvertimeBasis.Week;
        }

        var periodWorks = await GetOtherWorksInPeriodAsync(work, searchBasis);
        return periodWorks
            .Where(w => IsBeforeInOrder(work, w))
            .ToList();
    }

    private async Task<List<Work>> GetOtherWorksInPeriodAsync(Work work, OvertimeBasis basis)
    {
        var (start, end) = basis == OvertimeBasis.Week
            ? await ResolveWeekRangeAsync(work.CurrentDate)
            : (work.CurrentDate, work.CurrentDate);

        return await _context.Work
            .Where(w => w.ClientId == work.ClientId
                && w.Id != work.Id
                && w.AnalyseToken == work.AnalyseToken
                && w.CurrentDate >= start
                && w.CurrentDate <= end)
            .ToListAsync();
    }

    /// <summary>
    /// Deterministic partition order: (CurrentDate, StartTime, Id) ascending. Id only decides between two
    /// Works that share the same CurrentDate and StartTime (mid-shift tie-break) — it carries no meaning
    /// beyond making the order total and reproducible across repeated recomputation.
    /// </summary>
    private static bool IsBeforeInOrder(Work candidate, Work reference)
    {
        var dateComparison = candidate.CurrentDate.CompareTo(reference.CurrentDate);
        if (dateComparison != 0)
        {
            return dateComparison < 0;
        }

        var startTimeComparison = candidate.StartTime.CompareTo(reference.StartTime);
        if (startTimeComparison != 0)
        {
            return startTimeComparison < 0;
        }

        return candidate.Id.CompareTo(reference.Id) < 0;
    }

    private async Task<(DateOnly Start, DateOnly End)> ResolveWeekRangeAsync(DateOnly date)
    {
        var start = await _weekConfiguration.GetWeekStartAsync(date);
        return (start, start.AddDays(DaysInWeek - 1));
    }

    // Resolution order (industry axis, K20/Branchen-Durchstich + dated overtime revisions, Phase 2): a
    // SchedulingRule referenced by the client's active contract that carries a COMPLETE tier 1 (AfterHours
    // + Rate) overrides the global OVERTIME_* settings entirely - rule and settings tiers are never mixed,
    // so an industry preset always defines its own full ladder. The dated revisions layer on top: the
    // latest revision with ValidFrom <= work date is a full snapshot of the rule's overtime ladder, so a
    // revision with a complete tier 1 replaces both the base rule ladder AND the settings, and an
    // applicable revision that omits overtime falls through to the global settings (NOT the base rule
    // ladder - same full-snapshot rule as the five surcharge rates). Without any active rule ladder and
    // without an applicable revision the global settings apply as before.
    private async Task<OvertimeSurchargeConfig> LoadConfigAsync(Guid clientId, DateOnly date)
    {
        // Cheap existence probes first: the contract resolution (2 queries) runs only when at least one
        // active rule carries its own tier ladder OR at least one dated revision exists - installations
        // without industry overtime presets and without revisions keep the pre-industry-axis cost. The
        // revision probe mirrors ClientContractDataProvider.LoadApplicableRateSnapshotsAsync: "any revision
        // at all", not "any revision carrying overtime", because a rate-only revision that is applicable to
        // the date is still a full snapshot whose absent overtime must fall through to settings.
        EffectiveContractData? effectiveData = null;
        var industryOvertimeRuleExists = await _context.SchedulingRules
            .AnyAsync(r => !r.IsDeleted && r.OvertimeTier1AfterHours != null && r.OvertimeTier1Rate > 0);
        var anyRevisionExists = await _context.SchedulingRuleRateRevisions.AnyAsync();
        if (industryOvertimeRuleExists || anyRevisionExists)
        {
            effectiveData = await _contractDataProvider.GetEffectiveContractDataAsync(clientId, date);
            if (effectiveData.SchedulingRuleId.HasValue)
            {
                var rule = await _context.SchedulingRules
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.Id == effectiveData.SchedulingRuleId.Value && !r.IsDeleted);
                if (rule != null)
                {
                    var snapshot = anyRevisionExists
                        ? await ResolveApplicableOvertimeSnapshotAsync(rule.Id, date)
                        : null;
                    var config = ResolveOvertimeConfig(rule, snapshot);
                    if (config != null)
                    {
                        return config;
                    }
                }
            }
        }

        var keys = new[]
        {
            SettingKeys.OvertimeBasis, SettingKeys.OvertimeRateMode,
            SettingKeys.OvertimeTier1AfterHours, SettingKeys.OvertimeTier1Rate,
            SettingKeys.OvertimeTier2AfterHours, SettingKeys.OvertimeTier2Rate,
            SettingKeys.OvertimeTier3AfterHours, SettingKeys.OvertimeTier3Rate,
        };

        var settings = await _context.Settings
            .Where(s => keys.Contains(s.Type))
            .ToDictionaryAsync(s => s.Type, s => s.Value);

        var basis = ParseBasis(settings.GetValueOrDefault(SettingKeys.OvertimeBasis));
        var rateMode = ParseOvertimeRateMode(settings.GetValueOrDefault(SettingKeys.OvertimeRateMode));

        decimal? threshold = null;
        if (!settings.ContainsKey(SettingKeys.OvertimeTier1AfterHours))
        {
            effectiveData ??= await _contractDataProvider.GetEffectiveContractDataAsync(clientId, date);
            threshold = effectiveData.OvertimeThreshold > 0 ? effectiveData.OvertimeThreshold : null;
        }

        var tiers = BuildTiers(settings, threshold);

        return new OvertimeSurchargeConfig
        {
            Basis = basis,
            RateMode = rateMode,
            Tiers = tiers,
        };
    }

    // Latest revision with ValidFrom <= work date for the referenced rule, or null when none applies. This
    // is the same "applicable snapshot" the rate resolution picks in ClientContractDataProvider - a single
    // revision row carries both the surcharge rates and the overtime ladder.
    private async Task<SchedulingRuleRateRevision?> ResolveApplicableOvertimeSnapshotAsync(Guid ruleId, DateOnly date)
    {
        return await _context.SchedulingRuleRateRevisions
            .AsNoTracking()
            .Where(r => r.SchedulingRuleId == ruleId && r.ValidFrom <= date)
            .OrderByDescending(r => r.ValidFrom)
            .FirstOrDefaultAsync();
    }

    // Full-snapshot resolution. snapshot != null: the applicable dated revision replaces the rule ladder
    // entirely - a complete tier 1 yields the revision ladder, an absent overtime block yields null so the
    // caller falls through to the global settings (never the base rule ladder). snapshot == null: the base
    // rule ladder if its tier 1 is complete, else null (pre-revision behaviour, falls to settings).
    private static OvertimeSurchargeConfig? ResolveOvertimeConfig(SchedulingRule rule, SchedulingRuleRateRevision? snapshot)
    {
        if (snapshot != null)
        {
            return snapshot.OvertimeTier1AfterHours != null && snapshot.OvertimeTier1Rate is > 0
                ? BuildConfig(snapshot.OvertimeBasis, snapshot.OvertimeRateMode,
                    snapshot.OvertimeTier1AfterHours, snapshot.OvertimeTier1Rate,
                    snapshot.OvertimeTier2AfterHours, snapshot.OvertimeTier2Rate,
                    snapshot.OvertimeTier3AfterHours, snapshot.OvertimeTier3Rate)
                : null;
        }

        return rule.OvertimeTier1AfterHours != null && rule.OvertimeTier1Rate is > 0
            ? BuildConfig(rule.OvertimeBasis, rule.OvertimeRateMode,
                rule.OvertimeTier1AfterHours, rule.OvertimeTier1Rate,
                rule.OvertimeTier2AfterHours, rule.OvertimeTier2Rate,
                rule.OvertimeTier3AfterHours, rule.OvertimeTier3Rate)
            : null;
    }

    private static OvertimeSurchargeConfig BuildConfig(
        OvertimeBasis? basis,
        SurchargeRateMode? rateMode,
        decimal? tier1AfterHours, decimal? tier1Rate,
        decimal? tier2AfterHours, decimal? tier2Rate,
        decimal? tier3AfterHours, decimal? tier3Rate)
    {
        var candidates = new (decimal? AfterHours, decimal? Rate, SurchargeType Type)[]
        {
            (tier1AfterHours, tier1Rate, SurchargeType.Overtime1),
            (tier2AfterHours, tier2Rate, SurchargeType.Overtime2),
            (tier3AfterHours, tier3Rate, SurchargeType.Overtime3),
        };

        var tiers = candidates
            .Where(c => c.AfterHours.HasValue && c.Rate is > 0)
            .Select(c => new OvertimeTierConfig(c.AfterHours!.Value, c.Rate!.Value, c.Type))
            .OrderBy(t => t.AfterHours)
            .ToList();

        // FixedPerShift is rejected at import time; treating a directly edited value as Multiplier is
        // the same runtime safety net ParseOvertimeRateMode applies to the settings path.
        var resolvedRateMode = rateMode == SurchargeRateMode.FixedPerHour
            ? SurchargeRateMode.FixedPerHour
            : SurchargeRateMode.Multiplier;

        return new OvertimeSurchargeConfig
        {
            Basis = basis ?? OvertimeBasis.Day,
            RateMode = resolvedRateMode,
            Tiers = tiers,
        };
    }

    private static List<OvertimeTierConfig> BuildTiers(IReadOnlyDictionary<string, string> settings, decimal? tier1ThresholdFallback)
    {
        var candidates = new (string AfterHoursKey, string RateKey, SurchargeType Type, decimal? Fallback)[]
        {
            (SettingKeys.OvertimeTier1AfterHours, SettingKeys.OvertimeTier1Rate, SurchargeType.Overtime1, tier1ThresholdFallback),
            (SettingKeys.OvertimeTier2AfterHours, SettingKeys.OvertimeTier2Rate, SurchargeType.Overtime2, null),
            (SettingKeys.OvertimeTier3AfterHours, SettingKeys.OvertimeTier3Rate, SurchargeType.Overtime3, null),
        };

        var tiers = new List<OvertimeTierConfig>();
        foreach (var (afterHoursKey, rateKey, type, fallback) in candidates)
        {
            var afterHours = ParseNullableDecimal(settings.GetValueOrDefault(afterHoursKey)) ?? fallback;
            var rate = ParseNullableDecimal(settings.GetValueOrDefault(rateKey));
            if (!afterHours.HasValue || !rate.HasValue || rate.Value <= 0)
            {
                continue;
            }

            tiers.Add(new OvertimeTierConfig(afterHours.Value, rate.Value, type));
        }

        // Defensive: region-setup validates strictly ascending AfterHours at import time, but settings
        // can also be edited directly. Sorting here only prevents negative/overlapping bands from a
        // misordered edit; it does not attempt to "fix" a mismatched tier-number/rank association.
        return tiers.OrderBy(t => t.AfterHours).ToList();
    }

    private static decimal? ParseNullableDecimal(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static OvertimeBasis ParseBasis(string? value)
    {
        return string.Equals(value, BasisWeek, StringComparison.OrdinalIgnoreCase)
            ? OvertimeBasis.Week
            : OvertimeBasis.Day;
    }

    private static SurchargeRateMode ParseOvertimeRateMode(string? value)
    {
        // FixedPerShift is rejected by RegionSetupService at import time (see AddOvertimeSettings); this
        // is the runtime safety net for a setting edited directly, bypassing that validation. Falling
        // back to Multiplier rather than throwing keeps a live Work save from failing over a malformed
        // config value.
        return string.Equals(value, RateModeFixedPerHour, StringComparison.OrdinalIgnoreCase)
            ? SurchargeRateMode.FixedPerHour
            : SurchargeRateMode.Multiplier;
    }
}
