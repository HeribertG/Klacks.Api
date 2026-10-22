// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that hands a placed Work over to another employee on the same day - the chat equivalent of dragging the
/// entry onto another row of the schedule. It sends the same ReassignWorkClientCommand as the UI, so the day lock,
/// the group visibility of both employees, the hard-blocking qualification check, the container children, the period
/// hours of both employees and the SignalR updates all stay in that one handler. The skill only adds what a spoken
/// request needs: it finds the Work by id or by employee and date (a day with several Works must be disambiguated by
/// workId), refuses what the UI never lets a planner drag (a locked Work, a container child) and a sealed day on either
/// side also in a scenario, and reports a collision at the target as a warning exactly as the UI does. A Work or a
/// target employee the caller may not see is answered like an unknown one; a day outside the target's membership or
/// group period is refused by the handler, as the grid refuses the drop. The handler commits before it recalculates, so
/// a failure after the commit is told apart from a refusal by re-reading the stored Work: "not handed over, nothing
/// changed" versus "handed over, a follow-up step failed". The result carries SourceClientId, the employee the Work
/// came from, which is what undoing the hand-over needs.
/// </summary>
/// <param name="workId">Optional. UUID of the Work to hand over; alternative to employee and date.</param>
/// <param name="clientId">Optional. UUID of the employee who currently holds the Work; alternative to the name.</param>
/// <param name="firstName">Optional. First name of the employee who currently holds the Work.</param>
/// <param name="lastName">Optional. Last name of the employee who currently holds the Work.</param>
/// <param name="idNumber">Optional. Visible client number that disambiguates duplicate names of the current holder.</param>
/// <param name="date">Optional. Day of the Work (yyyy-MM-dd, or a day word); required when the Work is found by employee.</param>
/// <param name="targetClientId">Optional. UUID of the employee who takes the Work over; alternative to the name.</param>
/// <param name="targetFirstName">Optional. First name of the employee who takes the Work over.</param>
/// <param name="targetLastName">Optional. Last name of the employee who takes the Work over.</param>
/// <param name="targetIdNumber">Optional. Visible client number that disambiguates duplicate names of the target.</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("reassign_work")]
public class ReassignWorkSkill : BaseSkillImplementation
{
    private const string TimeFormat = "HH\\:mm";
    private const string WorkAction = "handed over";
    private const string TargetIdNumberParameterName = "targetIdNumber";

    private const string CommittedWithFollowUpFailureMessage =
        "Work {0} WAS handed over to {1}: the change is committed. A follow-up step failed afterwards ({2}), so the " +
        "period hours, overtime or schedule entries of both employees may be stale. Check the schedule; do not hand " +
        "the work over again.";

    private const string MissingWorkMessage =
        "Say which work to hand over: give workId, or the employee who holds it (firstName and lastName, optionally " +
        "idNumber, or clientId) together with the date.";

    private const string MissingTargetMessage =
        "Say who takes the work over: give targetFirstName and targetLastName (optionally targetIdNumber), or " +
        "targetClientId. This is the target employee, not the current holder.";

    private const string ContainerChildMessage =
        "Work '{0}' is an entry inside a container work and cannot be handed over on its own. Hand over the " +
        "container work it belongs to; its entries move with it.";

    private const string SameEmployeeMessage = "Work '{0}' is already assigned to {1}; nothing was changed.";

    private const string SealedDayMessage =
        "Day {0} is sealed for {1}; the work was not handed over and nothing was changed.";

    private readonly IMediator _mediator;
    private readonly IWorkRepository _workRepository;
    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly IClientRepository _clientRepository;
    private readonly IClientSearchRepository _searchRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ISealedDayRepository _sealedDayRepository;
    private readonly IPreCommitConflictChecker _conflictChecker;
    private readonly IShiftRepository _shiftRepository;
    private readonly ICompanyClock _companyClock;

