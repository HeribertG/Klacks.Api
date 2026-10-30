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
/// With allWeekdays the same break is added to every non-holiday weekday template of the container under ONE lock and
/// ONE update of the complete template list (holiday templates stay untouched). A fixed break is skipped for a template
/// whose last visit ends before the break starts (a movable break is never skipped); every skipped weekday is reported
/// with its reason. The update commits per template, so the write and the verification run in ONE database transaction:
/// after the write every changed template is re-read and its number of equal break items must have grown by exactly one;
/// a mismatch rolls every template back and is returned as an error naming
/// the weekdays; a write that throws on a later template rolls every template back as well, so nothing is half written and a retry cannot add a break twice.
/// With reoptimizeRoute every changed template is re-optimized under the same lock, one after the other; a failed or
/// throwing re-optimization is reported for its weekday and leaves the verified break in place.
/// </summary>
/// <param name="containerId">Required. UUID of the container shift (ShiftType.IsContainer).</param>
/// <param name="weekday">Required unless allWeekdays is true (then ignored). ISO weekday 1=Monday..7=Sunday of the template.</param>
/// <param name="allWeekdays">Optional. True to add the break to every non-holiday weekday template; defaults to false.</param>
/// <param name="startTime">Optional. Start of a fixed break; omitted for a movable break.</param>
/// <param name="durationMinutes">Optional. Break length in minutes; ContainerVisitDefaults.DefaultMovableBlockMinutes when omitted.</param>
/// <param name="absenceName">Optional. Absence type for containers by name or abbreviation; the seeded unpaid break when omitted.</param>
/// <param name="reoptimizeRoute">Optional. True to re-optimize the route after the break was added; defaults to false.</param>
/// <param name="shiftVisibilityGuard">Answers a hidden container exactly like a missing one</param>
/// <param name="unitOfWork">Runs the all-weekdays write and its verification in one transaction</param>
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

[SkillImplementation(SkillName)]
public class AddContainerTemplateBreakSkill : BaseSkillImplementation
{
    private const string SkillName = "add_container_template_break";
    private const string ContainerIdParameter = "containerId";
    private const string WeekdayParameter = "weekday";
    private const string StartTimeParameter = "startTime";
    private const string DurationMinutesParameter = "durationMinutes";
    private const string AbsenceNameParameter = "absenceName";
    private const string ReoptimizeRouteParameter = "reoptimizeRoute";
    private const string AllWeekdaysParameter = "allWeekdays";
    private const string DistanceFormat = "0.##";
    private const int NoSingleWeekday = 0;
    private const string WeekdaySeparator = ", ";
    private const string NoWeekdayTemplatesMessage =
        "The container has no weekday template yet (holiday templates are not changed) — configure the container template for the weekdays first.";
    private const string VerificationFailedForWeekdaysMessage =
        "Verification failed: the break could not be confirmed in the container template of {0} after the write; " +
        "the whole write was rolled back and nothing was changed. Reload the container page and check the templates.";
    private const string AllSkippedMessage =
        "No break was added: every weekday template was skipped ({0}).";
    private const string SkippedAfterLastVisitReason =
        "its last visit ends at {0}, before the break starts at {1}";

    private readonly IShiftRepository _shiftRepository;
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IMediator _mediator;
    private readonly IUserService _userService;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IContainerRouteWriteService _routeWriteService;
    private readonly ILogger<AddContainerTemplateBreakSkill> _logger;

    public AddContainerTemplateBreakSkill(
        IShiftRepository shiftRepository,
        IAbsenceRepository absenceRepository,
        IMediator mediator,
        IUserService userService,
        IShiftVisibilityGuard shiftVisibilityGuard,
        IUnitOfWork unitOfWork,
        IContainerRouteWriteService routeWriteService,
        ILogger<AddContainerTemplateBreakSkill> logger)
    {
        _shiftRepository = shiftRepository;
        _absenceRepository = absenceRepository;
        _mediator = mediator;
        _userService = userService;
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _unitOfWork = unitOfWork;
        _routeWriteService = routeWriteService;
        _logger = logger;
    }

    private sealed record BreakRequest(
        Guid ContainerId, int IsoWeekday, TimeOnly? StartTime, int DurationMinutes, bool ReoptimizeRoute, bool AllWeekdays);

    private sealed record AddedBreak(ContainerTemplateResource Template, ContainerTemplateItemResource Item);

    private sealed record PlannedWeekday(ContainerTemplateResource Template, ContainerTemplateItemResource Item, int EqualBefore);

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

