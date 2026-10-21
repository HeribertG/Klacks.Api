// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Planned decisions of every entity-import family of one region-setup run, computed (and validated)
/// before the import transaction opens and applied inside it by RegionEntityImportCoordinator.ApplyAsync.
/// </summary>
/// <param name="PeriodCaps">Planned period cap rows</param>
/// <param name="RestDayRotations">Planned rest-day rotation rows</param>
/// <param name="CounterRules">Planned counter rule rows</param>
/// <param name="RestrictedTimeWindows">Planned restricted time window rows (empty on the marketplace path)</param>
/// <param name="RulePresets">Planned scheduling rule preset rows</param>
/// <param name="RateRevisions">Planned rate revision rows</param>
/// <param name="Qualifications">Planned qualification catalog rows</param>
/// <param name="Macros">Planned macro rows and function-holder demotions</param>
/// <param name="RuleSourceKeyByBoundEntityKey">Bound row or revision source key to its preset source key</param>

using Klacks.Api.Application.Services.Setup;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed record RegionEntityImportPlan(
    EntityImportFamilyPlan<PeriodCapRuleImportValues, PeriodCapRule> PeriodCaps,
    EntityImportFamilyPlan<RestDayRotationImportValues, RestDayRotationRule> RestDayRotations,
    EntityImportFamilyPlan<CounterRuleImportValues, CounterRule> CounterRules,
    EntityImportFamilyPlan<RestrictedTimeWindowImportValues, RestrictedTimeWindowRule> RestrictedTimeWindows,
    EntityImportFamilyPlan<SchedulingRulePresetImportValues, SchedulingRule> RulePresets,
    EntityImportFamilyPlan<SchedulingRuleRateRevisionImportValues, SchedulingRuleRateRevision> RateRevisions,
    EntityImportFamilyPlan<QualificationCatalogImportValues, Qualification> Qualifications,
    MacroImportPlan Macros,
    IReadOnlyDictionary<string, string> RuleSourceKeyByBoundEntityKey)
{
    public bool HasWrites =>
        PeriodCaps.HasWrites
        || RestDayRotations.HasWrites
        || RulePresets.HasWrites
        || RateRevisions.HasWrites
        || Qualifications.HasWrites
        || Macros.Rows.HasWrites
        || CounterRules.HasWrites
        || RestrictedTimeWindows.HasWrites;
}