    public ReassignWorkSkill(
        IMediator mediator,
        IWorkRepository workRepository,
        IScheduleRowAccessGuard rowAccessGuard,
        IClientRepository clientRepository,
        IClientSearchRepository searchRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        ISealedDayRepository sealedDayRepository,
        IPreCommitConflictChecker conflictChecker,
        IShiftRepository shiftRepository,
        ICompanyClock companyClock)
    {
        _mediator = mediator;
        _workRepository = workRepository;
        _rowAccessGuard = rowAccessGuard;
        _clientRepository = clientRepository;
        _searchRepository = searchRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _sealedDayRepository = sealedDayRepository;
        _conflictChecker = conflictChecker;
        _shiftRepository = shiftRepository;
        _companyClock = companyClock;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var targetFirstName = GetParameter<string>(parameters, "targetFirstName");
        var targetLastName = GetParameter<string>(parameters, "targetLastName");
        var targetClientIdRaw = GetParameter<string>(parameters, "targetClientId");
        if (string.IsNullOrWhiteSpace(targetClientIdRaw)
            && string.IsNullOrWhiteSpace(targetFirstName)
            && string.IsNullOrWhiteSpace(targetLastName))
        {
            return SkillResult.Error(MissingTargetMessage);
        }

        var (target, targetError) = await ClientResolver.ResolveByIdOrNameAsync(
            _searchRepository, _clientRepository, _clientVisibilityGuard, targetClientIdRaw,
            targetFirstName, targetLastName, GetParameter<int?>(parameters, TargetIdNumberParameterName),
            cancellationToken, TargetIdNumberParameterName);
        if (target == null)
        {
            return SkillResult.Error(targetError!);
        }

        var (work, workError) = await FindWorkAsync(context, parameters, cancellationToken);
        if (work == null)
        {
            return SkillResult.Error(workError!);
        }

        if (!await _rowAccessGuard.AreAllVisibleAsync([work.ClientId, target.Id], work.AnalyseToken, cancellationToken))
        {
            return SkillResult.Error(WorkNotFoundMessage(work.Id));
        }

        var refusal = await CheckHandOverAsync(work, target, cancellationToken);
        if (refusal != null)
        {
            return SkillResult.Error(refusal);
        }

        var shift = await _shiftRepository.Get(work.ShiftId);
        var warnings = await CollectWarningsAsync(context, work, target.Id, cancellationToken);

        ReassignWorkClientResponse? response;
        try
        {
            response = await _mediator.Send(new ReassignWorkClientCommand(work.Id, target.Id), cancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            return await DescribeCommittedAsync(work, target, shift, warnings, ex.Message)
                   ?? SkillResult.Error(WorkNotFoundMessage(work.Id));
        }
        catch (Exception ex) when (ex is InvalidRequestException or ConflictException)
        {
            return await DescribeCommittedAsync(work, target, shift, warnings, ex.Message)
                   ?? SkillResult.Error($"Work {work.Id} was not {WorkAction}: {ex.Message} Nothing was changed.");
        }

        if (response?.Work == null)
        {
            return SkillResult.Error($"Work {work.Id} was not {WorkAction}: the hand-over returned no result.");
        }

        var warningNote = warnings.Count > 0
            ? $" Note: {warnings.Count} schedule warning(s) at {DisplayName(target)} were reported and committed anyway."
            : string.Empty;

        return SkillResult.SuccessResult(
            BuildResultData(work, target, shift, warnings, followUpFailed: false),
            $"Work {work.Id} ({shift?.Name ?? work.ShiftId.ToString()}, {work.CurrentDate.ToString(DateTimeFormats.IsoDate)}, " +
            $"{work.StartTime.ToString(TimeFormat)}-{work.EndTime.ToString(TimeFormat)}) was handed over from client " +
            $"{work.ClientId} to {DisplayName(target)}.{warningNote}");
    }

    private static object BuildResultData(
        Work work, Client target, Shift? shift, List<object> warnings, bool followUpFailed)
    {
        return new
        {
            WorkId = work.Id,
            SourceClientId = work.ClientId,
            TargetClientId = target.Id,
            ShiftId = work.ShiftId,
            ShiftName = shift?.Name,
            Date = work.CurrentDate,
            Warnings = warnings,
            FollowUpFailed = followUpFailed
        };
    }

    private async Task<SkillResult?> DescribeCommittedAsync(
        Work work, Client target, Shift? shift, List<object> warnings, string failure)
    {
        var stored = await _workRepository.GetNoTracking(work.Id);
        if (stored == null || stored.ClientId != target.Id)
        {
            return null;
        }

        return SkillResult.SuccessResult(
            BuildResultData(work, target, shift, warnings, followUpFailed: true),
            string.Format(CommittedWithFollowUpFailureMessage, work.Id, DisplayName(target), failure));
    }

    private async Task<(Work? Work, string? Error)> FindWorkAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var workIdRaw = GetParameter<string>(parameters, "workId");
        if (!string.IsNullOrWhiteSpace(workIdRaw))
        {
            if (!Guid.TryParse(workIdRaw, out var workId))
            {
                return (null, $"Invalid workId '{workIdRaw}'. Expected a UUID.");
            }

            var stored = await _workRepository.GetNoTracking(workId);
            return stored == null ? (null, WorkNotFoundMessage(workId)) : (stored, null);
        }

        var clientIdRaw = GetParameter<string>(parameters, "clientId");
        var firstName = GetParameter<string>(parameters, "firstName");
        var lastName = GetParameter<string>(parameters, "lastName");
        var dateRaw = GetParameter<string>(parameters, "date");
        if (string.IsNullOrWhiteSpace(dateRaw)
            || (string.IsNullOrWhiteSpace(clientIdRaw)
                && string.IsNullOrWhiteSpace(firstName)
                && string.IsNullOrWhiteSpace(lastName)))
        {
            return (null, MissingWorkMessage);
        }

        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var (parsedDate, invalidDate) = SkillDateParser.ParseOptionalUtcDate(dateRaw, today, context.UserLanguage);
        if (invalidDate || !parsedDate.HasValue)
        {
            return (null, SkillDateParser.InvalidDateMessageFor("date", dateRaw));
        }

        var (holder, holderError) = await ClientResolver.ResolveByIdOrNameAsync(
            _searchRepository, _clientRepository, _clientVisibilityGuard, clientIdRaw,
            firstName, lastName, GetParameter<int?>(parameters, ClientResolver.IdNumberParameterName), cancellationToken);
        if (holder == null)
        {
            return (null, holderError);
        }

        var date = DateOnly.FromDateTime(parsedDate.Value);
        var candidates = MainPlanWorks.ParentsOnly(await _workRepository.GetByClientAndDateRangeAsync(
            holder.Id, parsedDate.Value, parsedDate.Value, cancellationToken));

        if (candidates.Count == 0)
        {
            return (null, $"{DisplayName(holder)} has no work on {date.ToString(DateTimeFormats.IsoDate)}; nothing was changed.");
        }

        if (candidates.Count > 1)
        {
            return (null, await DescribeAmbiguityAsync(holder, date, candidates));
        }

        return (candidates[0], null);
    }

