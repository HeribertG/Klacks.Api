// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IReplacementWriteGuard"/>. A replacement without a valid person is refused with
/// InvalidRequestException. The conflict check applies to the real plan only (a scenario runs in a sandbox that bypasses
/// the hard write guards) and only to a change that names a person: a missing mandatory qualification blocks
/// unconditionally, a block-mode compliance escalation blocks unless an authorized supervisor overrides it. A schedule
/// collision alone does not block (owner decision 2026-08-22): it is persisted and the async post-commit check reports it.
/// A replacement that hands no hours over is not checked, since it occupies the substitute for no time.
/// An update that keeps person and Work and does not go beyond the stored window is not checked; one that does is
/// checked with its whole window, and the stored window is taken off the hours it would otherwise count a second time
/// (see <see cref="ReplacementConflictPlan"/>).
/// </summary>
/// <param name="clientRepository">Reads the type of the replacement person; its query filter hides deleted clients</param>
/// <param name="conflictChecker">Finds the conflicts the substitute would get in the handed-over time</param>
/// <param name="overrideAuthorizer">Decides whether the caller may override a block-mode escalation</param>
/// <param name="containerWorkChangeGuard">Refuses a replacement on a container work: a container is covered through a split</param>

using System.Globalization;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public class ReplacementWriteGuard : IReplacementWriteGuard
{
    private readonly IClientRepository _clientRepository;
    private readonly IPreCommitConflictChecker _conflictChecker;
    private readonly ISupervisorOverrideAuthorizer _overrideAuthorizer;
    private readonly IContainerWorkChangeGuard _containerWorkChangeGuard;

    public ReplacementWriteGuard(
        IClientRepository clientRepository,
        IPreCommitConflictChecker conflictChecker,
        ISupervisorOverrideAuthorizer overrideAuthorizer,
        IContainerWorkChangeGuard containerWorkChangeGuard)
    {
        _clientRepository = clientRepository;
        _conflictChecker = conflictChecker;
        _overrideAuthorizer = overrideAuthorizer;
        _containerWorkChangeGuard = containerWorkChangeGuard;
    }

    public async Task EnsureAllowedAsync(
        WorkChange workChange,
        Work parentWork,
        bool overrideBlockRequested,
        WorkChange? storedWorkChange,
        CancellationToken cancellationToken)
    {
        await _containerWorkChangeGuard.EnsureNoReplacementAsync(workChange, parentWork);
        await EnsureValidPersonAsync(workChange, cancellationToken);

        if (workChange.AnalyseToken != null || !workChange.ReplaceClientId.HasValue)
        {
            return;
        }

        if (!ReplacementWindow.TryGetInterval(
                workChange.Type,
                parentWork.StartTime,
                parentWork.EndTime,
                parentWork.CurrentDate,
                workChange.StartTime,
                workChange.EndTime,
                workChange.ChangeTime,
                out var windowStart,
                out var windowEnd))
        {
            return;
        }

        var plan = ReplacementConflictPlan.For(workChange, storedWorkChange, parentWork, windowStart, windowEnd);
        if (plan.Added.Count == 0)
        {
            return;
        }

        await EnsureNoBlockingConflictAsync(workChange, parentWork, overrideBlockRequested, plan, cancellationToken);
    }

    private async Task EnsureValidPersonAsync(WorkChange workChange, CancellationToken cancellationToken)
    {
        if (!ReplacementWorkChangeTypes.IsReplacement(workChange.Type))
        {
            return;
        }

        if (workChange.ReplaceClientId is not { } personId || personId == Guid.Empty)
        {
            throw new InvalidRequestException(ReplacementWriteMessages.PersonRequired);
        }

        var person = await _clientRepository.GetTypeAndDisplayNameAsync(personId, cancellationToken);
        if (person == null)
        {
            throw new InvalidRequestException(ReplacementWriteMessages.PersonNotFound);
        }

        if (!ReplacementPersonRule.IsEligible(person.Type))
        {
            throw new InvalidRequestException(ReplacementWriteMessages.PersonNotAnEmployee);
        }
    }

    private async Task EnsureNoBlockingConflictAsync(
        WorkChange workChange,
        Work parentWork,
        bool overrideBlockRequested,
        ReplacementConflictPlan plan,
        CancellationToken cancellationToken)
    {
        var conflictCheck = await _conflictChecker.CheckAsync(plan.Added, plan.Removed, null, cancellationToken);
        if (!conflictCheck.HasHardBlocking && !conflictCheck.HasOverridableBlocking)
        {
            return;
        }

        var authorized = await _overrideAuthorizer.IsAuthorizedAsync(overrideBlockRequested);
        if (authorized && !conflictCheck.HasHardBlocking)
        {
            return;
        }

        throw new ConflictException(string.Format(
            CultureInfo.InvariantCulture,
            ReplacementWriteMessages.Blocked,
            workChange.ReplaceClientId,
            conflictCheck.NewConflicts.Count(c => c.Type == ScheduleValidationType.Error),
            parentWork.CurrentDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)));
    }
}
