// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Turns a grouping report into the data the chat model presents: names instead of ids, sentences
/// instead of codes, at most MaxListedItemsPerCode entries per finding kind and per proposal action
/// (with omitted counts), and only what the caller's group scope allows (GroupingScopeVisibility): findings
/// about clients, shifts or groups outside the scope are left out, findings about ungrouped duties and
/// employees stay, and proposals that target a foreign group or move a client or shift living only in
/// foreign groups are left out. The counts (findings, proposals, analysed employees and duties) cover only
/// what the caller may see; the plan code describes the whole analysis, because the apply step recomputes
/// and compares the whole plan. The period is given in ISO and in display form (dd.MM.yyyy) and the
/// visible proposals are counted per kind, zeros included, so the model never has to derive either.
/// PlanningUnits carries the per-unit gaps and blocking causes (GroupingUnitReportBuilder).
/// </summary>
/// <param name="report">Analysis result with ids and display-name lookups.</param>
/// <param name="scope">Caller's group scope; findings and proposals of other groups are left out.</param>
/// <param name="groupName">Resolved name of the analysed subtree, or null for the whole installation.</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Services.Grouping;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Skills;

internal static class GroupingReportViewBuilder
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string PeriodTemplate = "{0} to {1} ({2} to {3})";

    public static GroupingReportView Build(GroupingFeasibilityReport report, GroupScopeAccess scope, string? groupName)
    {
        var visibleFindings = GroupingScopeVisibility.VisibleFindings(report, scope);
        var visibleProposals = GroupingScopeVisibility.VisibleProposals(report, scope);
        var findingGroups = visibleFindings
            .GroupBy(finding => (finding.Code, finding.ReportOnly))
            .ToList();
        var proposalGroups = visibleProposals
            .GroupBy(proposal => proposal.Kind)
            .ToList();
        var limit = GroupingFeasibilityDefaults.MaxListedItemsPerCode;

        var findings = findingGroups
            .SelectMany(group => group.Take(limit))
            .Select(finding => ToView(report, scope, finding))
            .ToList();
        var proposals = proposalGroups
            .SelectMany(group => group.Take(limit))
            .OrderBy(proposal => (int)proposal.Kind)
            .Select((proposal, index) => ToView(report, proposal, index + 1))
            .ToList();
        var omitted = findingGroups
            .Where(group => group.Count() > limit)
            .Select(group => new GroupingOmittedCount(GroupingReportTexts.Findings[group.Key], group.Count() - limit))
            .Concat(proposalGroups
                .Where(group => group.Count() > limit)
                .Select(group => new GroupingOmittedCount(GroupingReportTexts.Actions[group.Key], group.Count() - limit)))
            .ToList();

        var from = report.Request.From.ToString(DateFormat, CultureInfo.InvariantCulture);
        var until = report.Request.Until.ToString(DateFormat, CultureInfo.InvariantCulture);
        return new GroupingReportView(
            from,
            until,
            string.Format(
                CultureInfo.InvariantCulture, PeriodTemplate, from, until,
                report.Request.From.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture),
                report.Request.Until.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture)),
            groupName,
            GroupingFingerprint.Shorten(report.Fingerprint),
            GroupingFeasibilityCounts.From(visibleFindings, visibleProposals.Count),
            new GroupingProposalKindCounts(
                visibleProposals.Count(proposal => proposal.Kind == GroupingProposalKind.CreateGroup),
                visibleProposals.Count(proposal => proposal.Kind == GroupingProposalKind.AddShift),
                visibleProposals.Count(proposal => proposal.Kind == GroupingProposalKind.AddClient),
                visibleProposals.Count(proposal => proposal.Kind == GroupingProposalKind.RemoveClient)),
            scope.IsUnrestricted ? report.AnalysedClientCount : CountVisibleMembers(report, scope, report.ClientNames.Keys),
            scope.IsUnrestricted ? report.AnalysedShiftCount : CountVisibleMembers(report, scope, report.ShiftNames.Keys),
            findings,
            proposals,
            omitted,
            GroupingUnitReportBuilder.Build(report, scope));
    }

    internal static GroupingProposalView ToView(GroupingFeasibilityReport report, GroupingProposal proposal, int step) => new(
        step,
        GroupingReportTexts.Actions[proposal.Kind],
        proposal.GroupId is Guid groupId ? report.GroupNames.GetValueOrDefault(groupId, string.Empty) : GroupingReportTexts.NewGroup,
        proposal.ClientId is Guid clientId ? report.ClientNames.GetValueOrDefault(clientId) : null,
        proposal.ShiftId is Guid shiftId ? report.ShiftNames.GetValueOrDefault(shiftId) : null);

    private static int CountVisibleMembers(GroupingFeasibilityReport report, GroupScopeAccess scope, IEnumerable<Guid> memberIds) =>
        memberIds.Count(memberId => GroupingScopeVisibility.IsMemberVisible(report, scope, memberId));

    private static GroupingFindingView ToView(GroupingFeasibilityReport report, GroupScopeAccess scope, GroupingFinding finding) => new(
        GroupingReportTexts.Findings[(finding.Code, finding.ReportOnly)],
        finding.GroupId is Guid groupId && GroupingScopeVisibility.IsGroupInScope(report, scope, groupId)
            ? report.GroupNames.GetValueOrDefault(groupId)
            : null,
        finding.ShiftId is Guid shiftId ? report.ShiftNames.GetValueOrDefault(shiftId) : null,
        finding.ClientId is Guid clientId ? report.ClientNames.GetValueOrDefault(clientId) : null,
        finding.Reason is GroupingIneligibilityReason reason ? GroupingReportTexts.Reasons[reason] : null,
        finding.ReasonCounts?.Select(count => new GroupingReasonView(GroupingReportTexts.Reasons[count.Reason], count.Count)).ToList(),
        finding.Weekday?.ToString(),
        finding.Demand,
        finding.Supply);
}
