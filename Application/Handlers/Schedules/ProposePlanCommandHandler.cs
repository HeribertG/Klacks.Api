// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for <see cref="ProposePlanCommand"/>. Mirrors WizardApplyService.ApplyAsScenarioAsync:
/// create an AnalyseScenario, clone the real schedule under its token, flush, THEN partition the
/// placements via the shared compliance partition service (so the guardrail sees the cloned world,
/// not an empty one), then write the accepted placements via the BulkAddWorks pipeline. Blocked
/// placements are reported as rejected with the violating rule's key. A placement on a day that is sealed for the
/// client (globally, or by a group the client belongs to or worked for that day) is rejected up front: the scenario
/// write itself would pass (scenario writes bypass the day lock), but accepting it could never reach the real plan.
/// A placement on a day outside the employee's membership or group period (the grid's locked cells) is rejected up front
/// as well, one by one, so the rest of the proposal is still written and the bulk write itself skips that check; the
/// group period is that of the proposal's target group. A placement on a shift hidden from the caller is rejected exactly
/// like one on a shift that does not exist, and a hidden shift is never cloned into the scenario.
/// </summary>
/// <param name="shiftRepository">Resolves shift start/end times for each placement</param>
/// <param name="scenarioRepository">Persists the new AnalyseScenario</param>
/// <param name="scenarioService">Clones real schedule data under the scenario token</param>
/// <param name="partitionService">Shared accept/block partition incl. the K1 supervisor override</param>
/// <param name="mediator">Dispatches BulkAddWorksCommand for the written placements</param>
/// <param name="unitOfWork">Flushes the scenario + clone before the guardrail check</param>
/// <param name="scenarioNameGenerator">Builds the localized, per-group unique scenario name</param>
/// <param name="sealedDayRepository">Tells which (client, date) pairs are sealed</param>
/// <param name="groupVisibilityGuard">Refuses a hidden target group like a missing one and a group-less proposal for restricted callers</param>
/// <param name="planningPeriodGuard">Names the placements whose employee is not plannable on that day (membership or group period)</param>
/// <param name="shiftVisibilityGuard">Leaves out the shifts the caller may not book (one query for all placements)</param>
using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Schedules;

public sealed class ProposePlanCommandHandler : IRequestHandler<ProposePlanCommand, ProposePlanOutcome>
{
    private const string ShiftNotFoundReason = "shift not found";
    private const string SealedDayReason = "day sealed";
    private const string OutsidePlanningPeriodReason = "outside membership or group period";
    private const int HoursPerDay = 24;

    private readonly IShiftRepository _shiftRepository;
    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly ICompliancePartitionService _partitionService;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScenarioNameGenerator _scenarioNameGenerator;
    private readonly ISealedDayRepository _sealedDayRepository;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IClientPlanningPeriodGuard _planningPeriodGuard;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public ProposePlanCommandHandler(
        IShiftRepository shiftRepository,
        IAnalyseScenarioRepository scenarioRepository,
        IAnalyseScenarioService scenarioService,
        ICompliancePartitionService partitionService,
        IMediator mediator,
        IUnitOfWork unitOfWork,
        IScenarioNameGenerator scenarioNameGenerator,
        ISealedDayRepository sealedDayRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        IClientPlanningPeriodGuard planningPeriodGuard,
        IShiftVisibilityGuard shiftVisibilityGuard)
    {
        _groupVisibilityGuard = groupVisibilityGuard;
        _planningPeriodGuard = planningPeriodGuard;
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _shiftRepository = shiftRepository;
        _scenarioRepository = scenarioRepository;
        _scenarioService = scenarioService;
        _partitionService = partitionService;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _scenarioNameGenerator = scenarioNameGenerator;
        _sealedDayRepository = sealedDayRepository;
    }

