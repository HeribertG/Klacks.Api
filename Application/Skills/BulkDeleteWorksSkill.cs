// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that deletes all Works of one employee in a date range (optionally of one shift) with one sentence, through
/// the same BulkDeleteWorksCommand as the schedule's multi-delete. The model never supplies ids: the
/// IBulkWorkDeletionPlanner resolves employee, range and shift on the server into the exact list, refuses locked
/// Works, sealed days, an unknown or hidden employee, an empty match and more than BulkWorkDeletionDefaults
/// MaxWorksPerCall Works, and the handler adds container children, day lock, group visibility and period hours.
/// The skill is Sensitive: the autonomy gate holds every call for a dedicated confirmation whose text is the
/// server-computed list at that moment (BulkWorkDeletionConfirmationPreviewProvider). The confirmation binds the list,
/// not only the parameters: the preview's work ids travel with the token (ConfirmationBinding, kind Work) and
/// confirm_pending_action hands them to this skill in SkillExecutionContext.ConfirmedActionBinding. The skill plans
/// again right before it deletes and deletes only when that plan has exactly the bound works; a Work that appeared,
/// vanished or was swapped in the meantime deletes nothing and the refusal says how many differ, so the user is asked
/// again with a fresh preview. A call without that binding (a plan step, a goal plan, a direct call) never deletes. A Work
/// whose time or shift changed but whose id is the same is still the bound Work. Anything short of a complete delete is never reported as
/// complete: a refusal or a delete of nothing is a failure, a partial delete is a committed change marked Partial that
/// carries the deleted ids. The handler commits before it recalculates, so a failure after the commit is told apart
/// from a refusal by re-reading which of the planned Works are gone: none (nothing deleted), all (deleted, a follow-up
/// step failed) or some (partial).
/// </summary>
/// <param name="clientId">Optional. UUID of the employee; alternative to the name.</param>
/// <param name="firstName">Optional. First name of the employee whose works are deleted.</param>
/// <param name="lastName">Optional. Last name of the employee whose works are deleted.</param>
/// <param name="idNumber">Optional. Visible client number that disambiguates duplicate names.</param>
/// <param name="fromDate">Required. First day of the range (yyyy-MM-dd, or a day word).</param>
/// <param name="untilDate">Optional. Last day of the range; defaults to fromDate.</param>
/// <param name="shiftId">Optional. UUID of one shift; only works on that shift are deleted.</param>

using FluentValidation;
using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class BulkDeleteWorksSkill : BaseSkillImplementation
{
    public const string SkillName = "bulk_delete_works";

    private const string DateFormat = "yyyy-MM-dd";

    private const string RefusedMessage = "The works of {0} were refused, nothing was deleted: {1}";

    private const string CommittedWithFollowUpFailureMessage =
        "All {0} work(s) of {1} between {2} and {3} WERE deleted: the change is committed. A follow-up step failed " +
        "afterwards ({4}), so the period hours or overtime of the employee may be stale. Check the schedule; do not " +
        "delete again. A deleted work can only be restored one by one: an administrator at any time, the deleting " +
        "user within the undo window.";

    private const string PartialMessage =
        "Only {0} of {1} works of {2} were deleted - not all ({3}). The deleted ones ARE committed; their ids are in " +
        "the result. Check the schedule before telling the user it is done.";

    private const string FailedIdsReason = "{0} failed";

    private readonly IMediator _mediator;
    private readonly IBulkWorkDeletionPlanner _planner;
    private readonly IWorkRepository _workRepository;

    public BulkDeleteWorksSkill(IMediator mediator, IBulkWorkDeletionPlanner planner, IWorkRepository workRepository)
    {
        _mediator = mediator;
        _planner = planner;
        _workRepository = workRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var plan = await _planner.PlanAsync(parameters, context.UserLanguage, cancellationToken);
        if (plan.IsRefused)
        {
            return SkillResult.Error(plan.Refusal!);
        }

        if (context.ConfirmedActionBinding is not { Kind: ConfirmationBindingKinds.Work } confirmed)
        {
            return SkillResult.Error(string.Format(ConfirmationBindingMessages.BindingRequired, SkillName));
        }

        var current = BulkWorkDeletionBinding.Of(plan);
        if (!confirmed.Matches(current))
        {
            return SkillResult.Error(ConfirmationBindingMessages.Describe(confirmed, current));
        }

        var workIds = plan.Works.Select(work => work.WorkId).ToList();

        BulkWorksResponse response;
        try
        {
            response = await _mediator.Send(
                new BulkDeleteWorksCommand(new BulkDeleteWorksRequest { WorkIds = workIds }), cancellationToken);
        }
        catch (ValidationException ex)
        {
            return SkillResult.Error(string.Format(
                RefusedMessage, plan.EmployeeName, string.Join(" ", ex.Errors.Select(error => error.ErrorMessage))));
        }
        catch (Exception ex) when (ex is InvalidRequestException or ConflictException or KeyNotFoundException)
        {
            return await DescribeFailureAsync(plan, workIds, ex.Message);
        }

        if (response.FailedCount > 0 || response.SuccessCount != workIds.Count)
        {
            var reason = string.Format(FailedIdsReason, response.FailedCount);
            return response.DeletedIds.Count == 0
                ? SkillResult.Error(string.Format(RefusedMessage, plan.EmployeeName, reason))
                : PartialResult(plan, response.DeletedIds, workIds.Count, reason);
        }

        return SkillResult.SuccessResult(
            new
            {
                DeletedCount = response.SuccessCount,
                DeletedWorkIds = response.DeletedIds,
                plan.EmployeeName,
                plan.From,
                plan.Until
            },
            $"{response.SuccessCount} work(s) of {plan.EmployeeName} between {plan.From.ToString(DateFormat)} and " +
            $"{plan.Until.ToString(DateFormat)} were deleted. A deleted work can only be restored one by one: an " +
            "administrator at any time, the deleting user within the undo window.");
    }

    private async Task<SkillResult> DescribeFailureAsync(BulkWorkDeletionPlan plan, List<Guid> workIds, string failure)
    {
        var stillActive = (await _workRepository.GetByIdsAsync(workIds)).Select(work => work.Id).ToHashSet();
        var deletedIds = workIds.Where(id => !stillActive.Contains(id)).ToList();

        if (deletedIds.Count == 0)
        {
            return SkillResult.Error(string.Format(RefusedMessage, plan.EmployeeName, failure));
        }

        if (deletedIds.Count == workIds.Count)
        {
            return SkillResult.SuccessResult(
                new
                {
                    DeletedCount = deletedIds.Count,
                    DeletedWorkIds = deletedIds,
                    plan.EmployeeName,
                    plan.From,
                    plan.Until,
                    FollowUpFailed = true
                },
                string.Format(
                    CommittedWithFollowUpFailureMessage,
                    deletedIds.Count, plan.EmployeeName, plan.From.ToString(DateFormat), plan.Until.ToString(DateFormat),
                    failure));
        }

        return PartialResult(plan, deletedIds, workIds.Count, failure);
    }

    private static SkillResult PartialResult(
        BulkWorkDeletionPlan plan, IReadOnlyCollection<Guid> deletedIds, int plannedCount, string reason)
    {
        return SkillResult.SuccessResult(
            new
            {
                DeletedCount = deletedIds.Count,
                DeletedWorkIds = deletedIds,
                PlannedCount = plannedCount,
                Partial = true,
                plan.EmployeeName,
                plan.From,
                plan.Until
            },
            string.Format(PartialMessage, deletedIds.Count, plannedCount, plan.EmployeeName, reason));
    }
}
