// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Registers the scenario data operations: the IAnalyseScenarioService entry point and its collaborators (clone,
/// accept checks, delete and promote steps, reject sweep, slot replacement, lookups). All scoped, because every
/// collaborator stages changes on the request scope's DataBaseContext and the caller commits them together.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.ScenarioVariants;
using Klacks.Api.Infrastructure.Repositories.Schedules;
using Klacks.Api.Infrastructure.Services.AnalyseScenarios;

namespace Klacks.Api.Infrastructure.Extensions;

public static class AnalyseScenarioServiceCollectionExtensions
{
    public static IServiceCollection AddAnalyseScenarioServices(this IServiceCollection services)
    {
        services.AddScoped<ScenarioGroupScopeReader>();
        services.AddScoped<ScenarioCloneSourceResolver>();
        services.AddScoped<ScenarioShiftCloner>();
        services.AddScoped<ScenarioWorkCloner>();
        services.AddScoped<ScenarioDayRowCloner>();
        services.AddScoped<ScenarioShiftSatelliteCloner>();
        services.AddScoped<ScenarioSourceOnlyRowCloner>();
        services.AddScoped<ScenarioVariantLinkCloner>();
        services.AddScoped<ScenarioCloneCoordinator>();

        services.AddScoped<ScenarioRowLookup>();
        services.AddScoped<ScenarioOwnedRowStager>();
        services.AddScoped<ScenarioReplacementRequestBook>();
        services.AddScoped<ScenarioDataDiscarder>();

        services.AddScoped<AcceptDayLockPlanBuilder>();
        services.AddScoped<ScenarioAcceptScopeResolver>();
        services.AddScoped<ScenarioRealPlanReplacer>();
        services.AddScoped<ScenarioStalenessDetector>();
        services.AddScoped<ScenarioRealAbsenceGuard>();
        services.AddScoped<ScenarioCoverRedoPresetReader>();
        services.AddScoped<ScenarioAcceptConflictValidator>();
        services.AddScoped<ScenarioLockSkipCounter>();
        services.AddScoped<ScenarioDayRowPromoter>();
        services.AddScoped<ScenarioVariantPromoter>();
        services.AddScoped<ScenarioPromoter>();
        services.AddScoped<ScenarioOpenSlotFinder>();
        services.AddScoped<Application.Interfaces.Schedules.ICoverMoveFactsReader, Services.Schedules.CoverMoveFactsReader>();

        services.AddScoped<ScenarioSlotReplacer>();

        services.AddScoped<IScenarioVariantRepository, ScenarioVariantRepository>();
        services.AddScoped<IVariantShiftWriteGuard, VariantShiftWriteGuard>();
        services.AddScoped<IScenarioVariantShiftCopier, ScenarioVariantShiftCopier>();

        services.AddScoped<ScenarioVariantAcceptGate>();
        services.AddScoped<IAnalyseScenarioService, AnalyseScenarioService>();

        return services;
    }
}
