// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Carries out a reviewed grouping plan. Recomputes the analysis on the server with the same period and
/// subtree and refuses when the plan code no longer matches (stale guard). With apply=false (default) it
/// only previews the permitted changes and the skipped ones; with apply=true it applies the permitted
/// changes in one transaction - first create/add, then remove - and relays the database-verified counts.
/// apply=true is refused unless the same user previewed the same recomputed plan (here with apply=false, or
/// through the grouping check that showed it) within
/// GroupingFeasibilityDefaults.PreviewValidityMinutes (server-side gate: a model that jumps straight to
/// apply after a bare "yes" in a fresh conversation is sent back to the preview) and in an earlier chat
/// turn than the apply (a model that previews and applies in one turn is told to ask the user first; the
/// preview stays valid for the user's next turn); a successful apply consumes the preview.
/// Start date of new memberships defaults to the start of the checked period when validFrom is not
/// supplied (stated explicitly in the result); a supplied validFrom is used and validated as before. The
/// name of a new group is asked from the user, never invented.
/// Skipped changes that target a group outside the caller's group scope, or that would add a person who
/// only belongs to foreign groups, are only counted, never named. After a successful apply the analysis is
/// recomputed for the same period and subtree and the remaining gaps per planning unit (unfillable and
/// uncovered duties, capacity shortfalls, blocking causes) are appended with the statement that they need
/// master-data changes, so the model cannot claim that everything is plannable now; a failed recompute is
/// logged and reported as such, never as a failed apply.
/// </summary>
/// <param name="analyzer">Recomputes the plan for the stale guard.</param>
/// <param name="applier">Transactional, verified apply.</param>
/// <param name="groupRepository">Resolves the optional subtree by name.</param>
/// <param name="groupScopeGuard">Caller's group scope.</param>
/// <param name="companyClock">Company today for dates and the snapshot key.</param>
/// <param name="snapshotStore">Daily report snapshot, dropped after a successful apply so the next detector tick recomputes.</param>
/// <param name="previewRegistry">Which user previewed which plan fingerprint in which chat turn (gate of apply=true).</param>
/// <param name="logger">Failures of the recompute after a successful apply.</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Application.Services.Grouping;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(GroupingSkillNames.Apply)]
public class ApplyGroupingPlanSkill : BaseSkillImplementation
{
    public const string FingerprintParameter = "fingerprint";
    public const string ApplyParameter = "apply";
    public const string ValidFromParameter = "validFrom";
    public const string NewGroupNameParameter = "newGroupName";
    public const string PreviewRequiredMarker = "preview required";
    public const string SameTurnMarker = "user confirmation required";

    private const string MissingFingerprintMessage =
        "The plan code from the group plannability check is required. Run the check first and use the code it reports.";
    private const string StalePlanMessage =
        "The data changed since the check, so this plan code is no longer valid (the current plan has the code {0}). "
        + "Nothing was changed. Show the user the new check result before applying anything.";
    private const string PreviewRequiredMessage =
        "Not applied (" + PreviewRequiredMarker + "): this user has not previewed this plan in the last {0} minutes. "
        + "Call apply_grouping_plan with apply=false and the same fingerprint first, show the user the listed changes "
        + "and ask for confirmation; only after the user confirms that preview call again with apply=true. Nothing was changed.";
    private const string SameTurnMessage =
        "Not applied (" + SameTurnMarker + "): the preview of this plan was shown in this same turn, so the user has not "
        + "confirmed it yet. Show the user the listed changes and ask the user to confirm first; call again with apply=true "
        + "and the same fingerprint only after the user has answered. Nothing was changed.";
    private const string NothingToChangeMessage = "The current plan contains no group changes. Nothing was changed.";
    private const string NothingPermittedMessage = "None of the proposed changes may be carried out by this user: {0}. Nothing was changed.";
    private const string MissingGroupNameMessage =
        "The plan creates a new group. Ask the user for its name and call again with newGroupName. Nothing was changed.";
    private const string AskForGroupNameInPreview = " Ask the user for the name of the new group (newGroupName).";
    private const string PreviewTemplate =
        "Preview: {0} change(s) would be made in this order: {1}.{2} Nothing was changed yet.{3}{4} "
        + "Ask the user to confirm, then call again with apply=true and the same fingerprint.";
    private const string SkippedTemplate = " {0} change(s) are skipped: {1}.";
    private const string HiddenSkippedTemplate = " {0} further change(s) concern groups or people outside the assigned group scope and are skipped.";
    private const string AppliedTemplate =
        "Created {0} group(s), added {1} duty/duties and {2} employee(s) to groups, removed {3} employee(s) from groups; "
        + "all confirmed in the database (verified). {4} change(s) were already in place.{5}{6}";
    private const string StartDateGivenTemplate = " Membership start date: {0:yyyy-MM-dd}.";
    private const string StartDateDefaultedTemplate =
        " Membership start date: {0:yyyy-MM-dd} (no validFrom was given, so the start of the checked period applies).";
    private const string ListSeparator = "; ";

