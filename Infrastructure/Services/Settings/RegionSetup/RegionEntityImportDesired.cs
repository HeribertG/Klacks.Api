// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// All desired entity-import rows of one region-setup profile (top-level compliance rows plus the
/// industry-bound ones, presets, revisions, qualifications and macros).
/// </summary>
/// <param name="PeriodCaps">Top-level and industry-bound period caps</param>
/// <param name="RestDayRotations">Top-level and industry-bound rest-day rotations</param>
/// <param name="RulePresets">Scheduling rule presets</param>
/// <param name="RateRevisions">Dated rate revisions of the presets</param>
/// <param name="Qualifications">Qualification catalog rows</param>
/// <param name="Macros">Imported macros</param>
/// <param name="CounterRules">Top-level and industry-bound counter rules</param>
/// <param name="RestrictedTimeWindows">Top-level restricted time windows</param>
/// <param name="RuleSourceKeyByBoundEntityKey">Bound row or revision source key to its preset source key</param>

using Klacks.Api.Application.Services.Setup;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed record RegionEntityImportDesired(
    List<EntityImportDesired<PeriodCapRuleImportValues>> PeriodCaps,
    List<EntityImportDesired<RestDayRotationImportValues>> RestDayRotations,
    List<EntityImportDesired<SchedulingRulePresetImportValues>> RulePresets,
    List<EntityImportDesired<SchedulingRuleRateRevisionImportValues>> RateRevisions,
    List<EntityImportDesired<QualificationCatalogImportValues>> Qualifications,
    List<EntityImportDesired<MacroImportValues>> Macros,
    List<EntityImportDesired<CounterRuleImportValues>> CounterRules,
    List<EntityImportDesired<RestrictedTimeWindowImportValues>> RestrictedTimeWindows,
    Dictionary<string, string> RuleSourceKeyByBoundEntityKey)
{
    public bool IsEmpty =>
        PeriodCaps.Count == 0 && RestDayRotations.Count == 0 && RulePresets.Count == 0
        && RateRevisions.Count == 0 && Qualifications.Count == 0 && Macros.Count == 0 && CounterRules.Count == 0
        && RestrictedTimeWindows.Count == 0;
}
