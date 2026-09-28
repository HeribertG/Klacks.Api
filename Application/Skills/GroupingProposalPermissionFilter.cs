// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Splits the proposals of a grouping plan into those the caller may carry out and those that are
/// skipped with a reason. Adding a duty needs the rights of add_shift_to_group (edit shifts, view
/// groups), adding an employee those of add_client_to_group (edit clients, view groups), removing one
/// that of remove_client_from_group (edit clients); creating a group needs the right to create groups
/// and an unrestricted group scope (a restricted user may not create a top-level group). Every target
/// group must be inside the caller's group scope, and an employee may only be added when the caller can
/// already see them (ungrouped, or at least one group in scope): adding a person who only belongs to foreign
/// groups to a group in scope would widen the caller's view to that person. The result is independent of the proposal order and
/// keeps the dependencies of the plan intact: when the creation of the new group is skipped, every change
/// that targets the new group is skipped as well (the applier rejects a plan that targets a group it does
/// not create), and when an addition of an employee is skipped, the removals of that employee are skipped
/// too, because the plan only removes a membership while the employee keeps another group, which may be
/// exactly the skipped addition. A permitted creation of the new group is skipped as well when no
/// permitted change targets the new group, so no empty group is created. Scope is checked over the group
/// ancestry (GroupingScopeVisibility), not the nested-set Root column. Admins hold every right.
/// </summary>
/// <param name="report">Analysis whose proposals are partitioned; supplies the group lineage for the scope check.</param>
/// <param name="permissions">Roles and permissions of the caller.</param>
/// <param name="scope">Caller's group scope.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Skills;

internal static class GroupingProposalPermissionFilter
{
    public const string MissingCreateGroupRight = "the right to create groups is missing";
    public const string RootGroupOutOfScope = "creating a top-level group is outside the assigned group scope";
    public const string MissingShiftRight = "the right to assign duties to groups is missing";
    public const string MissingClientRight = "the right to change employees' groups is missing";
    public const string GroupOutOfScope = "the group is outside the assigned group scope";
    public const string ClientOutOfScope = "the employee belongs only to groups outside the assigned group scope";
    public const string NewGroupSkipped = "the new group cannot be created";
    public const string AdditionSkipped = "the employee's proposed addition to another group is skipped, so the employee could lose the last group";
    public const string NoDependentChanges = "no permitted change would go into the new group, so it would stay empty";

    public static GroupingProposalDecision Partition(
        GroupingFeasibilityReport report, IReadOnlyList<string> permissions, GroupScopeAccess scope)
    {
        bool Has(string permission) => permissions.Contains(Roles.Admin) || permissions.Contains(permission);
        bool InScope(Guid groupId) => GroupingScopeVisibility.IsGroupInScope(report, scope, groupId);

        var createGroupReason = !Has(Permissions.CanCreateGroups)
            ? MissingCreateGroupRight
            : scope.IsUnrestricted ? null : RootGroupOutOfScope;

        string? TargetReason(GroupingProposal proposal) => proposal.GroupId is Guid groupId
            ? (InScope(groupId) ? null : GroupOutOfScope)
            : (createGroupReason is null ? null : NewGroupSkipped);

        string? ClientReason(GroupingProposal proposal) =>
            GroupingScopeVisibility.IsMemberVisible(report, scope, proposal.ClientId) ? null : ClientOutOfScope;

        string? OwnReason(GroupingProposal proposal) => proposal.Kind switch
        {
            GroupingProposalKind.CreateGroup => createGroupReason,
            GroupingProposalKind.AddShift => !Has(Permissions.CanEditShifts) || !Has(Permissions.CanViewGroups)
                ? MissingShiftRight
                : TargetReason(proposal),
            GroupingProposalKind.AddClient => !Has(Permissions.CanEditClients) || !Has(Permissions.CanViewGroups)
                ? MissingClientRight
                : TargetReason(proposal) ?? ClientReason(proposal),
            _ => !Has(Permissions.CanEditClients) ? MissingClientRight : TargetReason(proposal),
        };

        var reasons = report.Proposals.Select(proposal => (Proposal: proposal, Reason: OwnReason(proposal))).ToList();
        var clientsWithSkippedAddition = reasons
            .Where(item => item.Reason is not null && item.Proposal.Kind == GroupingProposalKind.AddClient && item.Proposal.ClientId is not null)
            .Select(item => item.Proposal.ClientId!.Value)
            .ToHashSet();

        var permitted = new List<GroupingProposal>();
        var skipped = new List<GroupingSkippedProposal>();
        foreach (var (proposal, ownReason) in reasons)
        {
            var reason = ownReason
                ?? (proposal.Kind == GroupingProposalKind.RemoveClient
                    && proposal.ClientId is Guid clientId
                    && clientsWithSkippedAddition.Contains(clientId)
                        ? AdditionSkipped
                        : null);

            if (reason is null)
            {
                permitted.Add(proposal);
            }
            else
            {
                skipped.Add(new GroupingSkippedProposal(proposal, reason));
            }
        }

        var createGroup = permitted.FirstOrDefault(proposal => proposal.Kind == GroupingProposalKind.CreateGroup);
        if (createGroup is not null && !permitted.Any(proposal => proposal.Kind != GroupingProposalKind.CreateGroup && proposal.GroupId is null))
        {
            permitted.Remove(createGroup);
            skipped.Add(new GroupingSkippedProposal(createGroup, NoDependentChanges));
        }

        return new GroupingProposalDecision(permitted, skipped);
    }
}
