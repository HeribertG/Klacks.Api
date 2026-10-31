// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Turns a recorded acceptance into the scenario's plan. The slot's replacement WorkChange is found on the server (absent
/// employee, workday, source shift, slot start) and rewritten through PutCommand, so locks, surcharges, the period hours of
/// both people and the notifications run exactly as for a manual edit and the recovery marker stays. The candidate must
/// still be eligible in the scenario at this moment for the stored window of the slot and may not already stand in at an overlapping time (the candidate
/// search does not count replacements as occupancy). What belonged to the old stand-in goes with them: the relocation of
/// their own work of a swap unless they still stand in at that time, their untouched proposal row and, after their last cover, their temporary membership. Recorded
/// answers stay, they are facts. A later Declined or NotReached does not switch back; the latest acceptance wins. Works only
/// on an active scenario and a slot that still has a replacement; anything else keeps just the answer. Only the planner's
/// recorded answer triggers this, nobody is contacted.
/// </summary>
/// <param name="workChangeRepository">Finds the slot's replacement and what else the old stand-in holds</param>
/// <param name="requestRepository">Stage-only request book</param>
/// <param name="scenarioRepository">Scenario of the row</param>
/// <param name="scenarioService">Maps scenario clone shifts to their root shift; removes temporary memberships</param>
/// <param name="mediator">Runs the candidate search and the WorkChange commands</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Services.Schedules.Recovery;

public sealed class ScenarioReplacementSwitcher : IScenarioReplacementSwitcher
{
    private const string AmbiguousSlotMessage =
        "This slot has several replacements in the scenario; change the right one in the schedule.";
    private const string NotEligibleMessage = "This person can no longer take over this slot in the scenario.";
    private const string AlreadyStandingInMessage = "This person already stands in at this time in the scenario.";
    private const string NoGroupMessage = "The slot has no group to check the person against.";
    private const string ChangeNotFoundMessage = "WorkChange with ID {0} not found";

    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly IReplacementRequestRepository _requestRepository;
    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IMediator _mediator;

    public ScenarioReplacementSwitcher(
        IWorkChangeRepository workChangeRepository,
        IReplacementRequestRepository requestRepository,
        IAnalyseScenarioRepository scenarioRepository,
        IAnalyseScenarioService scenarioService,
        IMediator mediator)
    {
        _workChangeRepository = workChangeRepository;
        _requestRepository = requestRepository;
        _scenarioRepository = scenarioRepository;
        _scenarioService = scenarioService;
        _mediator = mediator;
    }

    public async Task<bool> ApplyAcceptedAsync(ReplacementRequest row, bool overrideBlock, CancellationToken cancellationToken)
    {
        if (row.Outcome != ReplacementRequestOutcome.Accepted || row.AnalyseToken is not { } token)
        {
            return false;
        }

        var scenario = await _scenarioRepository.GetByTokenAsync(token, cancellationToken);
        if (scenario?.Status != AnalyseScenarioStatus.Active)
        {
            return false;
        }

        var slotChange = await FindSlotChangeAsync(token, row, cancellationToken);
        if (slotChange is null)
        {
            return false;
        }

        if (slotChange.ReplaceClientId == row.CandidateClientId)
        {
            return true;
        }

        var (slotStart, slotEnd) = StoredWindowOf(slotChange);
        await EnsureEligibleAsync(row, slotStart, slotEnd, scenario.GroupId ?? row.GroupId, token, overrideBlock, cancellationToken);
        if (await _workChangeRepository.HasOverlappingScenarioReplacementAsync(
                token, row.CandidateClientId, row.Date, slotChange.Work!.StartTime, slotStart, slotEnd, cancellationToken))
        {
            throw new ConflictException(AlreadyStandingInMessage);
        }

        await ReplaceStandInAsync(slotChange.Id, token, row.CandidateClientId, cancellationToken);
        await RemoveOldStandInRowsAsync(token, slotChange.ReplaceClientId!.Value, row, cancellationToken);
        return true;
    }

