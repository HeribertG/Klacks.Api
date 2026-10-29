// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Optimizes the route of a container's weekday template and writes the planned visit times, travel times and route
/// info, exactly as the container page's "optimize route" followed by saving would (ContainerRouteWriteService). The
/// container must exist, be visible to the caller and be a container shift; the template must hold at least two
/// visits. Nothing is written when a visit could not be placed on the route.
/// </summary>
/// <param name="containerId">Required. UUID of the container shift (ShiftType.IsContainer).</param>
/// <param name="weekday">Required. ISO weekday 1=Monday..7=Sunday of the template to optimize.</param>
/// <param name="isHoliday">Optional. True to optimize the holiday variant of the weekday; defaults to false.</param>
/// <param name="shiftVisibilityGuard">Answers a hidden container exactly like a missing one</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("optimize_container_route")]
public class OptimizeContainerRouteSkill : BaseSkillImplementation
{
    private const int MinIsoWeekday = 1;
    private const int MaxIsoWeekday = 7;
    private const int IsoSunday = 7;
    private const int StorageSunday = 0;
    private const string DistanceFormat = "0.##";

    private readonly IShiftRepository _shiftRepository;
    private readonly IUserService _userService;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly IContainerRouteWriteService _routeWriteService;

    public OptimizeContainerRouteSkill(
        IShiftRepository shiftRepository,
        IUserService userService,
        IShiftVisibilityGuard shiftVisibilityGuard,
        IContainerRouteWriteService routeWriteService)
    {
        _shiftRepository = shiftRepository;
        _userService = userService;
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _routeWriteService = routeWriteService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var containerId = GetRequiredGuid(parameters, "containerId");
        var isoWeekday = GetRequiredInt(parameters, "weekday");
        var isHoliday = GetParameter<bool>(parameters, "isHoliday", false);

        if (isoWeekday < MinIsoWeekday || isoWeekday > MaxIsoWeekday)
        {
            return SkillResult.Error("weekday must be between 1 (Monday) and 7 (Sunday).");
        }

        var container = await _shiftRepository.Get(containerId);
        if (container is null || !await _shiftVisibilityGuard.IsVisibleAsync(containerId, cancellationToken))
        {
            return SkillResult.Error($"Shift/container not found: {containerId}");
        }

        if (container.ShiftType != ShiftType.IsContainer)
        {
            return SkillResult.Error($"Shift {containerId} is not a container shift (ShiftType must be IsContainer).");
        }

        var storageWeekday = isoWeekday == IsoSunday ? StorageSunday : isoWeekday;
        var instanceId = _userService.GetInstanceId() ?? string.Empty;
        var result = await _routeWriteService.OptimizeAndWriteAsync(
            containerId, storageWeekday, isHoliday, instanceId, cancellationToken);

        if (!result.Success)
        {
            return SkillResult.Error(result.Error!);
        }

        var estimatedNote = result.IsEstimated
            ? " Travel times are straight-line estimates because the routing service was not reachable."
            : string.Empty;

        return SkillResult.SuccessResult(
            new
            {
                ContainerId = containerId,
                ContainerName = container.Name,
                Weekday = isoWeekday,
                IsHoliday = isHoliday,
                result.StartBase,
                result.EndBase,
                result.TotalDistanceKm,
                result.EstimatedTravelTime,
                result.IsEstimated,
                VisitCount = result.Stops.Count,
                result.BreakCount,
                result.Stops,
                Verified = true
            },
            $"Route of container '{container.Name}' for weekday {isoWeekday}{(isHoliday ? " (holiday)" : string.Empty)} optimized: " +
            $"{result.Stops.Count} visit(s), {result.TotalDistanceKm.ToString(DistanceFormat, CultureInfo.InvariantCulture)} km, {result.EstimatedTravelTime} travel and on-site time. " +
            "Visit times and travel times were written to the template and confirmed in the database (verified)." +
            estimatedNote +
            " If the container page is open, reload it before saving there, otherwise its older state overwrites this route.");
    }
}