    private async Task<string> DescribeAmbiguityAsync(Client holder, DateOnly date, List<Work> candidates)
    {
        var lines = new List<string>();
        foreach (var candidate in candidates)
        {
            var shift = await _shiftRepository.Get(candidate.ShiftId);
            lines.Add(
                $"workId {candidate.Id}: {shift?.Name ?? candidate.ShiftId.ToString()}" +
                $"{(string.IsNullOrWhiteSpace(shift?.Abbreviation) ? string.Empty : $" ({shift!.Abbreviation})")}, " +
                $"{candidate.StartTime.ToString(TimeFormat)}-{candidate.EndTime.ToString(TimeFormat)}");
        }

        return $"{DisplayName(holder)} has {candidates.Count} works on {date.ToString(DateTimeFormats.IsoDate)}: " +
               $"{string.Join("; ", lines)}. Ask the user which one is meant, then call again with that workId.";
    }

    private async Task<string?> CheckHandOverAsync(Work work, Client target, CancellationToken cancellationToken)
    {
        if (work.LockLevel != WorkLockLevel.None)
        {
            return WorkLockRefusal.Message(work.Id, work.LockLevel, WorkAction);
        }

        if (work.ParentWorkId != null)
        {
            return string.Format(ContainerChildMessage, work.Id);
        }

        if (work.ClientId == target.Id)
        {
            return string.Format(SameEmployeeMessage, work.Id, DisplayName(target));
        }

        if (await _sealedDayRepository.IsDayLockedAsync(work.CurrentDate, work.ClientId, cancellationToken))
        {
            return string.Format(SealedDayMessage, work.CurrentDate.ToString(DateTimeFormats.IsoDate), "the current holder");
        }

        if (await _sealedDayRepository.IsDayLockedAsync(work.CurrentDate, target.Id, cancellationToken))
        {
            return string.Format(SealedDayMessage, work.CurrentDate.ToString(DateTimeFormats.IsoDate), DisplayName(target));
        }

        return null;
    }

    private async Task<List<object>> CollectWarningsAsync(
        SkillExecutionContext context, Work work, Guid targetClientId, CancellationToken cancellationToken)
    {
        if (work.AnalyseToken != null || work.ParentWorkId != null)
        {
            return [];
        }

        var planned = new PlannedWorkRow(targetClientId, work.CurrentDate, work.StartTime, work.EndTime, work.ShiftId);
        var vacated = new PlannedRemovalRow(work.ClientId, work.CurrentDate, work.StartTime, work.EndTime, work.Id);
        var check = await _conflictChecker.CheckAsync([planned], [vacated], null, cancellationToken);

        return check.NewConflicts
            .Select(conflict => (object)new
            {
                Severity = conflict.Type.ToString(),
                conflict.Comment,
                conflict.Date,
                CommentParams = LocalizedCommentParams.ForLanguage(conflict.CommentParams, context.UserLanguage)
            })
            .ToList();
    }

    private static string WorkNotFoundMessage(Guid workId) => $"Work '{workId}' not found.";

    private static string DisplayName(Client client) => $"{client.FirstName} {client.Name}".Trim();
}
