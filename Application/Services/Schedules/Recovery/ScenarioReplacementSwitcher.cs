// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Turns a recorded acceptance into the scenario's plan. The slot's replacement WorkChange is found on the server (absent
/// employee, workday, source shift, slot start) and rewritten through PutCommand, so locks, surcharges, the period hours of
/// both people and the notifications run exactly as for a manual edit and the recovery marker stays. A slot whose work a
/// cover moved has no WorkChange: the moved work passes to the candidate with its origin, and the row becomes a Move row of
/// that work. A work something holds in place or a failed recalculation refuses the switch, the answer rolls back with
/// it. The candidate must
/// still be eligible in the scenario at this moment for the stored window of the slot and may not already stand in at an overlapping time (the candidate
/// search does not count replacements as occupancy). What belonged to the old stand-in goes with them: the relocation of
/// their own work of a swap, moved or replaced, when this scenario wrote that relocation and, for a moved work, nothing holds it and
/// they are free at that time, their untouched proposal row and, when they cover nothing else on the day of the slot, their
/// temporary membership of that day; their memberships of other days stay. A swap
/// chain is taken back as far as it reaches: the works the old stand-in handed on on other days within the relief reach of
/// the engine return as well, and whoever took one of them gets back what they handed on in turn, up to the chain length
/// of the engine; a borrowed receiver who then covers nothing on the day of the returned work loses the temporary
/// membership of that day.
/// A work that went away because its person is absent themselves (the request book knows that slot) stays. When a person
/// of the chain still covers something else in the scenario, or a work on another day cannot return, the switch is refused
/// as a whole. A candidate borrowed from another group under the same root gets the temporary membership of the day in
/// the scenario, like a borrowed person of the automatic cover. Recorded
/// answers stay, they are facts. A later Declined or NotReached does not switch back; the latest acceptance wins. Works only
/// on an active scenario and a slot that still has a replacement; anything else keeps just the answer. Only the planner's
/// recorded answer triggers this, nobody is contacted. A slot moved by an earlier accept is settled, its acceptance keeps just
/// the answer; a slot of an open source scenario still switches. The person who took the swapped work is refused with a reason of their own.
/// </summary>
/// <param name="workChangeRepository">Finds the slot's replacement and what else the old stand-in holds</param>
/// <param name="workRepository">Finds the slot's moved work</param>
/// <param name="coverMaterializer">Hands a moved work on or back</param>
/// <param name="requestRepository">Stage-only request book; tells a cover of an own absence from a handed-on work</param>
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
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using RulesetDefaults = Klacks.ScheduleRecovery.Model.RulesetDefaults;

namespace Klacks.Api.Application.Services.Schedules.Recovery;

public sealed class ScenarioReplacementSwitcher : IScenarioReplacementSwitcher
{
    private const string AmbiguousSlotMessage =
        "This slot has several replacements in the scenario; change the right one in the schedule.";
    private const string NotEligibleMessage = "This person can no longer take over this slot in the scenario.";
    private const string AlreadyStandingInMessage = "This person already stands in at this time in the scenario.";
    private const string NoGroupMessage = "The slot has no group to check the person against.";
    private const string ChangeNotFoundMessage = "WorkChange with ID {0} not found";
    private const string RecalculationFailedMessage = "The service could not be recalculated for this person.";
    private const string HeldWorkMessage = "This service can no longer change person; it is sealed or replaced.";
    private const string HopReceiverMessage =
        "This person already took over the service the current stand-in gave up for this swap; change the swap in the schedule.";
    private const string ChainNotUndoableMessage =
        "This cover is part of a swap chain that cannot be undone automatically; reject the scenario and cover the absence again.";
    private const int FirstReliefLevel = 1;
    private const int MaxReliefLevels = RulesetDefaults.MaxSwapChainDepth - 1;

    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly IWorkRepository _workRepository;
    private readonly IAbsenceCoverMaterializer _coverMaterializer;
    private readonly IReplacementRequestRepository _requestRepository;
    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IMediator _mediator;

