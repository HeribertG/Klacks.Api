// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves a seal_open_orders call into its dry-run, shared by SealOpenOrdersSkill and its confirmation preview provider.
/// An administrator with unrestricted group scope reaches every open order, including orders without a group; every
/// other caller only reaches orders linked to at least one group they can see, may neither name a hidden group nor run
/// the group assignment, and is refused when no group is in scope. groupName is resolved against the reachable groups with
/// the shared GroupResolver under the default Strict policy, so a fuzzy or hidden name is answered with the ask-back and
/// nothing is planned. groupName and autoAssignGroups cannot be combined, and a non-positive maxCount is refused. The
/// resolved command (Apply=false) is sent once; its result carries the full classified selection the binding is built from.
/// </summary>
/// <param name="groupRepository">Lists the groups a group name is resolved against.</param>
/// <param name="groupScopeGuard">Tells whether the caller's group scope is unrestricted and which groups it covers.</param>
/// <param name="mediator">Sends the dry-run SealOpenOrdersCommand.</param>
/// <param name="companyClock">The company's "today" for relative dates.</param>

using Klacks.Api.Application.Commands.Orders;
using Klacks.Api.Application.DTOs.Orders;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Services.Orders;

public sealed class SealOpenOrdersPlanner : ISealOpenOrdersPlanner
{
    private const string NoGroupScopeError =
        "No group is assigned to your group scope, so there is no open order you may seal. " +
        "Ask an administrator to assign you a group scope.";

    private const string AutoAssignRequiresFullAccessError =
        "autoAssignGroups is only available to administrators with unrestricted group scope: the group assignment " +
        "works on orders that have no group yet, and those can only be sealed by an administrator. " +
        "Seal the orders that already belong to your groups, or ask an administrator to run the group assignment.";

    private const string GroupNoteFormat = "Group: '{0}'. ";

    private const string InvalidMaxCountError =
        "maxCount must be greater than 0 when it is given.";

    private const string GroupNameWithAutoAssignError =
        "groupName and autoAssignGroups cannot be combined: the group assignment works on orders that have " +
        "no group at all, so it would write links across the whole installation while groupName is meant to " +
        "restrict the run to one group. Run the group assignment on its own first, or drop groupName.";

    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IMediator _mediator;
    private readonly ICompanyClock _companyClock;

    public SealOpenOrdersPlanner(
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        IMediator mediator,
        ICompanyClock companyClock)
    {
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _mediator = mediator;
        _companyClock = companyClock;
    }

    public async Task<SealOpenOrdersPlan> PlanAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var access = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        var hasFullAccess = access.IsUnrestricted && context.UserPermissions.Contains(Roles.Admin);

        var maxCount = SkillParameterReader.Read<int?>(parameters, "maxCount");
        if (maxCount is <= 0)
        {
            return SealOpenOrdersPlan.Refuse(InvalidMaxCountError);
        }

        var autoAssignGroups = SkillParameterReader.Read<bool?>(parameters, "autoAssignGroups") ?? false;
        var groupName = SkillParameterReader.Read<string>(parameters, "groupName");
        if (autoAssignGroups && !string.IsNullOrWhiteSpace(groupName))
        {
            return SealOpenOrdersPlan.Refuse(GroupNameWithAutoAssignError);
        }

        if (autoAssignGroups && !hasFullAccess)
        {
            return SealOpenOrdersPlan.Refuse(AutoAssignRequiresFullAccessError);
        }

        IReadOnlyList<Group> reachableGroups = hasFullAccess && string.IsNullOrWhiteSpace(groupName)
            ? []
            : await LoadReachableGroupsAsync(access, hasFullAccess);
        if (!hasFullAccess && reachableGroups.Count == 0)
        {
            return SealOpenOrdersPlan.Refuse(NoGroupScopeError);
        }

        var scope = hasFullAccess
            ? OpenOrderSealingScope.FullAccess()
            : OpenOrderSealingScope.ForGroups(reachableGroups.Select(g => g.Id));

        Group? targetGroup = null;
        if (!string.IsNullOrWhiteSpace(groupName))
        {
            var (group, groupError) = GroupResolver.Resolve(reachableGroups, groupName);
            if (group == null)
            {
                return SealOpenOrdersPlan.Refuse(groupError!);
            }

            targetGroup = group;
        }

        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var fromDateStr = SkillParameterReader.Read<string>(parameters, "fromDate");
        var untilDateStr = SkillParameterReader.Read<string>(parameters, "untilDate");
        var (fromDate, invalidFromDate) = SkillDateParser.ParseOptionalUtcDate(fromDateStr, today, context.UserLanguage);
        var (untilDate, invalidUntilDate) = SkillDateParser.ParseOptionalUtcDate(untilDateStr, today, context.UserLanguage);
        if (invalidFromDate)
        {
            return SealOpenOrdersPlan.Refuse(SkillDateParser.InvalidDateMessageFor("fromDate", fromDateStr!));
        }

        if (invalidUntilDate)
        {
            return SealOpenOrdersPlan.Refuse(SkillDateParser.InvalidDateMessageFor("untilDate", untilDateStr!));
        }

        var command = new SealOpenOrdersCommand(
            SkillParameterReader.Read<string>(parameters, "sourceSystemId"),
            fromDate.HasValue ? DateOnly.FromDateTime(fromDate.Value) : null,
            untilDate.HasValue ? DateOnly.FromDateTime(untilDate.Value) : null,
            SkillParameterReader.Read<string>(parameters, "customerName"),
            targetGroup?.Id,
            maxCount,
            autoAssignGroups,
            ValidFrom: null,
            Apply: false,
            context.UserName,
            scope);

        try
        {
            var preview = await _mediator.Send(command, cancellationToken);
            var groupNote = targetGroup == null ? string.Empty : string.Format(GroupNoteFormat, targetGroup.Name);
            return new SealOpenOrdersPlan(null, command, preview, groupNote);
        }
        catch (SkillVerificationException ex)
        {
            return SealOpenOrdersPlan.Refuse(ex.Message);
        }
    }

    private async Task<IReadOnlyList<Group>> LoadReachableGroupsAsync(GroupScopeAccess access, bool hasFullAccess)
    {
        var groups = await _groupRepository.List();
        return hasFullAccess ? groups : groups.Where(access.IsInScope).ToList();
    }
}