        if (request.AllWeekdays)
        {
            return await AddToAllWeekdaysAsync(request, container, absence, lockScope, context.UserLanguage, cancellationToken);
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
        var allWeekdays = GetParameter<bool>(parameters, AllWeekdaysParameter, false);
        var isoWeekday = NoSingleWeekday;
        if (!allWeekdays)
        {
            isoWeekday = GetRequiredInt(parameters, WeekdayParameter);
            if (!IsoWeekday.IsValid(isoWeekday))
            {
                return (null, IsoWeekday.RangeError);
            }
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
        return (new BreakRequest(containerId, isoWeekday, startTime, durationMinutes, reoptimizeRoute, allWeekdays), null);
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

        var item = ContainerTemplateBreakItems.Build(absence.Id, request.StartTime, request.DurationMinutes);
        var equalBefore = ContainerTemplateBreakItems.CountEqual(template.ContainerTemplateItems, item);
        template.ContainerTemplateItems = ContainerTemplateBreakItems.Insert(template.ContainerTemplateItems, item);

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
        var equalAfter = stored is null ? 0 : ContainerTemplateBreakItems.CountEqual(stored.ContainerTemplateItems, item);

        return equalAfter == equalBefore + 1
            ? (new AddedBreak(template, item), null)
            : (null, "Verification failed: the break could not be confirmed in the container template after the write.");
    }

    private async Task<SkillResult> AddToAllWeekdaysAsync(
        BreakRequest request, Shift container, Absence absence, ContainerTemplateLockScope lockScope, string? language,
        CancellationToken cancellationToken)
    {
        var templates = await _mediator.Send(new GetContainerTemplatesQuery(request.ContainerId), cancellationToken);
        var weekdayTemplates = templates.Where(t => !t.IsHoliday).OrderBy(t => IsoWeekday.FromStorage(t.Weekday)).ToList();
        if (weekdayTemplates.Count == 0)
        {
            return SkillResult.Error(NoWeekdayTemplatesMessage);
        }

        var (planned, skipped) = PlanWeekdays(request, absence, weekdayTemplates);
        if (planned.Count == 0)
        {
            return SkillResult.Error(string.Format(
                AllSkippedMessage, string.Join(WeekdaySeparator, skipped.Select(DescribeSkipped))));
        }

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(
                () => PutAndVerifyAsync(request.ContainerId, templates, planned, cancellationToken));
        }
        catch (Exception ex)
        {
            _unitOfWork.DiscardTrackedChanges();
            if (ex is ContainerLockedException or SkillVerificationException)
            {
                return SkillResult.Error(ex.Message);
            }

            throw;
        }

        var outcomes = new List<ContainerBreakWeekdayOutcome>(skipped);
        foreach (var weekday in planned)
        {
            outcomes.Add(await CompleteWeekdayAsync(request, lockScope, weekday, cancellationToken));
        }

        return BuildAllWeekdaysResult(
            request, container, absence, outcomes.OrderBy(o => o.Weekday).ToList(), planned[0].Item, language);
    }

    private async Task<bool> PutAndVerifyAsync(
        Guid containerId, List<ContainerTemplateResource> templates, List<PlannedWeekday> planned,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new PutContainerTemplatesCommand(containerId, templates), cancellationToken);

        var unconfirmed = await FindUnconfirmedWeekdaysAsync(containerId, planned, cancellationToken);
        if (unconfirmed.Count > 0)
        {
            throw new SkillVerificationException(
                SkillName,
                string.Format(VerificationFailedForWeekdaysMessage, string.Join(WeekdaySeparator, unconfirmed.Select(WeekdayName))));
        }

