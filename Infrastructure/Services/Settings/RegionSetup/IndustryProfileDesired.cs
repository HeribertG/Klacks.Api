// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Desired entity rows of the industryProfiles map of a region-setup profile.
/// </summary>
/// <param name="RulePresets">Scheduling rule presets of all industries</param>
/// <param name="RateRevisions">Dated rate revisions of the presets</param>
/// <param name="Qualifications">Qualification catalog rows of all industries</param>
/// <param name="BoundPeriodCaps">Period caps bound to their industry's single preset</param>
/// <param name="BoundRestDayRotations">Rest-day rotations bound to their industry's single preset</param>
/// <param name="BoundCounterRules">Counter rules bound to their industry's single preset</param>
/// <param name="RuleSourceKeyByBoundEntityKey">Bound row or revision source key to its preset source key</param>

using Klacks.Api.Application.Services.Setup;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed record IndustryProfileDesired(
    List<EntityImportDesired<SchedulingRulePresetImportValues>> RulePresets,
    List<EntityImportDesired<SchedulingRuleRateRevisionImportValues>> RateRevisions,
    List<EntityImportDesired<QualificationCatalogImportValues>> Qualifications,
    List<EntityImportDesired<PeriodCapRuleImportValues>> BoundPeriodCaps,
    List<EntityImportDesired<RestDayRotationImportValues>> BoundRestDayRotations,
    List<EntityImportDesired<CounterRuleImportValues>> BoundCounterRules,
    Dictionary<string, string> RuleSourceKeyByBoundEntityKey);