    public async Task<ProposePlanOutcome> Handle(ProposePlanCommand request, CancellationToken cancellationToken)
    {
        var groupId = request.GroupId;
        var fromDate = request.FromDate;
        var untilDate = request.UntilDate;
        var placements = request.Placements;

        await _groupVisibilityGuard.EnsureScenarioScopeWritableAsync(groupId, cancellationToken);

        var rejected = new List<RejectedPlacement>();

        var shiftIds = placements.Select(p => p.ShiftId).Distinct().ToList();
        var visibleShiftIds = await _shiftVisibilityGuard.FilterVisibleAsync(shiftIds, cancellationToken);
        var shifts = new Dictionary<Guid, Shift>();
        foreach (var shiftId in shiftIds.Where(visibleShiftIds.Contains))
        {
            var shift = await _shiftRepository.Get(shiftId);
            if (shift != null)
            {
                shifts[shiftId] = shift;
            }
        }

        var sealedPairs = await _sealedDayRepository.GetLockedPairsAsync(
            placements.Select(p => (p.Date, p.ClientId)).Distinct().ToList(), cancellationToken);
        var outsidePeriodCells = (await FindPeriodViolationsAsync(placements, groupId, cancellationToken))
            .Select(violation => (violation.ClientId, violation.Date))
            .ToHashSet();

        var resolved = new List<PlacementInput>();
        foreach (var placement in placements)
        {
            if (sealedPairs.Contains((placement.Date, placement.ClientId)))
            {
                rejected.Add(new RejectedPlacement(placement.ClientId, placement.ShiftId, placement.Date, SealedDayReason));
            }
            else if (outsidePeriodCells.Contains((placement.ClientId, placement.Date)))
            {
                rejected.Add(new RejectedPlacement(
                    placement.ClientId, placement.ShiftId, placement.Date, OutsidePlanningPeriodReason));
            }
            else if (shifts.ContainsKey(placement.ShiftId))
            {
                resolved.Add(placement);
            }
            else
            {
                rejected.Add(new RejectedPlacement(placement.ClientId, placement.ShiftId, placement.Date, ShiftNotFoundReason));
            }
        }

        var token = Guid.NewGuid();
        var name = await _scenarioNameGenerator.GenerateAsync(
            ScenarioNameKind.Proposal, fromDate, untilDate, groupId, request.Language, cancellationToken);
        var scenario = new AnalyseScenario
        {
            Name = name,
            GroupId = groupId,
            FromDate = fromDate,
            UntilDate = untilDate,
            Token = token,
            RunGroupId = Guid.NewGuid()
        };
        await _scenarioRepository.Add(scenario);

        // Clone the real schedule under the token FIRST, then flush, so the guardrail sees the cloned
        // world. If the check ran before this, it would query by token, find nothing, and falsely pass.
        var shiftIdMap = await _scenarioService.CloneScenarioDataAsync(
            groupId, fromDate, untilDate, token, shifts.Keys.ToList(), sourceToken: null, cancellationToken);
        await _unitOfWork.CompleteAsync();

        var plannedRows = resolved
            .Select(p => (Placement: p, Row: new PlannedWorkRow(
                p.ClientId, p.Date, shifts[p.ShiftId].StartShift, shifts[p.ShiftId].EndShift, p.ShiftId)))
            .ToList();

        var partition = await _partitionService.PartitionAsync(
            plannedRows.Select(p => p.Row).ToList(), token, request.OverrideBlock, cancellationToken);

        var accepted = partition.AcceptedIndexes.Select(i => plannedRows[i]).ToList();
        rejected.AddRange(partition.BlockedIndexes.Select(b =>
        {
            var placement = plannedRows[b.Index].Placement;
            return new RejectedPlacement(placement.ClientId, placement.ShiftId, placement.Date, b.ReasonKey);
        }));

        if (accepted.Count > 0)
        {
            await WriteAsync(accepted, shifts, shiftIdMap, token, fromDate, untilDate, cancellationToken);
        }

        return new ProposePlanOutcome(
            scenario.Id,
            token,
            name,
            accepted.Select(a => a.Placement).ToList(),
            rejected,
            partition.ReportableConflicts);
    }

    /// <summary>
    /// The placements outside their employee's planning period. With a target group (whose visibility the handler has
    /// already ensured) the group period of that group applies, not the group the caller happens to have selected;
    /// without one, the request's group context decides, like for every other write.
    /// </summary>
    private Task<IReadOnlyList<PlanningPeriodViolation>> FindPeriodViolationsAsync(
        IReadOnlyList<PlacementInput> placements,
        Guid? groupId,
        CancellationToken cancellationToken)
    {
        var cells = placements.Select(p => (p.ClientId, p.Date)).Distinct().ToList();
        return groupId is Guid targetGroupId
            ? _planningPeriodGuard.FindViolationsInGroupsAsync(cells, [targetGroupId], cancellationToken)
            : _planningPeriodGuard.FindViolationsAsync(cells, cancellationToken);
    }

    private async Task WriteAsync(
        List<(PlacementInput Placement, PlannedWorkRow Row)> accepted,
        IReadOnlyDictionary<Guid, Shift> shifts,
        IReadOnlyDictionary<Guid, Guid> shiftIdMap,
        Guid token,
        DateOnly fromDate,
        DateOnly untilDate,
        CancellationToken cancellationToken)
    {
        var bulkItems = accepted.Select(a =>
        {
            var shift = shifts[a.Placement.ShiftId];
            var cloneShiftId = shiftIdMap.TryGetValue(a.Placement.ShiftId, out var mapped)
                ? mapped
                : a.Placement.ShiftId;
            return new BulkWorkItem
            {
                ClientId = a.Placement.ClientId,
                ShiftId = cloneShiftId,
                CurrentDate = a.Placement.Date,
                StartTime = shift.StartShift,
                EndTime = shift.EndShift,
                WorkTime = WorkHours(shift.StartShift, shift.EndShift),
                AnalyseToken = token
            };
        }).ToList();

        await _mediator.Send(new BulkAddWorksCommand(new BulkAddWorksRequest
        {
            Works = bulkItems,
            PeriodStart = fromDate,
            PeriodEnd = untilDate
        }, PlanningPeriodCheck.SkipForPlannerOutput), cancellationToken);
    }

    private static decimal WorkHours(TimeOnly start, TimeOnly end)
    {
        var duration = end.ToTimeSpan() - start.ToTimeSpan();
        if (duration.TotalHours <= 0)
        {
            duration = duration.Add(TimeSpan.FromHours(HoursPerDay));
        }
        return (decimal)duration.TotalHours;
    }
}
