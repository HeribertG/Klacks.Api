// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Books an employee on a container (a tour) for every date of a range on which the container can be worked: the date
/// lies inside the container's validity, the container runs on its weekday and a template applies to it. The dates are
/// decided by ContainerWeekBookingPlanner (existing booking, sealed day, planning period, blocking schedule conflict);
/// every date that is left out is returned with its reason. All remaining dates go through ONE BulkAddWorksCommand, the
/// same pipeline as place_work, so ContainerWorkExpansionService expands the sub-works and sub-breaks of each day and the
/// period hours are recalculated. A SealedOrder id is resolved to the plannable container copy the templates hang on.
/// Atomicity: the pipeline commits and notifies on its own (as place_work does), so this skill is not wrapped in a
/// transaction and cannot roll the command back. What it guarantees instead: nothing is sent for a date that fails a
/// check before the write, the command itself is all or nothing (a refusal is answered as an error and books nothing),
/// and after it the works are re-read without tracking: every booked date must have a top-level work on the container
/// and, where the template holds task visits, sub-works. A date that cannot be confirmed is returned as an error naming
/// it and the dates that were confirmed, never as a success.
/// </summary>
/// <param name="containerId">Required. UUID of the container shift (ShiftType.IsContainer), or of its sealed order.</param>
/// <param name="clientId">Required. UUID of the employee to book.</param>
/// <param name="fromDate">Required. First date of the range (ISO yyyy-MM-dd).</param>
/// <param name="untilDate">Required. Last date of the range (ISO yyyy-MM-dd); at most MaxRangeDays days in total.</param>
/// <param name="shiftRepository">Reads the container shift</param>
/// <param name="shiftReader">Finds the plannable copy of a sealed container order</param>
/// <param name="shiftVisibilityGuard">Answers a hidden container exactly like a missing one</param>
/// <param name="childrenRepository">Reads the sub-works of a booked container work; they belong to the customer of each visit, not to the employee</param>
/// <param name="clientVisibilityGuard">Answers a hidden employee exactly like an unknown one</param>

