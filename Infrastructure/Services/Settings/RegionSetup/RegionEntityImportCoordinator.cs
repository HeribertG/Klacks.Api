// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Entity-import side of the region setup (K20/K5/K16): builds the desired rows of a profile, plans every
/// entity family before any write and applies the plan inside the caller's transaction in one fixed
/// order (presets first, because every bound row resolves its SchedulingRuleId from them; macro
/// demotions flushed before the new holders are staged). compliance.* entity rows and industryProfiles are never gated by a section marker; a per-row
/// ImportSourceKey/ImportContentHash mechanism decides insert/update/skip per row instead, so customer
/// edits are never overwritten. Each entry point keeps its own build/plan sequence, which decides the
/// error a profile with several invalid sections reports first.
/// </summary>
/// <param name="periodCapImporter">Imports PeriodCapRule rows</param>
/// <param name="restDayRotationImporter">Imports RestDayRotationRule rows</param>
/// <param name="counterRuleImporter">Imports CounterRule rows</param>
/// <param name="restrictedTimeWindowImporter">Imports RestrictedTimeWindowRule rows</param>
/// <param name="rulePresetImporter">Imports SchedulingRule preset rows</param>
/// <param name="rateRevisionImporter">Imports dated rate revision rows of the presets</param>
/// <param name="qualificationImporter">Imports Qualification catalog rows</param>
/// <param name="macroImporter">Imports Macro rows and demotes displaced function holders</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Application.Services.Setup;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed class RegionEntityImportCoordinator
{
    private const string PeriodCapsSection = "compliance.periodCaps";
    private const string PeriodCapsSourceKeyPrefix = "region-setup:compliance.periodCaps";
    private const string RestDayRotationsSection = "compliance.restDayRotations";
    private const string RestDayRotationsSourceKeyPrefix = "region-setup:compliance.restDayRotations";
    private const string CounterRulesSection = "compliance.counterRules";
    private const string CounterRulesSourceKeyPrefix = "region-setup:compliance.counterRules";
    private const string RestrictedTimeWindowsSection = "compliance.restrictedTimeWindows";
    private const string RestrictedTimeWindowsSourceKeyPrefix = "region-setup:compliance.restrictedTimeWindows";

    private readonly PeriodCapRuleImporter _periodCapImporter;
    private readonly RestDayRotationRuleImporter _restDayRotationImporter;
    private readonly CounterRuleImporter _counterRuleImporter;
    private readonly RestrictedTimeWindowRuleImporter _restrictedTimeWindowImporter;
    private readonly SchedulingRulePresetImporter _rulePresetImporter;
    private readonly SchedulingRuleRateRevisionImporter _rateRevisionImporter;
    private readonly QualificationCatalogImporter _qualificationImporter;
    private readonly MacroImporter _macroImporter;
    private readonly ILogger<RegionEntityImportCoordinator> _logger;

    public RegionEntityImportCoordinator(
        PeriodCapRuleImporter periodCapImporter,
        RestDayRotationRuleImporter restDayRotationImporter,
        CounterRuleImporter counterRuleImporter,
        RestrictedTimeWindowRuleImporter restrictedTimeWindowImporter,
        SchedulingRulePresetImporter rulePresetImporter,
        SchedulingRuleRateRevisionImporter rateRevisionImporter,
        QualificationCatalogImporter qualificationImporter,
        MacroImporter macroImporter,
        ILogger<RegionEntityImportCoordinator> logger)
    {
        _periodCapImporter = periodCapImporter;
        _restDayRotationImporter = restDayRotationImporter;
        _counterRuleImporter = counterRuleImporter;
        _restrictedTimeWindowImporter = restrictedTimeWindowImporter;
        _rulePresetImporter = rulePresetImporter;
        _rateRevisionImporter = rateRevisionImporter;
        _qualificationImporter = qualificationImporter;
        _macroImporter = macroImporter;
        _logger = logger;
    }

    public async Task<RegionEntityImportPlan> BuildAndPlanBootImportAsync(RegionSetupProfile profile)
    {
        var industryDesired = IndustryProfileImportDesiredBuilder.Build(profile.IndustryProfiles);

        var periodCapDesired = PeriodCapRuleImporter.BuildDesired(
            profile.Compliance?.PeriodCaps, PeriodCapsSection, PeriodCapsSourceKeyPrefix);
        periodCapDesired.AddRange(industryDesired.BoundPeriodCaps);
        var periodCaps = await _periodCapImporter.PlanAsync(periodCapDesired);

        var restDayRotationDesired = RestDayRotationRuleImporter.BuildDesired(
            profile.Compliance?.RestDayRotations, RestDayRotationsSection, RestDayRotationsSourceKeyPrefix);
        restDayRotationDesired.AddRange(industryDesired.BoundRestDayRotations);
        var restDayRotations = await _restDayRotationImporter.PlanAsync(restDayRotationDesired);

        var counterRuleDesired = CounterRuleImporter.BuildDesired(
            profile.Compliance?.CounterRules, CounterRulesSection, CounterRulesSourceKeyPrefix);
        counterRuleDesired.AddRange(industryDesired.BoundCounterRules);
        var counterRules = await _counterRuleImporter.PlanAsync(counterRuleDesired);

        var restrictedTimeWindowDesired = RestrictedTimeWindowRuleImporter.BuildDesired(
            profile.Compliance?.RestrictedTimeWindows, RestrictedTimeWindowsSection, RestrictedTimeWindowsSourceKeyPrefix);
        var restrictedTimeWindows = await _restrictedTimeWindowImporter.PlanAsync(restrictedTimeWindowDesired);
        await _restrictedTimeWindowImporter.WarnOnMissingGroupTagsAsync(restrictedTimeWindowDesired);

        var rulePresets = await _rulePresetImporter.PlanAsync(industryDesired.RulePresets);
        var rateRevisions = await _rateRevisionImporter.PlanAsync(industryDesired.RateRevisions);
        var qualifications = await _qualificationImporter.PlanAsync(industryDesired.Qualifications);

        var macroDesired = _macroImporter.BuildDesired(profile.Macros);
        var macros = await _macroImporter.PlanAsync(macroDesired);

        return new RegionEntityImportPlan(
            periodCaps, restDayRotations, counterRules, restrictedTimeWindows, rulePresets, rateRevisions, qualifications, macros,
            industryDesired.RuleSourceKeyByBoundEntityKey);
    }

    // K20 entity-import path for an installation where every settings section is already fully applied:
    // a missing or no-longer-valid profile file must never block startup on a repeat run, so any read,
    // parse or validation error is logged and yields null. Planning (PlanFullyAppliedImportAsync) is
    // deliberately outside this tolerance - a customer-held macro function slot still fails the start.
    public async Task<RegionEntityImportDesired?> TryBuildDesiredIfPresentAsync(string filePath)
    {
        try
        {
            var profile = await RegionSetupFileReader.ReadProfileAsync(filePath);
            var industryDesired = IndustryProfileImportDesiredBuilder.Build(profile.IndustryProfiles);
            var periodCaps = PeriodCapRuleImporter.BuildDesired(
                profile.Compliance?.PeriodCaps, PeriodCapsSection, PeriodCapsSourceKeyPrefix);
            periodCaps.AddRange(industryDesired.BoundPeriodCaps);
            var restDayRotations = RestDayRotationRuleImporter.BuildDesired(
                profile.Compliance?.RestDayRotations, RestDayRotationsSection, RestDayRotationsSourceKeyPrefix);
            restDayRotations.AddRange(industryDesired.BoundRestDayRotations);
            var macros = _macroImporter.BuildDesired(profile.Macros);
            var counterRules = CounterRuleImporter.BuildDesired(
                profile.Compliance?.CounterRules, CounterRulesSection, CounterRulesSourceKeyPrefix);
            counterRules.AddRange(industryDesired.BoundCounterRules);
            var restrictedTimeWindows = RestrictedTimeWindowRuleImporter.BuildDesired(
                profile.Compliance?.RestrictedTimeWindows, RestrictedTimeWindowsSection, RestrictedTimeWindowsSourceKeyPrefix);
            return new RegionEntityImportDesired(
                periodCaps, restDayRotations, industryDesired.RulePresets, industryDesired.RateRevisions, industryDesired.Qualifications, macros, counterRules, restrictedTimeWindows, industryDesired.RuleSourceKeyByBoundEntityKey);
        }
        catch (InvalidRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "Region setup: entity-import section skipped due to invalid profile content: {Message}",
                ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Region setup: entity-import check skipped due to an error reading or parsing the profile file");
            return null;
        }
    }

    public async Task<RegionEntityImportPlan> PlanFullyAppliedImportAsync(RegionEntityImportDesired desired)
    {
        var periodCaps = await _periodCapImporter.PlanAsync(desired.PeriodCaps);
        var restDayRotations = await _restDayRotationImporter.PlanAsync(desired.RestDayRotations);
        var rulePresets = await _rulePresetImporter.PlanAsync(desired.RulePresets);
        var rateRevisions = await _rateRevisionImporter.PlanAsync(desired.RateRevisions);
        var qualifications = await _qualificationImporter.PlanAsync(desired.Qualifications);
        var macros = await _macroImporter.PlanAsync(desired.Macros);
        var counterRules = await _counterRuleImporter.PlanAsync(desired.CounterRules);
        var restrictedTimeWindows = await _restrictedTimeWindowImporter.PlanAsync(desired.RestrictedTimeWindows);
        await _restrictedTimeWindowImporter.WarnOnMissingGroupTagsAsync(desired.RestrictedTimeWindows);

        return new RegionEntityImportPlan(
            periodCaps, restDayRotations, counterRules, restrictedTimeWindows, rulePresets, rateRevisions, qualifications, macros,
            desired.RuleSourceKeyByBoundEntityKey);
    }

    public async Task<RegionEntityImportPlan> BuildAndPlanPackageImportAsync(RegionSetupProfile profile)
    {
        var industryDesired = IndustryProfileImportDesiredBuilder.Build(profile.IndustryProfiles);
        var macroDesired = _macroImporter.BuildDesired(profile.Macros);

        var periodCaps = await _periodCapImporter.PlanAsync(industryDesired.BoundPeriodCaps);
        var restDayRotations = await _restDayRotationImporter.PlanAsync(industryDesired.BoundRestDayRotations);
        var counterRules = await _counterRuleImporter.PlanAsync(industryDesired.BoundCounterRules);
        var rulePresets = await _rulePresetImporter.PlanAsync(industryDesired.RulePresets);
        var rateRevisions = await _rateRevisionImporter.PlanAsync(industryDesired.RateRevisions);
        var qualifications = await _qualificationImporter.PlanAsync(industryDesired.Qualifications);
        var macros = await _macroImporter.PlanAsync(macroDesired);

        return new RegionEntityImportPlan(
            periodCaps, restDayRotations, counterRules,
            EntityImportFamilyPlan<RestrictedTimeWindowImportValues, RestrictedTimeWindowRule>.Empty,
            rulePresets, rateRevisions, qualifications, macros,
            industryDesired.RuleSourceKeyByBoundEntityKey);
    }

    public async Task<RegionEntityImportApplyResult> ApplyAsync(RegionEntityImportPlan plan)
    {
        var (ruleIdBySourceKey, changedSurchargeRuleIds) = _rulePresetImporter.Apply(plan.RulePresets);
        var rateRevisionChanges = _rateRevisionImporter.Apply(plan.RateRevisions, plan.RuleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
        _periodCapImporter.Apply(plan.PeriodCaps, plan.RuleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
        _restDayRotationImporter.Apply(plan.RestDayRotations, plan.RuleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
        _counterRuleImporter.Apply(plan.CounterRules, plan.RuleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
        _restrictedTimeWindowImporter.Apply(plan.RestrictedTimeWindows);
        _qualificationImporter.Apply(plan.Qualifications);
        await _macroImporter.ApplyDemotionsAsync(plan.Macros.Demotions);
        _macroImporter.ApplyDecisions(plan.Macros.Rows);
        return new RegionEntityImportApplyResult(rateRevisionChanges, changedSurchargeRuleIds);
    }
}
