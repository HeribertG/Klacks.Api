// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Registers the checks of the schedule timeline worker. All scoped: the worker is a singleton hosted service and
/// resolves them from its own scope on every attempt of a job, so they share that attempt's DataBaseContext with
/// the evaluators they call. Registered regardless of whether the hosted worker is switched on.
/// </summary>

using Klacks.Api.Infrastructure.Services.ScheduleTimeline;

namespace Klacks.Api.Infrastructure.Extensions;

public static class ScheduleTimelineServiceCollectionExtensions
{
    public static IServiceCollection AddScheduleTimelineChecks(this IServiceCollection services)
    {
        services.AddScoped<ScheduleTimelineCheckLoader>();
        services.AddScoped<ScheduleTimelineComplianceRules>();
        services.AddScoped<ScheduleTimelineSingleCheck>();
        services.AddScoped<ScheduleTimelineRangeCheck>();

        return services;
    }
}
