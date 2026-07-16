// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IOvertimeConfigResolver"/>. Owns the system's single overtime-definition
/// resolution, extracted from OvertimeSurchargeCalculator so the surcharge engine (K3/K4) and the
/// OvertimeHours period cap evaluate against the SAME ladder instead of two diverging definitions.
/// </summary>
/// <param name="context">Database access for the SchedulingRule ladders, dated revision snapshots and the OVERTIME_* settings</param>
/// <param name="contractDataProvider">Resolves the client's active SchedulingRule and the OvertimeThreshold (tier 1's AfterHours fallback)</param>
using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public class OvertimeConfigResolver : IOvertimeConfigResolver
{
    private const string BasisWeek = "week";
    private const string RateModeFixedPerHour = "fixedperhour";

    private readonly DataBaseContext _context;
    private readonly IClientContractDataProvider _contractDataProvider;

    public OvertimeConfigResolver(
        DataBaseContext context,
        IClientContractDataProvider contractDataProvider)
    {
        _context = context;
        _contractDataProvider = contractDataProvider;
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
    public async Task<OvertimeSurchargeConfig> ResolveAsync(Guid clientId, DateOnly date)
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