    public ScenarioReplacementSwitcher(
        IWorkChangeRepository workChangeRepository,
        IWorkRepository workRepository,
        IAbsenceCoverMaterializer coverMaterializer,
        IReplacementRequestRepository requestRepository,
        IAnalyseScenarioRepository scenarioRepository,
        IAnalyseScenarioService scenarioService,
        IMediator mediator)
    {
        _workChangeRepository = workChangeRepository;
        _workRepository = workRepository;
        _coverMaterializer = coverMaterializer;
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
            return await PassOnMovedWorkAsync(token, scenario.GroupId ?? row.GroupId, row, overrideBlock, cancellationToken);
        }

        if (slotChange.ReplaceClientId == row.CandidateClientId)
        {
            return true;
        }

        var (slotStart, slotEnd) = StoredWindowOf(slotChange);
        var groupId = scenario.GroupId ?? row.GroupId;
        var isBorrowed = await EnsureCandidateFitsAsync(
            row, slotChange.Work!, slotChange.ReplaceClientId!.Value, slotStart, slotEnd, groupId, token,
            overrideBlock, cancellationToken);

        await ReplaceStandInAsync(slotChange.Id, token, row.CandidateClientId, cancellationToken);
        await JoinBorrowedCandidateAsync(isBorrowed, token, groupId, row, cancellationToken);
        await RemoveOldStandInRowsAsync(token, slotChange.ReplaceClientId!.Value, row, cancellationToken);
        return true;
    }

    private async Task<bool> PassOnMovedWorkAsync(
        Guid token, Guid? groupId, ReplacementRequest row, bool overrideBlock, CancellationToken cancellationToken)
    {
        var work = await FindMovedWorkAsync(token, row, cancellationToken);
        if (work is null)
        {
            return false;
        }

        if (work.ClientId == row.CandidateClientId)
        {
            return true;
        }

        var isBorrowed = await EnsureCandidateFitsAsync(
            row, work, work.ClientId, work.StartTime, work.EndTime, groupId, token, overrideBlock, cancellationToken);

        switch (await _coverMaterializer.PassOnMovedWorkAsync(token, work.Id, row.CandidateClientId, cancellationToken))
        {
            case MovedWorkHandOverResult.Held:
                throw new ConflictException(HeldWorkMessage);
            case MovedWorkHandOverResult.RecalculationFailed:
                throw new ConflictException(RecalculationFailedMessage);
        }

        row.Kind = ReplacementRequestKind.Move;
        row.WorkId = work.Id;
        await JoinBorrowedCandidateAsync(isBorrowed, token, groupId, row, cancellationToken);

        await RemoveOldStandInRowsAsync(token, work.ClientId, row, cancellationToken);
        return true;
    }

    private async Task<Work?> FindMovedWorkAsync(Guid token, ReplacementRequest row, CancellationToken cancellationToken)
    {
        var works = await _workRepository.ListMovedByScenarioAsync(token, row.AbsentClientId, row.Date, cancellationToken);
        if (works.Count == 0)
        {
            return null;
        }

        var rootShiftIds = await _scenarioService.ResolveRootShiftIdsAsync(
            works.Select(work => work.ShiftId).Distinct().ToList(), cancellationToken);
        var onSlot = works
            .Where(work => rootShiftIds.GetValueOrDefault(work.ShiftId, work.ShiftId) == row.ShiftId)
            .ToList();
        if (onSlot.Count <= 1)
        {
            return onSlot.SingleOrDefault();
        }

        var atSlotStart = onSlot.Where(work => work.StartTime == row.StartTime).ToList();
        return atSlotStart.Count == 1 ? atSlotStart[0] : throw new ConflictException(AmbiguousSlotMessage);
    }

    /// <returns>True when the candidate is borrowed from another group under the same root</returns>
    private async Task<bool> EnsureCandidateFitsAsync(
        ReplacementRequest row,
        Work work,
        Guid oldStandInId,
        TimeOnly slotStart,
        TimeOnly slotEnd,
        Guid? groupId,
        Guid token,
        bool overrideBlock,
        CancellationToken cancellationToken)
    {
        var isBorrowed = await EnsureEligibleAsync(
            row, work, oldStandInId, slotStart, slotEnd, groupId, token, overrideBlock, cancellationToken);
        if (await _workChangeRepository.HasOverlappingScenarioReplacementAsync(
                token, row.CandidateClientId, row.Date, work.StartTime, slotStart, slotEnd, cancellationToken))
        {
            throw new ConflictException(AlreadyStandingInMessage);
        }

        return isBorrowed;
    }

    private async Task JoinBorrowedCandidateAsync(
        bool isBorrowed, Guid token, Guid? groupId, ReplacementRequest row, CancellationToken cancellationToken)
    {
        if (isBorrowed && groupId is { } receivingGroupId)
        {
            await _scenarioService.AddScenarioMembershipAsync(
                token, row.CandidateClientId, receivingGroupId, row.Date, row.Date, cancellationToken);
        }
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

    private async Task<bool> EnsureEligibleAsync(
        ReplacementRequest row,
        Work work,
        Guid oldStandInId,
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

        var slot = ReplacementWindow.ToInterval(work.CurrentDate, work.StartTime, slotStart, slotEnd);
        var (lead, trail, extraPaid) = await PaddingOfSlotAsync(work, slot, cancellationToken);
        var search = await _mediator.Send(
            new FindReplacementQuery(
                row.ShiftId, DateOnly.FromDateTime(slot.StartAt), slotStart, slotEnd, searchGroupId, token, overrideBlock,
                lead, trail, extraPaid, row.CandidateClientId),
            cancellationToken);
        var accepted = search.Eligible.FirstOrDefault(candidate => candidate.ClientId == row.CandidateClientId);
        if (accepted is null)
        {
            throw new ConflictException(
                await TookTheSwappedWorkAsync(token, oldStandInId, row, cancellationToken) ? HopReceiverMessage : NotEligibleMessage);
        }

        return accepted.IsBorrowed;
    }

    private async Task<bool> TookTheSwappedWorkAsync(
        Guid token, Guid oldStandInId, ReplacementRequest row, CancellationToken cancellationToken)
    {
        var hops = await _workRepository.ListMovedByScenarioAsync(token, oldStandInId, row.Date, cancellationToken);
        if (hops.Any(hop => hop.ClientId == row.CandidateClientId))
        {
            return true;
        }

        var relocations = await _workChangeRepository.ListReplacementsWrittenByScenarioAsync(
            token, oldStandInId, row.Date, cancellationToken);
        return relocations.Any(change => IsRecoveryChange(change) && change.ReplaceClientId == row.CandidateClientId);
    }

    /// <summary>
    /// The lead, trail and extra paid hours a stand-in takes over with the slot: the lead only when the slot begins at the
    /// start of the work, the trail only when it ends at its end, the travel and correction hours of those sides with them.
    /// </summary>
    private async Task<(decimal LeadHours, decimal TrailHours, decimal ExtraPaidHours)> PaddingOfSlotAsync(
        Work work, (DateTime StartAt, DateTime EndAt) slot, CancellationToken cancellationToken)
    {
        var changes = await _workChangeRepository.ListNonReplacementChangesAsync(work.Id, cancellationToken);
        var padding = RecoveryWorkPaddingReader.Collect(work, changes);
        var insideSlot = padding with
        {
            WithinExtraPaidHours = padding.WithinExtraPaidHours.Where(t => t.At >= slot.StartAt && t.At < slot.EndAt).ToList(),
        };
        var (workStartAt, workEndAt) = ReplacementWindow.ToInterval(work.CurrentDate, work.StartTime, work.StartTime, work.EndTime);
        return insideSlot.ForSegment(workStartAt, workEndAt, [(slot.StartAt, slot.EndAt)], 0);
    }

    private async Task ReplaceStandInAsync(Guid workChangeId, Guid token, Guid candidateId, CancellationToken cancellationToken)
    {
        var resource = await _mediator.Send(new GetWorkChangeInScopeQuery(workChangeId, token), cancellationToken);
        resource.ReplaceClientId = candidateId;

        _ = await _mediator.Send(new PutCommand<WorkChangeResource>(resource), cancellationToken)
            ?? throw new KeyNotFoundException(string.Format(ChangeNotFoundMessage, workChangeId));
    }

    /// <summary>
    /// Takes back what the old stand-in gave up for the cover. On the day of the slot a work stays while they still stand
    /// in at its time or something holds it; a work handed on on another day returns only when it is clear which cover it
    /// belonged to, see <see cref="UndoReliefsAsync"/>.
    /// </summary>
    private async Task RemoveOldStandInRowsAsync(
        Guid token, Guid oldStandInId, ReplacementRequest row, CancellationToken cancellationToken)
    {
        var reliefs = await ListReliefsAsync(token, oldStandInId, row.Date, cancellationToken);
        var onOtherDays = reliefs.Where(relief => relief.Work.CurrentDate != row.Date).ToList();
        if (onOtherDays.Count > 0)
        {
            await UndoReliefsAsync(token, oldStandInId, onOtherDays, row.Date, FirstReliefLevel, cancellationToken);
        }

        foreach (var relief in reliefs.Where(relief => relief.Work.CurrentDate == row.Date))
        {
            if (await _workChangeRepository.HasOverlappingScenarioReplacementAsync(
                    token, oldStandInId, row.Date, relief.Work.StartTime, relief.Start, relief.End, cancellationToken)
                || !await TakeBackAsync(token, oldStandInId, relief, staysWhenHeld: true, cancellationToken))
            {
                continue;
            }

            await SettleReceiverAsync(token, relief, row.Date, FirstReliefLevel, cancellationToken);
        }

        var proposal = await _requestRepository.FindLiveAsync(token, oldStandInId, row.ShiftId, row.Date, cancellationToken);
        if (proposal?.Outcome == ReplacementRequestOutcome.Proposed)
        {
            _requestRepository.Remove(proposal);
        }

        if (!await HoldsACoverOnDayAsync(token, oldStandInId, row.Date, cancellationToken))
        {
            await _scenarioService.RemoveScenarioMembershipsAsync(token, oldStandInId, row.Date, cancellationToken);
        }
    }

    /// <summary>
    /// Gives every listed work back to the person who handed it on. A person who still covers something in the scenario
    /// may have handed the work on for that cover, so the switch is refused instead of guessed.
    /// </summary>
    /// <param name="personId">The person the works return to</param>
    /// <param name="reliefs">The works that person handed on</param>
    /// <param name="slotDate">Workday of the slot the chain was built for</param>
    /// <param name="level">Position of the person in the chain, the old stand-in first</param>
    private async Task UndoReliefsAsync(
        Guid token, Guid personId, IReadOnlyList<Relief> reliefs, DateOnly slotDate, int level, CancellationToken cancellationToken)
    {
        if (await HoldsACoverAsync(token, personId, cancellationToken))
        {
            throw new ConflictException(ChainNotUndoableMessage);
        }

        foreach (var relief in reliefs)
        {
            await TakeBackAsync(token, personId, relief, staysWhenHeld: false, cancellationToken);
            await SettleReceiverAsync(token, relief, slotDate, level, cancellationToken);
        }
    }

    /// <summary>
    /// The receiver of a returned work lost the reason for what they handed on in turn, so that returns too; a receiver
    /// who then covers nothing on the day of the returned work loses the temporary membership of that day.
    /// </summary>
    private async Task SettleReceiverAsync(
        Guid token, Relief returned, DateOnly slotDate, int level, CancellationToken cancellationToken)
    {
        if (level < MaxReliefLevels)
        {
            var passedOn = await ListReliefsAsync(token, returned.ReceiverId, slotDate, cancellationToken);
            if (passedOn.Count > 0)
            {
                await UndoReliefsAsync(token, returned.ReceiverId, passedOn, slotDate, level + 1, cancellationToken);
            }
        }

        if (!await HoldsACoverOnDayAsync(token, returned.ReceiverId, returned.Work.CurrentDate, cancellationToken))
        {
            await _scenarioService.RemoveScenarioMembershipsAsync(
                token, returned.ReceiverId, returned.Work.CurrentDate, cancellationToken);
        }
    }

    /// <summary>
    /// Returns one work: a replaced one loses its recovery replacement, a moved one goes back to its person.
    /// </summary>
    /// <param name="staysWhenHeld">True on the day of the slot, where a held work stays without refusing the switch</param>
    /// <returns>False when the work stayed where it is</returns>
    private async Task<bool> TakeBackAsync(
        Guid token, Guid ownerId, Relief relief, bool staysWhenHeld, CancellationToken cancellationToken)
    {
        if (relief.Relocation is { } relocation)
        {
            _ = await _mediator.Send(new DeleteCommand<WorkChangeResource>(relocation.Id), cancellationToken)
                ?? throw new KeyNotFoundException(string.Format(ChangeNotFoundMessage, relocation.Id));
            return true;
        }

        switch (await _coverMaterializer.ReturnMovedWorkAsync(token, relief.Work.Id, ownerId, cancellationToken))
        {
            case MovedWorkHandOverResult.RecalculationFailed:
                throw new ConflictException(RecalculationFailedMessage);
            case MovedWorkHandOverResult.Held when !staysWhenHeld:
                throw new ConflictException(ChainNotUndoableMessage);
            case MovedWorkHandOverResult.Held:
                return false;
            default:
                return true;
        }
    }

    /// <summary>
    /// The works this scenario took from the person within the relief reach of the engine around the slot, replaced ones
    /// first. A work of a slot the request book lists with that person as the absent one went away for their own absence
    /// and is left out.
    /// </summary>
    private async Task<List<Relief>> ListReliefsAsync(
        Guid token, Guid personId, DateOnly slotDate, CancellationToken cancellationToken)
    {
        var from = slotDate.AddDays(-RulesetDefaults.ReliefReachDays);
        var until = slotDate.AddDays(RulesetDefaults.ReliefReachDays);
        var replaced = await _workChangeRepository.ListReplacementsWrittenByScenarioAsync(
            token, personId, from, until, cancellationToken);
        var moved = await _workRepository.ListMovedByScenarioAsync(token, personId, from, until, cancellationToken);
        var reliefs = replaced.Where(IsRecoveryChange).Select(change => new Relief(change.Work!, change))
            .Concat(moved.Select(work => new Relief(work, null)))
            .ToList();
        if (reliefs.Count == 0)
        {
            return reliefs;
        }

        var ownAbsence = (await _requestRepository.ListLiveByTokenAsync(token, cancellationToken))
            .Where(request => request.AbsentClientId == personId)
            .ToList();
        if (ownAbsence.Count == 0)
        {
            return reliefs;
        }

        var rootShiftIds = await _scenarioService.ResolveRootShiftIdsAsync(
            reliefs.Select(relief => relief.Work.ShiftId).Distinct().ToList(), cancellationToken);
        return reliefs
            .Where(relief => !ownAbsence.Any(request => request.Date == relief.Work.CurrentDate
                && (request.WorkId == relief.Work.Id
                    || request.ShiftId == rootShiftIds.GetValueOrDefault(relief.Work.ShiftId, relief.Work.ShiftId))))
            .ToList();
    }

    private async Task<bool> HoldsACoverAsync(Guid token, Guid personId, CancellationToken cancellationToken)
        => await _workChangeRepository.IsReplacementInScenarioAsync(token, personId, cancellationToken)
            || await _workRepository.HoldsMovedWorkInScenarioAsync(token, personId, cancellationToken);

    /// <summary>
    /// Whether the person stands in on a work or owns a moved work of the scenario whose workday is that day. The
    /// temporary membership is kept per day, so only a cover of the same day keeps the membership of that day.
    /// </summary>
    private async Task<bool> HoldsACoverOnDayAsync(Guid token, Guid personId, DateOnly date, CancellationToken cancellationToken)
        => await _workChangeRepository.IsReplacementInScenarioOnDayAsync(token, personId, date, cancellationToken)
            || await _workRepository.HoldsMovedWorkInScenarioOnDayAsync(token, personId, date, cancellationToken);

    /// <summary>A work a person gave up for a cover: moved to its receiver, or still theirs with a recovery replacement.</summary>
    /// <param name="Work">The work</param>
    /// <param name="Relocation">The recovery replacement on it; null for a moved work</param>
    private sealed record Relief(Work Work, WorkChange? Relocation)
    {
        public TimeOnly Start => Relocation?.StartTime ?? Work.StartTime;

        public TimeOnly End => Relocation?.EndTime ?? Work.EndTime;

        public Guid ReceiverId => Relocation?.ReplaceClientId ?? Work.ClientId;
    }

    private static bool IsRecoveryChange(WorkChange change)
        => string.Equals(change.Description, RecoveryMarkers.WorkChangeSource, StringComparison.Ordinal);
}
