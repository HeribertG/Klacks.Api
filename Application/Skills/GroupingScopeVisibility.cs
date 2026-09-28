// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Group-scope rules of the grouping skills. A group is in scope when it or one of its ancestors is a
/// visible root, with the ancestry resolved over Parent (report lineage or the loaded group list) instead
/// of the nested-set Root column, which is empty for most child groups of real installations. A client or
/// shift is visible when it belongs to no group at all or to at least one group in scope, as in the plan
/// view, which shows ungrouped clients to restricted users. A finding is visible when its client and shift
/// are visible and its group is in scope; a finding about an ungrouped duty or employee stays visible even
/// when the proposed target group is outside the scope, but the target's name is withheld. A proposal is
/// visible when its target group is in scope and the client or shift it moves is visible, so a restricted
/// user never learns of a person who only belongs to foreign groups. SnapshotFor gives the counts and the
/// report fingerprint of exactly the visible part, for the requester's inbox entry; for an unrestricted
/// caller it is the snapshot of the whole report.
/// </summary>
/// <param name="report">Analysis with group lineage and direct memberships.</param>
/// <param name="scope">Caller's group scope.</param>
/// <param name="groups">Loaded groups whose ancestry is resolved over Parent.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Services.Grouping;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Skills;

internal static class GroupingScopeVisibility
{
    public static bool IsGroupInScope(GroupingFeasibilityReport report, GroupScopeAccess scope, Guid groupId) =>
        scope.IsUnrestricted
        || (report.GroupLineage.TryGetValue(groupId, out var lineage) ? lineage : [groupId]).Any(scope.IsVisibleRoot);

    public static bool IsMemberVisible(GroupingFeasibilityReport report, GroupScopeAccess scope, Guid? memberId) =>
        scope.IsUnrestricted
        || memberId is not Guid id
        || !report.MemberGroups.TryGetValue(id, out var groupIds)
        || groupIds.Count == 0
        || groupIds.Any(groupId => IsGroupInScope(report, scope, groupId));

    public static bool IsFindingVisible(GroupingFeasibilityReport report, GroupScopeAccess scope, GroupingFinding finding) =>
        IsMemberVisible(report, scope, finding.ClientId)
        && IsMemberVisible(report, scope, finding.ShiftId)
        && (finding.GroupId is not Guid groupId
            || IsAboutUngroupedMember(finding)
            || IsGroupInScope(report, scope, groupId));

    public static bool IsProposalVisible(GroupingFeasibilityReport report, GroupScopeAccess scope, GroupingProposal proposal) =>
        (proposal.GroupId is not Guid groupId || IsGroupInScope(report, scope, groupId))
        && IsMemberVisible(report, scope, proposal.ClientId)
        && IsMemberVisible(report, scope, proposal.ShiftId);

    public static IReadOnlyList<GroupingFinding> VisibleFindings(GroupingFeasibilityReport report, GroupScopeAccess scope) =>
        report.Findings.Where(finding => IsFindingVisible(report, scope, finding)).ToList();

    public static IReadOnlyList<GroupingProposal> VisibleProposals(GroupingFeasibilityReport report, GroupScopeAccess scope) =>
        report.Proposals.Where(proposal => IsProposalVisible(report, scope, proposal)).ToList();

    public static GroupingFeasibilityDailySnapshot SnapshotFor(GroupingFeasibilityReport report, GroupScopeAccess scope)
    {
        if (scope.IsUnrestricted)
        {
            return GroupingFeasibilityDailySnapshot.From(report);
        }

        var findings = VisibleFindings(report, scope);
        return new GroupingFeasibilityDailySnapshot(
            GroupingFingerprint.ForReport(findings),
            GroupingFeasibilityCounts.From(findings, VisibleProposals(report, scope).Count),
            findings.Any(GroupingFinding.IsReportFinding));
    }

    public static IReadOnlyList<Group> FilterByLineage(IEnumerable<Group> groups, GroupScopeAccess scope)
    {
        var list = groups.ToList();
        if (scope.IsUnrestricted)
        {
            return list;
        }

        var tree = new GroupingGroupTree(list.Select(group =>
            new GroupingGroupRecord(group.Id, group.Name, group.Parent, group.Root, null, null)));
        return list.Where(group => tree.SelfAndAncestors(group.Id).Any(scope.IsVisibleRoot)).ToList();
    }

    private static bool IsAboutUngroupedMember(GroupingFinding finding) =>
        finding.Code is GroupingFindingCode.ShiftWithoutGroup or GroupingFindingCode.ClientWithoutGroup;
}
