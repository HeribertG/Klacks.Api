// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Adds a break (an absence item) to a container's weekday template, in the shape the container page stores breaks:
/// with startTime a fixed break (StartItem/EndItem, placed before the first item that starts later), without it a
/// movable break (no StartItem/EndItem, duration as the TimeRange 00:00..00:00+duration, appended) that has no clock
/// time of its own and only reserves its duration when the route is optimized. The container's complete template list is submitted under ContainerTemplateLockScope (which
/// never releases the lock of an open container page) and the break is verified by re-reading: the number of equal
/// break items must have grown by exactly one. With reoptimizeRoute the route is then re-optimized under the same
/// lock; a failed or throwing re-optimization leaves the verified break in place and is reported, not returned as an
/// error, so a retry does not add the break a second time.
/// </summary>
/// <param name="containerId">Required. UUID of the container shift (ShiftType.IsContainer).</param>
/// <param name="weekday">Required. ISO weekday 1=Monday..7=Sunday of the template.</param>
/// <param name="startTime">Optional. Start of a fixed break; omitted for a movable break.</param>
/// <param name="durationMinutes">Optional. Break length in minutes; ContainerVisitDefaults.DefaultMovableBlockMinutes when omitted.</param>
/// <param name="absenceName">Optional. Absence type for containers by name or abbreviation; the seeded unpaid break when omitted.</param>
/// <param name="reoptimizeRoute">Optional. True to re-optimize the route after the break was added; defaults to false.</param>
/// <param name="shiftVisibilityGuard">Answers a hidden container exactly like a missing one</param>
/// <param name="routeWriteService">Re-optimizes the route under the lock this skill holds</param>
/// <param name="logger">Logs a re-optimization that threw after the break was written</param>

using System.Globalization;
using Klacks.Api.Application.Commands.ContainerTemplates;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Queries.ContainerTemplates;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.RouteOptimization.VisitScheduling;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("add_container_template_break")]
public class AddContainerTemplateBreakSkill : BaseSkillImplementation
{
    private const string ContainerIdParameter = "containerId";
    private const string WeekdayParameter = "weekday";
    private const string StartTimeParameter = "startTime";
    private const string DurationMinutesParameter = "durationMinutes";
    private const string AbsenceNameParameter = "absenceName";
    private const string ReoptimizeRouteParameter = "reoptimizeRoute";
    private const string DistanceFormat = "0.##";

    private readonly IShiftRepository _shiftRepository;
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IMediator _mediator;
    private readonly IUserService _userService;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly IContainerRouteWriteService _routeWriteService;
    private readonly ILogger<AddContainerTemplateBreakSkill> _logger;

    public AddContainerTemplateBreakSkill(
        IShiftRepository shiftRepository,
        IAbsenceRepository absenceRepository,
        IMediator mediator,
        IUserService userService,
        IShiftVisibilityGuard shiftVisibilityGuard,
        IContainerRouteWriteService routeWriteService,
        ILogger<AddContainerTemplateBreakSkill> logger)
    {
        _shiftRepository = shiftRepository;
        _absenceRepository = absenceRepository;
        _mediator = mediator;
        _userService = userService;
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _routeWriteService = routeWriteService;
        _logger = logger;
    }

    private sealed record BreakRequest(
        Guid ContainerId, int IsoWeekday, TimeOnly? StartTime, int DurationMinutes, bool ReoptimizeRoute);

    private sealed record AddedBreak(ContainerTemplateResource Template, ContainerTemplateItemResource Item);

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var (request, requestError) = ReadRequest(context, parameters);
        if (requestError is not null)
        {
            return SkillResult.Error(requestError);
        }

        var container = await _shiftRepository.Get(request!.ContainerId);
        if (container is null || !await _shiftVisibilityGuard.IsVisibleAsync(request.ContainerId, cancellationToken))
        {
            return SkillResult.Error($"Shift/container not found: {request.ContainerId}");
        }

        if (container.ShiftType != ShiftType.IsContainer)
        {
            return SkillResult.Error($"Shift {request.ContainerId} is not a container shift (ShiftType must be IsContainer).");
        }

        var (absence, absenceError) = ContainerBreakAbsenceResolver.Resolve(
            await _absenceRepository.List(), GetParameter<string>(parameters, AbsenceNameParameter));
        if (absence is null)
        {
            return SkillResult.Error(absenceError!);
        }

