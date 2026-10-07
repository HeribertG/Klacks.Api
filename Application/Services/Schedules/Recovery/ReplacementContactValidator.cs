// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Validates a contact attempt of the recovery dialog. Both employees must be visible; the shift must exist,
/// must be a real shift (never a scenario clone - the book keys on source shifts) and must belong to a visible
/// group or to none; an optional group must exist and be visible; an optional scenario token must reference an
/// existing scenario of a visible group (a group-less scenario only for unrestricted callers); an optional
/// absence type must exist; the slot date must lie within ReplacementRequestLimits.MaxContactPastDays /
/// MaxContactFutureDays around the company's today. Hidden is answered exactly like missing.
/// </summary>
/// <param name="clientVisibilityGuard">Visibility of the two employees</param>
/// <param name="groupVisibilityGuard">Visibility of groups and scenarios</param>
/// <param name="shiftRepository">Shift lookup and its groups</param>
/// <param name="groupRepository">Group existence</param>
/// <param name="scenarioRepository">Scenario lookup by token</param>
/// <param name="absenceRepository">Absence type existence</param>
/// <param name="companyClock">Company's today for the date plausibility</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Application.Services.Schedules.Recovery;

public sealed class ReplacementContactValidator : IReplacementContactValidator
{
    private const string ClientNotFoundMessage = "Client not found";
    private const string ShiftNotFoundMessage = "Shift not found";
    private const string GroupNotFoundMessage = "Group not found";
    private const string ScenarioNotFoundMessage = "Scenario not found";
    private const string AbsenceNotFoundMessage = "Absence not found";
    private const string EmptyIdMessage = "CandidateClientId, AbsentClientId and ShiftId are required.";
    private const string SameClientMessage = "The candidate cannot be the absent employee.";
    private const string DateOutOfRangeMessage = "The slot date {0:yyyy-MM-dd} lies outside the recordable window ({1:yyyy-MM-dd} to {2:yyyy-MM-dd}).";

    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IShiftRepository _shiftRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IAbsenceRepository _absenceRepository;
    private readonly ICompanyClock _companyClock;

    public ReplacementContactValidator(
        IClientVisibilityGuard clientVisibilityGuard,
        IGroupVisibilityGuard groupVisibilityGuard,
        IShiftRepository shiftRepository,
        IGroupRepository groupRepository,
        IAnalyseScenarioRepository scenarioRepository,
        IAbsenceRepository absenceRepository,
        ICompanyClock companyClock)
    {
        _clientVisibilityGuard = clientVisibilityGuard;
        _groupVisibilityGuard = groupVisibilityGuard;
        _shiftRepository = shiftRepository;
        _groupRepository = groupRepository;
        _scenarioRepository = scenarioRepository;
        _absenceRepository = absenceRepository;
        _companyClock = companyClock;
    }

    public async Task ValidateAsync(RecordReplacementContactRequest request, CancellationToken cancellationToken = default)
    {
        if (request.CandidateClientId == Guid.Empty || request.AbsentClientId == Guid.Empty || request.ShiftId == Guid.Empty)
        {
            throw new InvalidRequestException(EmptyIdMessage);
        }

        if (request.CandidateClientId == request.AbsentClientId)
        {
            throw new InvalidRequestException(SameClientMessage);
        }

        await EnsureDatePlausibleAsync(request.Date, cancellationToken);

        if (!await _clientVisibilityGuard.AreAllVisibleAsync(
                [request.CandidateClientId, request.AbsentClientId], cancellationToken))
        {
            throw new KeyNotFoundException(ClientNotFoundMessage);
        }

        await EnsureShiftVisibleAsync(request.ShiftId, cancellationToken);

        if (request.GroupId is { } groupId
            && (!await _groupRepository.Exists(groupId)
                || !await _groupVisibilityGuard.IsGroupVisibleAsync(groupId, cancellationToken)))
        {
            throw new KeyNotFoundException(GroupNotFoundMessage);
        }

        if (request.AnalyseToken is { } token)
        {
            await EnsureScenarioVisibleAsync(token, cancellationToken);
        }

        if (request.AbsenceId is { } absenceId && !await _absenceRepository.Exists(absenceId))
        {
            throw new KeyNotFoundException(AbsenceNotFoundMessage);
        }
    }

    private async Task EnsureDatePlausibleAsync(DateOnly date, CancellationToken cancellationToken)
    {
        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var earliest = today.AddDays(-ReplacementRequestLimits.MaxContactPastDays);
        var latest = today.AddDays(ReplacementRequestLimits.MaxContactFutureDays);
        if (date < earliest || date > latest)
        {
            throw new InvalidRequestException(string.Format(DateOutOfRangeMessage, date, earliest, latest));
        }
    }

    private async Task EnsureShiftVisibleAsync(Guid shiftId, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.GetNoTracking(shiftId);
        if (shift is null || shift.AnalyseToken.HasValue || shift.ScenarioSourceShiftId.HasValue)
        {
            throw new KeyNotFoundException(ShiftNotFoundMessage);
        }

        var groups = await _shiftRepository.GetGroupsForShift(shiftId);
        if (groups.Count == 0)
        {
            return;
        }

        foreach (var group in groups)
        {
            if (await _groupVisibilityGuard.IsGroupVisibleAsync(group.Id, cancellationToken))
            {
                return;
            }
        }

        throw new KeyNotFoundException(ShiftNotFoundMessage);
    }

    private async Task EnsureScenarioVisibleAsync(Guid token, CancellationToken cancellationToken)
    {
        var scenario = await _scenarioRepository.GetByTokenAsync(token, cancellationToken);
        var visible = scenario switch
        {
            null => false,
            { GroupId: { } scenarioGroupId } => await _groupVisibilityGuard.IsGroupVisibleAsync(scenarioGroupId, cancellationToken),
            _ => await _groupVisibilityGuard.IsUnrestrictedAsync(cancellationToken)
        };

        if (!visible)
        {
            throw new KeyNotFoundException(ScenarioNotFoundMessage);
        }
    }
}
