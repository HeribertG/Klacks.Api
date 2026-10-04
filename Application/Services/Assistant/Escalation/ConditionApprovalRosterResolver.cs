// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IConditionApprovalRosterResolver"/>. Builds the call order stage by stage - last
/// planner, group planning audience, admins - and runs every candidate through the same eligibility
/// gate: an existing, non-blocked account (deactivation or Identity lockout, via
/// IUserManagementService.IsAccountBlockedAsync) whose expanded rights cover the remediation skill's
/// RequiredPermissions - the ISkillPermissionGate check the delegation handler shares, with the Admin
/// bypass SkillExecutorService.ValidatePermissions applies.
/// Stage 1 exists only for shift-scoped kinds: the audit stamp on a Client names whoever maintained
/// master data, not a planner, and kinds without an EntityId have nothing to look at; the stage-1 planner
/// must also still see one of the finding's groups when the finding has any. Within stages 2
/// and 3 the order is AppUser.EscalationRosterOrder, the same wake-up order the absence roster uses, so
/// an admin's drag'n'drop on the roster card steers both chains. The planning audience already contains
/// every admin; they are held back to stage 3 so a scoped planner is always asked before the fallback.
/// </summary>
/// <param name="workRepository">Stage 1 lookup: the most recent Work touch under the affected shift.</param>
/// <param name="audienceResolver">Stages 2 and 3: group planning audience and admins.</param>
/// <param name="userManagementService">Account existence and blocked state per candidate.</param>
/// <param name="permissionGate">Whether a candidate's current rights cover the remediation skill.</param>
/// <param name="logger">Records why a stage-1 planner was passed over; the outcome itself is silent.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Escalation;

public sealed class ConditionApprovalRosterResolver : IConditionApprovalRosterResolver
{
    private const string PlannerSkippedLogMessage =
        "Condition {ConditionId} ({TriggerKind}): last planner {UserId} skipped from approval stage 1 - {Reason}";

    private const string PlannerNotAPersonReason = "audit actor is not a user account";
    private const string PlannerUnknownReason = "account no longer exists";
    private const string PlannerBlockedReason = "account is deactivated or locked out";
    private const string PlannerLacksPermissionReason = "account lacks the remediation skill's permissions";
    private const string PlannerOutsideVisibilityReason = "account no longer sees any group of the finding";

    private readonly IWorkRepository _workRepository;
    private readonly IPlanningAudienceResolver _audienceResolver;
    private readonly IUserManagementService _userManagementService;
    private readonly ISkillPermissionGate _permissionGate;
    private readonly ILogger<ConditionApprovalRosterResolver> _logger;