        var instanceId = _userService.GetInstanceId() ?? string.Empty;
        await using var lockScope = await ContainerTemplateLockScope.AcquireAsync(
            _mediator, request.ContainerId, instanceId, cancellationToken);
        if (!lockScope.Acquired)
        {
            return SkillResult.Error("The container is currently being edited by another user.");
        }

        var (added, writeError) = await AddBreakAsync(request, absence, cancellationToken);
        if (writeError is not null)
        {
            return SkillResult.Error(writeError);
        }

        var route = request.ReoptimizeRoute
            ? await ReoptimizeAsync(lockScope, request.ContainerId, added!.Template, cancellationToken)
            : null;

        return BuildResult(request, container, absence, added!, route, context.UserLanguage);
    }

    private async Task<ContainerRouteWriteResult> ReoptimizeAsync(
        ContainerTemplateLockScope lockScope, Guid containerId, ContainerTemplateResource template,
        CancellationToken cancellationToken)
    {
        try
        {
            return await _routeWriteService.OptimizeAndWriteUnderHeldLockAsync(
                lockScope, containerId, template.Weekday, template.IsHoliday, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Re-optimizing container {ContainerId} after adding a break failed", containerId);
            return ContainerRouteWriteResult.Failure("an unexpected error occurred, the visit times were not rewritten.");
        }
    }

    private static (BreakRequest? Request, string? Error) ReadRequest(
        SkillExecutionContext context, Dictionary<string, object> parameters)
    {
        var containerId = GetRequiredGuid(parameters, ContainerIdParameter);
        var isoWeekday = GetRequiredInt(parameters, WeekdayParameter);
        if (!IsoWeekday.IsValid(isoWeekday))
        {
            return (null, IsoWeekday.RangeError);
        }

        var durationMinutes = GetParameter<int?>(parameters, DurationMinutesParameter)
            ?? ContainerVisitDefaults.DefaultMovableBlockMinutes;
        if (durationMinutes < ContainerVisitDefaults.MinBlockDurationMinutes || durationMinutes >= DayTimeConstants.MinutesPerDay)
        {
            return (null, $"durationMinutes must be between {ContainerVisitDefaults.MinBlockDurationMinutes} and {DayTimeConstants.MinutesPerDay - 1}.");
        }

        var startTimeRaw = GetParameter<string>(parameters, StartTimeParameter);
        TimeOnly? startTime = null;
        if (!string.IsNullOrWhiteSpace(startTimeRaw))
        {
            if (!SkillCalendarStringParser.TryParseTimeOnly(startTimeRaw, context.UserLanguage, out var parsed))
            {
                return (null, $"startTime '{startTimeRaw}' is not a valid time (expected e.g. 12:00). Omit it for a movable break.");
            }

            startTime = parsed;
        }

        var reoptimizeRoute = GetParameter<bool>(parameters, ReoptimizeRouteParameter, false);
        return (new BreakRequest(containerId, isoWeekday, startTime, durationMinutes, reoptimizeRoute), null);
    }

    private async Task<(AddedBreak? Added, string? Error)> AddBreakAsync(
        BreakRequest request, Absence absence, CancellationToken cancellationToken)
    {
        var templates = await _mediator.Send(new GetContainerTemplatesQuery(request.ContainerId), cancellationToken);
        var template = ContainerTemplateSelection.ForWeekday(templates, IsoWeekday.ToStorage(request.IsoWeekday));
        if (template is null)
        {
            return (null, "No container template configured for this weekday yet — configure the container template for this weekday first.");
        }

        var item = BuildBreakItem(absence.Id, request.StartTime, request.DurationMinutes);
        var equalBefore = template.ContainerTemplateItems.Count(existing => IsEqualBreak(existing, item));
        template.ContainerTemplateItems = InsertBreak(template.ContainerTemplateItems, item);

        try
        {
            await _mediator.Send(new PutContainerTemplatesCommand(request.ContainerId, templates), cancellationToken);
        }
        catch (ContainerLockedException ex)
        {
            return (null, ex.Message);
        }

        var reread = await _mediator.Send(new GetContainerTemplatesQuery(request.ContainerId), cancellationToken);
        var stored = reread.FirstOrDefault(t =>
            t.Weekday == template.Weekday
            && t.IsHoliday == template.IsHoliday
            && t.IsWeekdayAndHoliday == template.IsWeekdayAndHoliday);
        var equalAfter = stored?.ContainerTemplateItems.Count(existing => IsEqualBreak(existing, item)) ?? 0;

        return equalAfter == equalBefore + 1
            ? (new AddedBreak(template, item), null)
            : (null, "Verification failed: the break could not be confirmed in the container template after the write.");
    }

    private static ContainerTemplateItemResource BuildBreakItem(Guid absenceId, TimeOnly? startTime, int durationMinutes)
    {
        var item = new ContainerTemplateItemResource
        {
            Id = Guid.Empty,
            ShiftId = null,
            AbsenceId = absenceId,
            TransportMode = TransportMode.ByCar
        };

        if (startTime.HasValue)
        {
            item.StartItem = startTime.Value;
            item.EndItem = startTime.Value.AddMinutes(durationMinutes);
            return item;
        }

        var movableStart = ContainerVisitScheduler.ClockTime(ContainerVisitDefaults.MovableBlockDefaultStartMinutes);
        item.TimeRangeStartItem = movableStart;
        item.TimeRangeEndItem = movableStart.AddMinutes(durationMinutes);
        return item;
    }

    private static List<ContainerTemplateItemResource> InsertBreak(
        IReadOnlyList<ContainerTemplateItemResource> items, ContainerTemplateItemResource breakItem)
    {
        return breakItem.StartItem.HasValue
            ? ContainerTemplateItemOrder.Interleave<ContainerTemplateItemResource>([breakItem], items, ContainerTemplateItemStart.Minutes)
            : [.. items, breakItem];
    }

    private static bool IsEqualBreak(ContainerTemplateItemResource stored, ContainerTemplateItemResource expected) =>
        stored.AbsenceId == expected.AbsenceId
        && !stored.ShiftId.HasValue
        && stored.StartItem == expected.StartItem
        && stored.EndItem == expected.EndItem
        && stored.TimeRangeStartItem == expected.TimeRangeStartItem
        && stored.TimeRangeEndItem == expected.TimeRangeEndItem;

    private static SkillResult BuildResult(
        BreakRequest request, Shift container, Absence absence, AddedBreak added, ContainerRouteWriteResult? route, string? language)
    {
        var absenceName = absence.Name.GetValueOrFirstAvailable(language);
        var isFixed = request.StartTime.HasValue;
        var startText = isFixed ? ClockText(added.Item.StartItem!.Value) : null;
        var endText = isFixed ? ClockText(added.Item.EndItem!.Value) : null;
        var breakText = isFixed
            ? $"fixed break '{absenceName}' {startText}-{endText}"
            : $"movable break '{absenceName}' of {request.DurationMinutes} min (no clock time of its own; route optimization reserves its duration between the visits)";

        var message = $"Added the {breakText} to the container template of '{container.Name}' for weekday {request.IsoWeekday}" +
            $"{(added.Template.IsHoliday ? " (holiday variant)" : string.Empty)} and confirmed it in the database (verified)." +
            RouteText(request.ReoptimizeRoute, route) +
            " If the container page is open, reload it before saving there, otherwise its older state overwrites this change.";

        return SkillResult.SuccessResult(
            new
            {
                ContainerId = request.ContainerId,
                ContainerName = container.Name,
                Weekday = request.IsoWeekday,
                added.Template.IsHoliday,
                AbsenceId = absence.Id,
                AbsenceName = absenceName,
                IsFixed = isFixed,
                StartTime = startText,
                EndTime = endText,
                request.DurationMinutes,
                Verified = true,
                RouteReoptimized = route?.Success ?? false,
                RouteError = route?.Error,
                RouteStops = route is { Success: true } ? route.Stops : null
            },
            message);
    }

    private static string RouteText(bool reoptimizeRequested, ContainerRouteWriteResult? route)
    {
        const string visitTimesUnchanged =
            " Visit times were not changed; a visit may overlap the break until the route is re-optimized.";

        if (!reoptimizeRequested || route is null)
        {
            return visitTimesUnchanged;
        }

        if (!route.Success)
        {
            return $" The break stays saved, but re-optimizing the route failed: {route.Error}" + visitTimesUnchanged;
        }

        return $" The route was re-optimized: {route.Stops.Count} visit(s), " +
            $"{route.TotalDistanceKm.ToString(DistanceFormat, CultureInfo.InvariantCulture)} km; the new visit and travel " +
            "times were re-read and confirmed (verified).";
    }

    private static string ClockText(TimeOnly time) =>
        time.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture);
}
