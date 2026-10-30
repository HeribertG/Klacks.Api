// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Registers the container autofill: the IContainerAutofillService entry point and its collaborators (candidate
/// locator, travel time floor, greedy selector, ant colony sequencer, route refiner, route planner, planning stage). All scoped like
/// the entry point, because the locator writes coordinates and the travel time floor reads settings through scoped services.
/// </summary>

using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Services.RouteOptimization;
using Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

namespace Klacks.Api.Infrastructure.Extensions;

public static class ContainerAutofillServiceCollectionExtensions
{
    public static IServiceCollection AddContainerAutofillServices(this IServiceCollection services)
    {
        services.AddScoped<ContainerAutofillCandidateLocator>();
        services.AddScoped<IShiftLocationResolver>(sp => sp.GetRequiredService<ContainerAutofillCandidateLocator>());
        services.AddScoped<ContainerAutofillTravelTimeFloor>();
        services.AddScoped<ContainerAutofillGreedySelector>();
        services.AddScoped<ContainerAutofillRouteSequencer>();
        services.AddScoped<ContainerAutofillRouteRefiner>();
        services.AddScoped<ContainerAutofillRoutePlanner>();
        services.AddScoped<ContainerAutofillPlanningStage>();

        services.AddScoped<IContainerAutofillService, ContainerAutofillService>();

        return services;
    }
}