    private readonly IGroupingFeasibilityAnalyzer _analyzer;
    private readonly IGroupingPlanApplier _applier;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly ICompanyClock _companyClock;
    private readonly IGroupingFeasibilityDailySnapshotStore _snapshotStore;
    private readonly IGroupingPlanPreviewRegistry _previewRegistry;
    private readonly ILogger<ApplyGroupingPlanSkill> _logger;

    public ApplyGroupingPlanSkill(
        IGroupingFeasibilityAnalyzer analyzer,
        IGroupingPlanApplier applier,
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        ICompanyClock companyClock,
        IGroupingFeasibilityDailySnapshotStore snapshotStore,
        IGroupingPlanPreviewRegistry previewRegistry,
        ILogger<ApplyGroupingPlanSkill> logger)
    {
        _analyzer = analyzer;
        _applier = applier;
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _companyClock = companyClock;
        _snapshotStore = snapshotStore;
        _previewRegistry = previewRegistry;
        _logger = logger;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var fingerprint = GetParameter<string>(parameters, FingerprintParameter);
        if (string.IsNullOrWhiteSpace(fingerprint))
        {
            return SkillResult.Error(MissingFingerprintMessage);
        }

        var apply = GetParameter<bool?>(parameters, ApplyParameter) ?? false;
        var (from, until, periodError) = await GroupingSkillInputs.ResolvePeriodAsync(
            parameters, _companyClock, context.UserLanguage, cancellationToken);
        if (periodError is not null)
        {
            return SkillResult.Error(periodError);
        }

        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        var (groupId, _, groupError) = await GroupingSkillInputs.ResolveScopeGroupAsync(
            GetParameter<string>(parameters, GroupingSkillInputs.GroupNameParameter), _groupRepository, scope);
        if (groupError is not null)
        {
            return SkillResult.Error(groupError);
        }

        var report = await _analyzer.AnalyzeAsync(new GroupingAnalysisRequest(from, until, groupId), cancellationToken);
        if (!GroupingFingerprint.Matches(report.Fingerprint, fingerprint))
        {
            return SkillResult.Error(string.Format(
                CultureInfo.InvariantCulture, StalePlanMessage, GroupingFingerprint.Shorten(report.Fingerprint)));
        }

        if (report.Proposals.Count == 0)
        {
            return SkillResult.SuccessResult(new { Applied = false }, NothingToChangeMessage);
        }

        if (apply)
        {
            var previewStatus = _previewRegistry.GetPreviewStatus(context.UserId, report.Fingerprint, context.TurnId);
            if (previewStatus == GroupingPreviewStatus.Missing)
            {
                return SkillResult.Error(string.Format(
                    CultureInfo.InvariantCulture, PreviewRequiredMessage, GroupingFeasibilityDefaults.PreviewValidityMinutes));
            }

            if (previewStatus == GroupingPreviewStatus.SameTurn)
            {
                return SkillResult.Error(SameTurnMessage);
            }
        }

        var decision = GroupingProposalPermissionFilter.Partition(report, context.UserPermissions, scope);
        var steps = decision.Permitted
            .OrderBy(proposal => (int)proposal.Kind)
            .Select((proposal, index) => GroupingReportViewBuilder.ToView(report, proposal, index + 1))
            .ToList();
        var skipped = decision.Skipped
            .Where(item => GroupingScopeVisibility.IsProposalVisible(report, scope, item.Proposal))
            .Select(item => new { Change = GroupingReportViewBuilder.ToView(report, item.Proposal, 0), item.Reason })
            .ToList();
        var hiddenSkippedCount = decision.Skipped.Count - skipped.Count;
        var skippedText = (skipped.Count == 0
                ? string.Empty
                : string.Format(CultureInfo.InvariantCulture, SkippedTemplate, skipped.Count,
                    string.Join(ListSeparator, skipped.Select(item => $"{item.Change.Action} ({item.Reason})"))))
            + (hiddenSkippedCount == 0
                ? string.Empty
                : string.Format(CultureInfo.InvariantCulture, HiddenSkippedTemplate, hiddenSkippedCount));

        if (decision.Permitted.Count == 0)
        {
            return SkillResult.Error(string.Format(CultureInfo.InvariantCulture, NothingPermittedMessage,
                string.Join(ListSeparator, decision.Skipped.Select(item => item.Reason).Distinct())));
        }

        var addsMembers = decision.Permitted.Any(proposal => proposal.Kind is not GroupingProposalKind.RemoveClient);
        var createsGroup = decision.Permitted.Any(proposal => proposal.Kind == GroupingProposalKind.CreateGroup);
        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var rawValidFrom = GetParameter<string>(parameters, ValidFromParameter);
        var (validFrom, invalidDate) = SkillDateParser.ParseOptionalUtcDate(rawValidFrom, today, context.UserLanguage);
        if (invalidDate)
        {
            return SkillResult.Error(SkillDateParser.InvalidDateMessageFor(ValidFromParameter, rawValidFrom!));
        }

        var newGroupName = GetParameter<string>(parameters, NewGroupNameParameter)?.Trim();
        var periodStart = report.Request.From.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var effectiveValidFrom = validFrom ?? periodStart;
        var startDateNote = addsMembers
            ? string.Format(
                CultureInfo.InvariantCulture,
                validFrom is null ? StartDateDefaultedTemplate : StartDateGivenTemplate,
                effectiveValidFrom)
            : string.Empty;
        var data = new { Applied = false, Changes = steps, Skipped = skipped };

        if (!apply)
        {
            var askName = createsGroup && string.IsNullOrWhiteSpace(newGroupName) ? AskForGroupNameInPreview : string.Empty;
            _previewRegistry.RecordPreview(context.UserId, report.Fingerprint, context.TurnId);
            return SkillResult.SuccessResult(data, string.Format(
                CultureInfo.InvariantCulture, PreviewTemplate, steps.Count,
                string.Join(ListSeparator, steps.Select(step => $"{step.Step}. {step.Action}: {step.Group} {step.Client ?? step.Shift}".TrimEnd())),
                skippedText, startDateNote, askName));
        }

        if (createsGroup && string.IsNullOrWhiteSpace(newGroupName))
        {
            return SkillResult.Error(MissingGroupNameMessage);
        }

        GroupingApplyResult result;
        try
        {
            result = await _applier.ApplyAsync(
                new GroupingApplyCommand(decision.Permitted, effectiveValidFrom, newGroupName, context.UserName),
                cancellationToken);
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        _snapshotStore.Remove(GroupingFeasibilityDay.KeyFor(DateOnly.FromDateTime(today)));
        _previewRegistry.Forget(context.UserId, report.Fingerprint);

        var remaining = await RecomputeAsync(report.Request, scope, cancellationToken);
        return SkillResult.SuccessResult(
            new { Applied = true, Result = result, Skipped = skipped, Remaining = remaining?.PlanningUnits },
            string.Format(CultureInfo.InvariantCulture, AppliedTemplate,
                result.CreatedGroups, result.AddedShifts, result.AddedClients, result.RemovedClients,
                result.AlreadyInPlace, skippedText, startDateNote)
            + (remaining is null ? GroupingReportTexts.RecomputeFailedSentence : GroupingReportTexts.RemainingGaps(remaining)));
    }

    private async Task<GroupingReportView?> RecomputeAsync(
        GroupingAnalysisRequest request, GroupScopeAccess scope, CancellationToken cancellationToken)
    {
        try
        {
            var recomputed = await _analyzer.AnalyzeAsync(request, cancellationToken);
            return GroupingReportViewBuilder.Build(recomputed, scope, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Recomputing the grouping feasibility after a successful apply failed");
            return null;
        }
    }
}