using System.Globalization;
using FluentValidation;
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class AssignContainerWeekSkill : BaseSkillImplementation
{
    public const string SkillName = "assign_container_week";
    public const int MaxRangeDays = 92;

    private const string ContainerIdParameter = "containerId";
    private const string ClientIdParameter = "clientId";
    private const string FromDateParameter = "fromDate";
    private const string UntilDateParameter = "untilDate";
    private const string DateSeparator = ", ";
    private const string ReasonSeparator = "; ";
    private const string DateRequiredMessage = "{0} is required (ISO date yyyy-MM-dd).";
    private const string RangeOrderMessage = "untilDate ({0}) must not be before fromDate ({1}).";
    private const string RangeTooLongMessage = "The range may span at most {0} days; ask the user for a shorter range.";
    private const string NoPlannableCopyMessage =
        "The container order {0} has no plannable container shift to book on.";
    private const string NoTemplatesMessage =
        "The container '{0}' has no weekday template yet, so there is nothing to book — configure its template first.";
    private const string NothingToBookMessage = "Nothing was booked: {0}";
    private const string RefusedMessage = "Booking of client {0} on container '{1}' was refused: {2}";
    private const string UnconfirmedMessage =
        "The booking command ran, but the schedule does not confirm it for: {0}. Confirmed: {1}. The pipeline commits on " +
        "its own, so nothing was rolled back — check the schedule of the employee before booking again.";
    private const string NoneConfirmed = "none";

    private readonly IMediator _mediator;
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftReader _shiftReader;
    private readonly IContainerTemplateRepository _containerTemplateRepository;
    private readonly IWorkRepository _workRepository;
    private readonly IContainerWorkChildrenReadRepository _childrenRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly ContainerWeekBookingPlanner _planner;

    public AssignContainerWeekSkill(
        IMediator mediator,
        IShiftRepository shiftRepository,
        IShiftReader shiftReader,
        IContainerTemplateRepository containerTemplateRepository,
        IWorkRepository workRepository,
        IContainerWorkChildrenReadRepository childrenRepository,
        IClientRepository clientRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ISealedDayRepository sealedDayRepository,
        IClientPlanningPeriodGuard planningPeriodGuard,
        IPreCommitConflictChecker conflictChecker,
        IShiftVisibilityGuard shiftVisibilityGuard)
    {
        _mediator = mediator;
        _shiftRepository = shiftRepository;
        _shiftReader = shiftReader;
        _containerTemplateRepository = containerTemplateRepository;
        _workRepository = workRepository;
        _childrenRepository = childrenRepository;
        _clientRepository = clientRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _planner = new ContainerWeekBookingPlanner(workRepository, sealedDayRepository, planningPeriodGuard, conflictChecker);
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var containerId = GetRequiredGuid(parameters, ContainerIdParameter);
        var clientId = GetRequiredGuid(parameters, ClientIdParameter);

        var (range, rangeError) = ReadRange(parameters);
        if (range is null)
        {
            return SkillResult.Error(rangeError!);
        }

        if (!await ClientResolver.ExistsVisibleAsync(_clientRepository, _clientVisibilityGuard, clientId, cancellationToken))
        {
            return SkillResult.Error($"Client {clientId} not found.");
        }

        var (container, containerError) = await ResolveContainerAsync(containerId, cancellationToken);
        if (container is null)
        {
            return SkillResult.Error(containerError!);
        }

        var templates = await _containerTemplateRepository.GetTemplatesForContainer(container.Id);
        if (templates.Count == 0)
        {
            return SkillResult.Error(string.Format(NoTemplatesMessage, container.Name));
        }

        var (from, until) = range.Value;
        var plan = await _planner.PlanAsync(container, templates, clientId, from, until, cancellationToken);
        if (plan.Bookings.Count == 0)
        {
            return SkillResult.Error(string.Format(NothingToBookMessage, DescribeSkipped(plan.Skipped)));
        }

        var (response, refusal) = await BookAsync(container, clientId, plan, cancellationToken);
        if (refusal is not null)
        {
            return SkillResult.Error(refusal);
        }

        var unconfirmed = await FindUnconfirmedDatesAsync(container, clientId, plan, cancellationToken);
        if (unconfirmed.Count > 0)
        {
            var confirmed = plan.Bookings.Select(b => b.Date).Except(unconfirmed).ToList();
            return SkillResult.Error(string.Format(
                UnconfirmedMessage, FormatDates(unconfirmed), confirmed.Count == 0 ? NoneConfirmed : FormatDates(confirmed)));
        }

        return BuildSuccessResult(container, clientId, plan, response!);
    }

    private static ((DateOnly From, DateOnly Until)? Range, string? Error) ReadRange(Dictionary<string, object> parameters)
    {
        var from = GetParameter<DateOnly?>(parameters, FromDateParameter);
        var until = GetParameter<DateOnly?>(parameters, UntilDateParameter);
        if (from is null)
        {
            return (null, string.Format(DateRequiredMessage, FromDateParameter));
        }

        if (until is null)
        {
            return (null, string.Format(DateRequiredMessage, UntilDateParameter));
        }

        if (until.Value < from.Value)
        {
            return (null, string.Format(RangeOrderMessage, FormatDate(until.Value), FormatDate(from.Value)));
        }

        if (until.Value.DayNumber - from.Value.DayNumber >= MaxRangeDays)
        {
            return (null, string.Format(RangeTooLongMessage, MaxRangeDays));
        }

        return ((from.Value, until.Value), null);
    }

    private async Task<(Shift? Container, string? Error)> ResolveContainerAsync(Guid id, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.Get(id);
        if (shift is null || !await _shiftVisibilityGuard.IsVisibleAsync(id, cancellationToken))
        {
            return (null, $"Shift/container not found: {id}");
        }

        if (shift.ShiftType != ShiftType.IsContainer)
        {
            return (null, $"Shift {id} is not a container shift (ShiftType must be IsContainer).");
        }

        if (shift.Status != ShiftStatus.SealedOrder)
        {
            return (shift, null);
        }

        var plannable = await _shiftReader.FindFirstByOriginalIdAsync(shift.Id, ShiftStatus.OriginalShift, cancellationToken);
        if (plannable is null || !await _shiftVisibilityGuard.IsVisibleAsync(plannable.Id, cancellationToken))
        {
            return (null, string.Format(NoPlannableCopyMessage, id));
        }

        return (await _shiftRepository.Get(plannable.Id) ?? plannable, null);
    }

    private async Task<(BulkWorksResponse? Response, string? Refusal)> BookAsync(
        Shift container, Guid clientId, ContainerWeekPlan plan, CancellationToken cancellationToken)
    {
        var workTime = CalculateWorkTime(container.StartShift, container.EndShift);
        var request = new BulkAddWorksRequest
        {
            PeriodStart = plan.Bookings[0].Date,
            PeriodEnd = plan.Bookings[^1].Date,
            Works = plan.Bookings.Select(booking => new BulkWorkItem
            {
                ClientId = clientId,
                ShiftId = container.Id,
                CurrentDate = booking.Date,
                StartTime = container.StartShift,
                EndTime = container.EndShift,
                WorkTime = workTime,
                AnalyseToken = null
            }).ToList()
        };

        try
        {
            return (await _mediator.Send(new BulkAddWorksCommand(request), cancellationToken), null);
        }
        catch (ValidationException ex)
        {
            return (null, string.Format(
                RefusedMessage, clientId, container.Name, string.Join(" ", ex.Errors.Select(error => error.ErrorMessage))));
        }
        catch (Exception ex) when (ex is InvalidRequestException or ConflictException or KeyNotFoundException)
        {
            return (null, string.Format(RefusedMessage, clientId, container.Name, ex.Message));
        }
    }

    private async Task<List<DateOnly>> FindUnconfirmedDatesAsync(
        Shift container, Guid clientId, ContainerWeekPlan plan, CancellationToken cancellationToken)
    {
        var works = await _workRepository.GetByClientAndDateRangeAsync(
            clientId,
            plan.Bookings[0].Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            plan.Bookings[^1].Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            cancellationToken);

        var parents = MainPlanWorks.ParentsOnly(works)
            .Where(work => work.ShiftId == container.Id)
            .GroupBy(work => work.CurrentDate)
            .ToDictionary(group => group.Key, group => group.Select(work => work.Id).ToHashSet());

        var unconfirmed = new List<DateOnly>();
        foreach (var booking in plan.Bookings)
        {
            if (!await IsConfirmedAsync(booking, parents, cancellationToken))
            {
                unconfirmed.Add(booking.Date);
            }
        }

        return unconfirmed;
    }

    private async Task<bool> IsConfirmedAsync(
        ContainerWeekBooking booking, Dictionary<DateOnly, HashSet<Guid>> parents, CancellationToken cancellationToken)
    {
        if (!parents.TryGetValue(booking.Date, out var parentIds))
        {
            return false;
        }

        if (booking.ExpectedVisitItems == 0)
        {
            return true;
        }

        foreach (var parentId in parentIds)
        {
            var children = await _childrenRepository.GetChildWorkShiftIds(parentId, cancellationToken);
            if (children.Count > 0)
            {
                return true;
            }
        }

        return false;
    }

    private static SkillResult BuildSuccessResult(Shift container, Guid clientId, ContainerWeekPlan plan, BulkWorksResponse response)
    {
        var bookedDates = plan.Bookings.Select(b => b.Date).ToList();
        var skippedText = plan.Skipped.Count == 0 ? string.Empty : $" Not booked: {DescribeSkipped(plan.Skipped)}.";
        var warningText = plan.Warnings.Count == 0
            ? string.Empty
            : $" Note: {plan.Warnings.Count} schedule warning(s) (e.g. rest time / overtime) — committed anyway.";

        var message = $"Booked client {clientId} on the container '{container.Name}' for {bookedDates.Count} date(s): " +
            $"{FormatDates(bookedDates)}. The container works and their sub-works were re-read and confirmed in the database (verified)." +
            skippedText + warningText +
            " Tell the user the booked dates and, for every date that was not booked, the reason.";

        return SkillResult.SuccessResult(
            new
            {
                ContainerId = container.Id,
                ContainerName = container.Name,
                ClientId = clientId,
                BookedCount = bookedDates.Count,
                Booked = bookedDates.Select(FormatDate).ToList(),
                Skipped = plan.Skipped.Select(s => new { Date = FormatDate(s.Date), s.Reason }).ToList(),
                Warnings = plan.Warnings.Select(w => new { Date = FormatDate(w.Date), w.Severity, w.Comment }).ToList(),
                Verified = true,
                BulkResponse = response
            },
            message);
    }

    private static string DescribeSkipped(IReadOnlyList<ContainerWeekSkip> skipped) =>
        string.Join(
            ReasonSeparator,
            skipped.GroupBy(s => s.Reason)
                .Select(group => $"{FormatDates(group.Select(s => s.Date))} ({group.Key})"));

    private static string FormatDates(IEnumerable<DateOnly> dates) =>
        string.Join(DateSeparator, dates.Select(FormatDate));

    private static string FormatDate(DateOnly date) =>
        date.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture);
}
