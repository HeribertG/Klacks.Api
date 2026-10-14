// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that takes back the soft-delete of a single Work entry (the chat equivalent of the schedule's undo toast)
/// through the existing RestoreWorkCommand, so every rule of the REST restore applies unchanged: day seal, sporadic
/// capacity, hard-blocking conflicts, container children and softenings, and the caller rule (a non-admin may only
/// restore their own delete inside the undo window; a sealed Work only an admin). Refusals are reported as such with
/// their reason - an expired window, a sealed Work, a slot that was taken in the meantime - and nothing is half done.
/// The handler commits before it recalculates, so a failure after the commit is told apart from a refusal by re-reading
/// the stored Work: "not restored, nothing changed" versus "restored, a follow-up step failed". A Work the caller
/// cannot restore (unknown, not deleted, deleted by someone else, outside the caller's group visibility, or a channel
/// without a verifiable identity) is answered with one and the same "not found" message.
/// </summary>
/// <param name="workId">Required. UUID of the soft-deleted Work entry (the WorkId the delete reported).</param>

using FluentValidation;
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Validation.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("restore_work")]
public class RestoreWorkSkill : BaseSkillImplementation
{
    private const string DateFormat = "yyyy-MM-dd";

    private const string CommittedWithFollowUpFailureMessage =
        "Work {0} WAS restored (client {1} on {2}): the change is committed. A follow-up step failed afterwards ({3}), " +
        "so the period hours or overtime of the employee may be stale. Check the schedule; do not restore it again.";

    private readonly IMediator _mediator;
    private readonly IWorkRepository _workRepository;

    public RestoreWorkSkill(IMediator mediator, IWorkRepository workRepository)
    {
        _mediator = mediator;
        _workRepository = workRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var workId = GetRequiredGuid(parameters, "workId");
        var wasDeleted = await _workRepository.GetDeletedAsync(workId, cancellationToken) != null;

        try
        {
            var restored = await _mediator.Send(new RestoreWorkCommand(workId), cancellationToken);
            if (restored == null)
            {
                return SkillResult.Error($"Work {workId} was not restored: the restore returned no result.");
            }

            return SkillResult.SuccessResult(
                new
                {
                    WorkId = workId,
                    restored.ClientId,
                    restored.ShiftId,
                    Date = restored.CurrentDate
                },
                $"Work {workId} (client {restored.ClientId} on {restored.CurrentDate.ToString(DateFormat)}) was restored.");
        }
        catch (ValidationException ex)
        {
            return SkillResult.Error(DescribeValidationFailure(workId, ex));
        }
        catch (KeyNotFoundException ex)
        {
            return await DescribeCommittedAsync(workId, wasDeleted, ex.Message)
                   ?? SkillResult.Error(
                       $"Deleted work {workId} not found or not restorable by you: it does not exist, is not deleted, " +
                       "was deleted by someone else, belongs to an employee outside your visibility, or your identity " +
                       "could not be verified in this channel. Nothing was changed.");
        }
        catch (Exception ex) when (ex is InvalidRequestException or ConflictException)
        {
            return await DescribeCommittedAsync(workId, wasDeleted, ex.Message)
                   ?? SkillResult.Error($"Work {workId} was not restored: {ex.Message} Nothing was changed.");
        }
    }

    private async Task<SkillResult?> DescribeCommittedAsync(Guid workId, bool wasDeleted, string failure)
    {
        if (!wasDeleted)
        {
            return null;
        }

        var stored = await _workRepository.GetNoTracking(workId);
        if (stored == null)
        {
            return null;
        }

        return SkillResult.SuccessResult(
            new
            {
                WorkId = workId,
                stored.ClientId,
                stored.ShiftId,
                Date = stored.CurrentDate,
                FollowUpFailed = true
            },
            string.Format(
                CommittedWithFollowUpFailureMessage,
                workId, stored.ClientId, stored.CurrentDate.ToString(DateFormat), failure));
    }

    private static string DescribeValidationFailure(Guid workId, ValidationException exception)
    {
        var messages = exception.Errors.Select(error => error.ErrorMessage).ToList();

        if (messages.Contains(RestoreWorkCommandValidator.UndoWindowExpiredMessage))
        {
            return $"Work {workId} was not restored: the undo window of {WorkRestoreDefaults.UndoWindowSeconds} seconds " +
                   "has expired. Only an administrator can restore it now; otherwise the assignment has to be entered " +
                   "again. Nothing was changed.";
        }

        if (messages.Contains(RestoreWorkCommandValidator.SealedWorkMessage))
        {
            return $"Work {workId} was not restored: it is sealed and only an administrator can restore a sealed work. " +
                   "Nothing was changed.";
        }

        return $"Work {workId} was not restored: {string.Join(" ", messages)}";
    }
}