    /// <summary>
    /// The replacement of the row's slot, or null when none is left. Several replacements on one slot are told apart by
    /// the slot start; a slot that stays ambiguous is refused instead of guessed.
    /// </summary>
    private async Task<WorkChange?> FindSlotChangeAsync(Guid token, ReplacementRequest row, CancellationToken cancellationToken)
    {
        var changes = await _workChangeRepository.ListScenarioReplacementsAsync(
            token, row.AbsentClientId, row.Date, cancellationToken);
        if (changes.Count == 0)
        {
            return null;
        }

        var rootShiftIds = await _scenarioService.ResolveRootShiftIdsAsync(
            changes.Select(change => change.Work!.ShiftId).Distinct().ToList(), cancellationToken);
        var onSlot = changes
            .Where(change => rootShiftIds.GetValueOrDefault(change.Work!.ShiftId, change.Work.ShiftId) == row.ShiftId)
            .ToList();
        if (onSlot.Count <= 1)
        {
            return onSlot.SingleOrDefault();
        }

        var atSlotStart = onSlot.Where(change => change.StartTime == row.StartTime).ToList();
        return atSlotStart.Count == 1 ? atSlotStart[0] : throw new ConflictException(AmbiguousSlotMessage);
    }

    /// <summary>
    /// The time the slot's replacement occupies. Only ReplacementWithin carries its own span; the partial replacement
    /// types are judged over their whole work.
    /// </summary>
    private static (TimeOnly Start, TimeOnly End) StoredWindowOf(WorkChange change)
        => change.Type == WorkChangeType.ReplacementWithin
            ? (change.StartTime, change.EndTime)
            : (change.Work!.StartTime, change.Work.EndTime);

    private async Task EnsureEligibleAsync(
        ReplacementRequest row,
        TimeOnly slotStart,
        TimeOnly slotEnd,
        Guid? groupId,
        Guid token,
        bool overrideBlock,
        CancellationToken cancellationToken)
    {
        if (groupId is not { } searchGroupId)
        {
            throw new ConflictException(NoGroupMessage);
        }

        var search = await _mediator.Send(
            new FindReplacementQuery(row.ShiftId, row.Date, slotStart, slotEnd, searchGroupId, token, overrideBlock),
            cancellationToken);
        if (search.Eligible.All(candidate => candidate.ClientId != row.CandidateClientId))
        {
            throw new ConflictException(NotEligibleMessage);
        }
    }

    private async Task ReplaceStandInAsync(Guid workChangeId, Guid token, Guid candidateId, CancellationToken cancellationToken)
    {
        var resource = await _mediator.Send(new GetWorkChangeInScopeQuery(workChangeId, token), cancellationToken);
        resource.ReplaceClientId = candidateId;

        _ = await _mediator.Send(new PutCommand<WorkChangeResource>(resource), cancellationToken)
            ?? throw new KeyNotFoundException(string.Format(ChangeNotFoundMessage, workChangeId));
    }

    private async Task RemoveOldStandInRowsAsync(
        Guid token, Guid oldStandInId, ReplacementRequest row, CancellationToken cancellationToken)
    {
        var ownChanges = await _workChangeRepository.ListScenarioReplacementsAsync(token, oldStandInId, row.Date, cancellationToken);
        foreach (var relocation in ownChanges.Where(IsRecoveryChange))
        {
            if (await _workChangeRepository.HasOverlappingScenarioReplacementAsync(
                    token, oldStandInId, row.Date, relocation.Work!.StartTime, relocation.StartTime, relocation.EndTime, cancellationToken))
            {
                continue;
            }

            _ = await _mediator.Send(new DeleteCommand<WorkChangeResource>(relocation.Id), cancellationToken)
                ?? throw new KeyNotFoundException(string.Format(ChangeNotFoundMessage, relocation.Id));
        }

        var proposal = await _requestRepository.FindLiveAsync(token, oldStandInId, row.ShiftId, row.Date, cancellationToken);
        if (proposal?.Outcome == ReplacementRequestOutcome.Proposed)
        {
            _requestRepository.Remove(proposal);
        }

        if (!await _workChangeRepository.IsReplacementInScenarioAsync(token, oldStandInId, cancellationToken))
        {
            await _scenarioService.RemoveScenarioMembershipsAsync(token, oldStandInId, row.Date, cancellationToken);
        }
    }

    private static bool IsRecoveryChange(WorkChange change)
        => string.Equals(change.Description, RecoveryMarkers.WorkChangeSource, StringComparison.Ordinal);
}
