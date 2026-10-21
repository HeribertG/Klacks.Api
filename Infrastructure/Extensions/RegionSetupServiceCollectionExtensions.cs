// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Registers the region setup: the boot entry point (IRegionSetupService), the marketplace entity-import
/// entry point (IRegionEntityImportService) and their scoped collaborators - section planning, backfills,
/// change detection, post-commit events and one importer per entity family. All scoped, because they
/// share the request/boot scope's DbContext through the repositories and IUnitOfWork.
/// </summary>

using Klacks.Api.Application.Interfaces.Settings;
using Klacks.Api.Infrastructure.Services.Settings;
using Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

namespace Klacks.Api.Infrastructure.Extensions;

public static class RegionSetupServiceCollectionExtensions
{
    public static IServiceCollection AddRegionSetupServices(this IServiceCollection services)
    {
        services.AddScoped<RegionSetupLanguagePlanner>();
        services.AddScoped<RegionSetupSectionPlanner>();
        services.AddScoped<RegionSetupCalendarSelectionResolver>();
        services.AddScoped<RegionSetupSettingsChangeDetector>();
        services.AddScoped<RegionSetupEventPublisher>();
        services.AddScoped<RegionSetupSettingsBackfiller>();

        services.AddScoped<PeriodCapRuleImporter>();
        services.AddScoped<RestDayRotationRuleImporter>();
        services.AddScoped<CounterRuleImporter>();
        services.AddScoped<RestrictedTimeWindowRuleImporter>();
        services.AddScoped<SchedulingRulePresetImporter>();
        services.AddScoped<SchedulingRuleRateRevisionImporter>();
        services.AddScoped<QualificationCatalogImporter>();
        services.AddScoped<MacroImporter>();
        services.AddScoped<RegionEntityImportCoordinator>();

        services.AddScoped<IRegionSetupService, RegionSetupService>();
        services.AddScoped<IRegionEntityImportService, RegionEntityImportService>();

        return services;
    }
}