    public ConditionApprovalRosterResolver(
        IWorkRepository workRepository,
        IPlanningAudienceResolver audienceResolver,
        IUserManagementService userManagementService,
        ISkillPermissionGate permissionGate,
        ILogger<ConditionApprovalRosterResolver> logger)
    {
        _workRepository = workRepository;
        _audienceResolver = audienceResolver;
        _userManagementService = userManagementService;
        _permissionGate = permissionGate;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EscalationRosterCandidate>> ResolveAsync(
        AgentCondition condition,
        IReadOnlyCollection<string> requiredPermissions,
        CancellationToken cancellationToken = default)
    {
        var roster = new List<EscalationRosterCandidate>();
        var seenUserIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var lastPlanner = await ResolveLastPlannerAsync(condition, requiredPermissions, cancellationToken);
        if (lastPlanner is not null)
        {
            Append(roster, seenUserIds, lastPlanner);
        }

        var adminIds = await _audienceResolver.GetAdminUserIdsAsync(cancellationToken);

        if (condition.GroupId is Guid groupId)
        {
            var audienceIds = await _audienceResolver.GetPlanningUserIdsForGroupAsync(groupId, cancellationToken);
            var plannerIds = audienceIds.Where(id => !adminIds.Contains(id));
            foreach (var planner in await SelectEligibleOrderedAsync(plannerIds, requiredPermissions, cancellationToken))
            {
                Append(roster, seenUserIds, planner);
            }
        }

        foreach (var admin in await SelectEligibleOrderedAsync(adminIds, requiredPermissions, cancellationToken))
        {
            Append(roster, seenUserIds, admin);
        }

        return roster;
    }

    private async Task<AppUser?> ResolveLastPlannerAsync(
        AgentCondition condition, IReadOnlyCollection<string> requiredPermissions, CancellationToken cancellationToken)
    {
        if (condition.EntityId is not Guid shiftId || !AgentTriggerShiftScopedKinds.Contains(condition.TriggerKind))
        {
            return null;
        }

        var actor = await _workRepository.GetLastPlannerAuditActorForShiftAsync(shiftId, cancellationToken);
        if (actor is null)
        {
            return null;
        }

        if (!AuditActorNames.IsPerson(actor))
        {
            LogPlannerSkipped(condition, actor, PlannerNotAPersonReason);
            return null;
        }

        var user = await _userManagementService.FindUserByIdAsync(actor);
        if (user is null)
        {
            LogPlannerSkipped(condition, actor, PlannerUnknownReason);
            return null;
        }

        if (await _userManagementService.IsAccountBlockedAsync(user))
        {
            LogPlannerSkipped(condition, actor, PlannerBlockedReason);
            return null;
        }

        if (!await _permissionGate.HoldsAsync(user, requiredPermissions))
        {
            LogPlannerSkipped(condition, actor, PlannerLacksPermissionReason);
            return null;
        }

        if (!await MaySeeConditionGroupsAsync(condition, user.Id, cancellationToken))
        {
            LogPlannerSkipped(condition, actor, PlannerOutsideVisibilityReason);
            return null;
        }

        return user;
    }

    /// <summary>
    /// Having touched a Work row under the shift once is no proof of seeing that shift today: the planner's
    /// GroupVisibility may have been narrowed since. The approval request names the finding, so stage 1 is
    /// admitted only when the planner still sees one of the condition's groups (the loaded join rows plus
    /// the primary GroupId) - checked on the person's own GroupVisibility rather than on the planning
    /// audience, because a role-less Planer-floor user with the remediation right is a legitimate stage 1.
    /// The join rows are not always loaded; with only the primary group known a planner of another group of
    /// the same shift is passed over, which fails closed - stages 2 and 3 still apply.
    ///
    /// A row without any group is left as before: the group could not be determined, so there is nothing to
    /// check against, and in an installation without groups that is every row while every user is
    /// unrestricted. The person was the last to edit that very shift, so the request tells them nothing
    /// about it they have not already seen.
    /// </summary>
    private async Task<bool> MaySeeConditionGroupsAsync(
        AgentCondition condition, string userId, CancellationToken cancellationToken)
    {
        var groupIds = condition.Groups.Select(group => group.GroupId).ToHashSet();
        if (condition.GroupId is Guid primaryGroupId)
        {
            groupIds.Add(primaryGroupId);
        }

        if (groupIds.Count == 0)
        {
            return true;
        }

        return await _audienceResolver.MaySeeAnyGroupAsync(userId, groupIds, cancellationToken);
    }

    private async Task<IReadOnlyList<AppUser>> SelectEligibleOrderedAsync(
        IEnumerable<string> userIds, IReadOnlyCollection<string> requiredPermissions, CancellationToken cancellationToken)
    {
        var eligible = new List<AppUser>();

        foreach (var userId in userIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!AuditActorNames.IsPerson(userId))
            {
                continue;
            }

            var user = await _userManagementService.FindUserByIdAsync(userId);
            if (user is null || await _userManagementService.IsAccountBlockedAsync(user))
            {
                continue;
            }

            if (await _permissionGate.HoldsAsync(user, requiredPermissions))
            {
                eligible.Add(user);
            }
        }

        return eligible
            .OrderBy(u => u.EscalationRosterOrder)
            .ThenBy(u => u.UserName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void Append(List<EscalationRosterCandidate> roster, HashSet<string> seenUserIds, AppUser user)
    {
        if (!seenUserIds.Add(user.Id))
        {
            return;
        }

        roster.Add(new EscalationRosterCandidate(user.Id, EscalationCandidateDisplayName.Build(user)));
    }

    private void LogPlannerSkipped(AgentCondition condition, string actor, string reason)
    {
        _logger.LogInformation(PlannerSkippedLogMessage, condition.Id, condition.TriggerKind, actor, reason);
    }
}
