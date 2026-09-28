// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Read-only check whether employees and duties are grouped so the schedule can be planned: runs the
/// grouping feasibility analysis for a period (default: company today plus 56 days) and optionally one
/// subtree, returns the report as plain sentences with names, the proposed group changes and the plan
/// code the apply step needs, and leaves one inbox entry per distinct report. Changes no data. When the
/// report proposes changes, the shown plan counts as this user's preview of it (full plan fingerprint,
/// current chat turn), so the user's confirmation in a later turn can apply it without a second preview;
/// an apply in this same turn stays refused.
/// </summary>
/// <param name="analyzer">Grouping feasibility analysis.</param>
/// <param name="notifier">Inbox delivery without duplicating the daily admin report.</param>
/// <param name="groupRepository">Resolves the optional subtree by name.</param>
/// <param name="groupScopeGuard">Restricts names and findings to the caller's group scope.</param>
/// <param name="companyClock">Company today for the default period.</param>
/// <param name="previewRegistry">Records the shown plan as the user's preview for the apply gate.</param>
/// <param name="logger">Duration and size of each run (live timing of the spec's 10 s rule).</param>

using System.Diagnostics;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(GroupingSkillNames.Analyze)]
public class AnalyzeGroupingFeasibilitySkill : BaseSkillImplementation
{
    private readonly IGroupingFeasibilityAnalyzer _analyzer;
    private readonly IGroupingFeasibilityNotifier _notifier;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly ICompanyClock _companyClock;
    private readonly IGroupingPlanPreviewRegistry _previewRegistry;
    private readonly ILogger<AnalyzeGroupingFeasibilitySkill> _logger;

    public AnalyzeGroupingFeasibilitySkill(
        IGroupingFeasibilityAnalyzer analyzer,
        IGroupingFeasibilityNotifier notifier,
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        ICompanyClock companyClock,
        IGroupingPlanPreviewRegistry previewRegistry,
        ILogger<AnalyzeGroupingFeasibilitySkill> logger)
    {
        _analyzer = analyzer;
        _notifier = notifier;
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _companyClock = companyClock;
        _previewRegistry = previewRegistry;
        _logger = logger;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var (from, until, periodError) = await GroupingSkillInputs.ResolvePeriodAsync(
            parameters, _companyClock, context.UserLanguage, cancellationToken);
        if (periodError is not null)
        {
            return SkillResult.Error(periodError);
        }

        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        var (groupId, groupName, groupError) = await GroupingSkillInputs.ResolveScopeGroupAsync(
            GetParameter<string>(parameters, GroupingSkillInputs.GroupNameParameter), _groupRepository, scope);
        if (groupError is not null)
        {
            return SkillResult.Error(groupError);
        }

        var snapshotGeneration = _notifier.CaptureSnapshotGeneration();
        var stopwatch = Stopwatch.StartNew();
        var report = await _analyzer.AnalyzeAsync(new GroupingAnalysisRequest(from, until, groupId), cancellationToken);
        stopwatch.Stop();
        _logger.LogInformation(
            "Grouping feasibility analysed {Clients} employee(s) and {Shifts} duty/duties in {ElapsedMs} ms: {Findings} finding(s), {Proposals} proposal(s)",
            report.AnalysedClientCount, report.AnalysedShiftCount, stopwatch.ElapsedMilliseconds,
            report.Findings.Count, report.Proposals.Count);

        await _notifier.NotifyAsync(
            report,
            GroupingScopeVisibility.SnapshotFor(report, scope),
            context.UserId,
            context.UserPermissions.Contains(Roles.Admin),
            snapshotGeneration,
            cancellationToken);

        if (report.Proposals.Count > 0)
        {
            _previewRegistry.RecordPreview(context.UserId, report.Fingerprint, context.TurnId);
        }

        var view = GroupingReportViewBuilder.Build(report, scope, groupName);
        return SkillResult.SuccessResult(view, GroupingReportTexts.Summary(view));
    }
}
