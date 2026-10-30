// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Writes ONE planned tour: creates its sealed container order (active only on the weekdays that have services, linked to the
/// groups), holds the container's template lock, and for every weekday runs the container autofill restricted to the
/// services of that tour on that day (ContainerAutofillRequest.RestrictToShiftIds), so no service of another tour can be
/// picked. The visible picks are placed on the route (ContainerVisitPlanner) and all weekday templates are written and
/// confirmed by re-reading (ContainerTemplateWriter). A service that did not fit the day's time budget is returned in the
/// weekday outcome, never dropped silently. It must run inside the caller's database transaction: a tour that places no
/// service at all, a lock that cannot be taken or a template that cannot be confirmed throws a SkillVerificationException,
/// which rolls the whole run back.
/// </summary>
/// <param name="shiftRepository">Persists the container order</param>
/// <param name="unitOfWork">Saves the container before it is autofilled</param>
/// <param name="mediator">Takes the template lock</param>
/// <param name="containerAutofillService">Selects and routes the services of a weekday</param>
/// <param name="visibilityFilter">Keeps the picks the caller may see</param>
/// <param name="templateWriter">Loads the picked services and writes and confirms the templates</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.RouteOptimization;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class WeeklyTourBuilder
{
    private const string NothingPlacedMessage =
        "No service of '{0}' could be placed on a route (the base address could not be located or no service fits the time " +
        "budget) — the whole run was rolled back and no tour was created.";
    private const string LockMessage =
        "The container '{0}' could not be locked for writing its templates — the whole run was rolled back.";

    private readonly IShiftRepository _shiftRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly IContainerAutofillService _containerAutofillService;
    private readonly ContainerAutofillVisibilityFilter _visibilityFilter;
    private readonly ContainerTemplateWriter _templateWriter;

    public WeeklyTourBuilder(
        IShiftRepository shiftRepository,
        IUnitOfWork unitOfWork,
        IMediator mediator,
        IContainerAutofillService containerAutofillService,
        ContainerAutofillVisibilityFilter visibilityFilter,
        ContainerTemplateWriter templateWriter)
    {
        _shiftRepository = shiftRepository;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _containerAutofillService = containerAutofillService;
        _visibilityFilter = visibilityFilter;
        _templateWriter = templateWriter;
    }

    /// <param name="settings">What every tour of the run shares</param>
    /// <param name="tour">The tour to write</param>
    /// <param name="cancellationToken">Cancels the writes</param>
    public async Task<WeeklyTourOutcome> BuildAsync(
        WeeklyTourBuildSettings settings, PlannedWeeklyTour tour, CancellationToken cancellationToken)
    {
        var name = $"{settings.NamePrefix} {tour.Number}";
        var container = await CreateContainerAsync(settings, tour, name);

        await using var lockScope = await ContainerTemplateLockScope.AcquireAsync(
            _mediator, container.Id, settings.InstanceId, cancellationToken);
        if (!lockScope.Acquired)
        {
            throw new SkillVerificationException(settings.SkillName, string.Format(LockMessage, name));
        }

        var templates = new List<ContainerTemplateResource>();
        var days = new List<WeeklyTourDayOutcome>();
        var placedShiftIds = new List<Guid>();
        foreach (var (isoDay, dayShifts) in tour.ShiftsByWeekday.OrderBy(entry => entry.Key))
        {
            var (day, template) = await PlanDayAsync(settings, container.Id, isoDay, dayShifts, cancellationToken);
            days.Add(day);
            if (template is not null)
            {
                templates.Add(template.Template);
                placedShiftIds.AddRange(template.PlacedShiftIds);
            }
        }

        if (templates.Count == 0)
        {
            throw new SkillVerificationException(settings.SkillName, string.Format(NothingPlacedMessage, name));
        }

        await _templateWriter.WriteAndConfirmAsync(settings.SkillName, container.Id, templates, cancellationToken);

        return new WeeklyTourOutcome(tour.Number, container.Id, container.Name, tour.ClientIds.Count, days, placedShiftIds);
    }

    private sealed record DayTemplate(ContainerTemplateResource Template, List<Guid> PlacedShiftIds);

    private async Task<Shift> CreateContainerAsync(WeeklyTourBuildSettings settings, PlannedWeeklyTour tour, string name)
    {
        var plan = settings.Plan;
        var spec = new ContainerShiftSpec(
            name,
            SkillWriteSupport.GenerateAbbreviation(name),
            plan.FromTime,
            plan.UntilTime,
            plan.FromDate,
            plan.UntilDate,
            SkillWriteSupport.CalculateWorkTime(plan.FromTime, plan.UntilTime),
            settings.DefaultMacroId,
            tour.ShiftsByWeekday.Keys.ToHashSet(),
            settings.Groups.Select(group => new GroupItem
            {
                GroupId = group.Id,
                ValidFrom = plan.FromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            }).ToList(),
            settings.UserName);

        var container = await _shiftRepository.AddWithSealedOrderHandling(ContainerShiftFactory.Build(spec));
        await _unitOfWork.CompleteAsync();
        return container;
    }

    private async Task<(WeeklyTourDayOutcome Day, DayTemplate? Template)> PlanDayAsync(
        WeeklyTourBuildSettings settings,
        Guid containerId,
        int isoDay,
        List<Shift> dayShifts,
        CancellationToken cancellationToken)
    {
        var plan = settings.Plan;
        var raw = await _containerAutofillService.AutofillAsync(new ContainerAutofillRequest(
            ContainerId: containerId,
            Weekday: IsoWeekday.ToStorage(isoDay),
            IsHoliday: false,
            StartBase: settings.Location,
            EndBase: settings.Location,
            FromTime: plan.FromTime,
            UntilTime: plan.UntilTime,
            TransportMode: settings.TransportMode,
            TimeRangeTolerance: settings.TimeRangeTolerance,
            CancellationToken: cancellationToken,
            RestrictToShiftIds: dayShifts.Select(shift => shift.Id).ToList()));

        var autofill = await _visibilityFilter.KeepVisibleSelectionAsync(raw, cancellationToken);
        var placedIds = autofill.SelectedShiftIds.Distinct().ToHashSet();
        var notPlaced = dayShifts
            .Where(shift => !placedIds.Contains(shift.Id))
            .Select(shift => WeeklyTourPlanner.DisplayName(shift.Client, shift.Name))
            .ToList();

        if (placedIds.Count == 0)
        {
            return (new WeeklyTourDayOutcome(isoDay, dayShifts.Count, 0, 0, autofill.IsEstimated, [], notPlaced), null);
        }

        var taskShifts = await _templateWriter.LoadTaskShiftsAsync(settings.SkillName, autofill.SelectedShiftIds);
        var planned = ContainerVisitPlanner.Plan(settings.SkillName, settings.Location, plan.FromTime, autofill, taskShifts);
        var template = _templateWriter.BuildTemplate(
            containerId, IsoWeekday.ToStorage(isoDay), plan.FromTime, plan.UntilTime, settings.Location, settings.TransportMode, planned);

        var day = new WeeklyTourDayOutcome(
            isoDay, dayShifts.Count, planned.Items.Count, autofill.TotalDistanceKm, autofill.IsEstimated, planned.Stops, notPlaced);
        return (day, new DayTemplate(template, taskShifts.Select(shift => shift.Id).ToList()));
    }
}