        return true;
    }

    private static (List<PlannedWeekday> Planned, List<ContainerBreakWeekdayOutcome> Skipped) PlanWeekdays(
        BreakRequest request, Absence absence, List<ContainerTemplateResource> weekdayTemplates)
    {
        var planned = new List<PlannedWeekday>();
        var skipped = new List<ContainerBreakWeekdayOutcome>();

        foreach (var template in weekdayTemplates)
        {
            var isoWeekday = IsoWeekday.FromStorage(template.Weekday);
            var skipReason = SkipReason(template, request.StartTime);
            if (skipReason is not null)
            {
                skipped.Add(new ContainerBreakWeekdayOutcome(isoWeekday, false, skipReason, false, null));
                continue;
            }

            var item = ContainerTemplateBreakItems.Build(absence.Id, request.StartTime, request.DurationMinutes);
            var equalBefore = ContainerTemplateBreakItems.CountEqual(template.ContainerTemplateItems, item);
            template.ContainerTemplateItems = ContainerTemplateBreakItems.Insert(template.ContainerTemplateItems, item);
            planned.Add(new PlannedWeekday(template, item, equalBefore));
        }

        return (planned, skipped);
    }

    private static string? SkipReason(ContainerTemplateResource template, TimeOnly? breakStart)
    {
        if (!breakStart.HasValue)
        {
            return null;
        }

        var lastVisitEnd = ContainerTemplateBreakItems.LastVisitEnd(template);
        return lastVisitEnd.HasValue && lastVisitEnd.Value < breakStart.Value
            ? string.Format(SkippedAfterLastVisitReason, ClockText(lastVisitEnd.Value), ClockText(breakStart.Value))
            : null;
    }

    private async Task<List<int>> FindUnconfirmedWeekdaysAsync(
        Guid containerId, List<PlannedWeekday> planned, CancellationToken cancellationToken)
    {
        var reread = await _mediator.Send(new GetContainerTemplatesQuery(containerId), cancellationToken);
        var unconfirmed = new List<int>();

        foreach (var weekday in planned)
        {
            var template = weekday.Template;
            var stored = reread.FirstOrDefault(t =>
                t.Weekday == template.Weekday
                && t.IsHoliday == template.IsHoliday
                && t.IsWeekdayAndHoliday == template.IsWeekdayAndHoliday);
            var equalAfter = stored is null ? 0 : ContainerTemplateBreakItems.CountEqual(stored.ContainerTemplateItems, weekday.Item);
            if (equalAfter != weekday.EqualBefore + 1)
            {
                unconfirmed.Add(IsoWeekday.FromStorage(template.Weekday));
            }
        }

        return unconfirmed;
    }

    private async Task<ContainerBreakWeekdayOutcome> CompleteWeekdayAsync(
        BreakRequest request, ContainerTemplateLockScope lockScope, PlannedWeekday weekday, CancellationToken cancellationToken)
    {
        var isoWeekday = IsoWeekday.FromStorage(weekday.Template.Weekday);
        if (!request.ReoptimizeRoute)
        {
            return new ContainerBreakWeekdayOutcome(isoWeekday, true, null, false, null);
        }

        var route = await ReoptimizeAsync(lockScope, request.ContainerId, weekday.Template, cancellationToken);
        return new ContainerBreakWeekdayOutcome(isoWeekday, true, null, route.Success, route.Error);
    }

    private static SkillResult BuildAllWeekdaysResult(
        BreakRequest request, Shift container, Absence absence, List<ContainerBreakWeekdayOutcome> outcomes,
        ContainerTemplateItemResource sampleItem, string? language)
    {
        var absenceName = absence.Name.GetValueOrFirstAvailable(language);
        var isFixed = request.StartTime.HasValue;
        var startText = isFixed ? ClockText(sampleItem.StartItem!.Value) : null;
        var endText = isFixed ? ClockText(sampleItem.EndItem!.Value) : null;
        var added = outcomes.Where(o => o.Added).ToList();
        var skipped = outcomes.Where(o => !o.Added).ToList();

        var message = $"Added the {DescribeBreak(request, absenceName, startText, endText)} to the container template of " +
            $"'{container.Name}' for {added.Count} weekday(s) ({string.Join(WeekdaySeparator, added.Select(o => WeekdayName(o.Weekday)))}) " +
            "and confirmed each one in the database (verified)." +
            SkippedText(skipped) +
            AllWeekdaysRouteText(request.ReoptimizeRoute, added) +
            " If the container page is open, reload it before saving there, otherwise its older state overwrites this change.";

        return SkillResult.SuccessResult(
            new
            {
                ContainerId = request.ContainerId,
                ContainerName = container.Name,
                AllWeekdays = true,
                AbsenceId = absence.Id,
                AbsenceName = absenceName,
                IsFixed = isFixed,
                StartTime = startText,
                EndTime = endText,
                request.DurationMinutes,
                Verified = true,
                Weekdays = outcomes
            },
            message);
    }

    private static string SkippedText(List<ContainerBreakWeekdayOutcome> skipped) =>
        skipped.Count == 0
            ? string.Empty
            : " Skipped: " + string.Join("; ", skipped.Select(DescribeSkipped)) + ".";

    private static string DescribeSkipped(ContainerBreakWeekdayOutcome outcome) =>
        $"{WeekdayName(outcome.Weekday)} ({outcome.SkippedReason})";

    private static string AllWeekdaysRouteText(bool reoptimizeRequested, List<ContainerBreakWeekdayOutcome> added)
    {
        if (!reoptimizeRequested)
        {
            return " Visit times were not changed; a visit may overlap the break until the route is re-optimized.";
        }

        var failed = added.Where(o => !o.RouteReoptimized).ToList();
        var reoptimized = added.Count - failed.Count;
        var text = $" The route was re-optimized for {reoptimized} of {added.Count} weekday(s).";
        return failed.Count == 0
            ? text
            : text + " Re-optimizing failed for " +
                string.Join("; ", failed.Select(o => $"{WeekdayName(o.Weekday)} ({o.RouteError})")) +
                "; there the break stays saved and a visit may overlap it until the route is re-optimized.";
    }

    private static string WeekdayName(int isoWeekday) =>
        CultureInfo.InvariantCulture.DateTimeFormat.GetDayName((DayOfWeek)IsoWeekday.ToStorage(isoWeekday));

    private static string DescribeBreak(BreakRequest request, string absenceName, string? startText, string? endText) =>
        request.StartTime.HasValue
            ? $"fixed break '{absenceName}' {startText}-{endText}"
            : $"movable break '{absenceName}' of {request.DurationMinutes} min (no clock time of its own; route optimization reserves its duration between the visits)";

    private static SkillResult BuildResult(
        BreakRequest request, Shift container, Absence absence, AddedBreak added, ContainerRouteWriteResult? route, string? language)
    {
        var absenceName = absence.Name.GetValueOrFirstAvailable(language);
        var isFixed = request.StartTime.HasValue;
        var startText = isFixed ? ClockText(added.Item.StartItem!.Value) : null;
        var endText = isFixed ? ClockText(added.Item.EndItem!.Value) : null;
        var breakText = DescribeBreak(request, absenceName, startText, endText);

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
