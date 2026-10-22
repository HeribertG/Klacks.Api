// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Surfaces schedule rule violations for a group + period so Klacksy can JUDGE a plan, not just
/// read it: rest-time, daily/weekly overtime, consecutive days, min rest days and collisions.
/// Reuses the same engine as the period-closing validator and the pre-commit guardrail. When an
/// analyseToken is supplied the isolated scenario is checked instead of the real plan.
/// Bounded by the caller's group scope like the schedule grid, which shows a planner the validations of visible
/// clients only: a group outside the scope is answered exactly like a missing one, and findings of clients the
/// caller may not see are dropped.
/// </summary>
/// <param name="groupId">Required. UUID of the group / planning blade.</param>
/// <param name="fromDate">Required. ISO date yyyy-MM-dd (period start).</param>
/// <param name="untilDate">Required. ISO date yyyy-MM-dd (period end, inclusive).</param>
/// <param name="analyseToken">Optional. UUID of a scenario; when set the isolated scenario is validated instead of the real plan.</param>
/// <param name="validationLoader">Replays the schedule validator over the period</param>
/// <param name="groupRepository">Loads the requested group for the scope check</param>
/// <param name="groupScopeGuard">Resolves the executing user's group scope</param>
/// <param name="clientVisibilityGuard">Keeps only the findings of clients the caller may see</param>

using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.PeriodClosing;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("detect_conflicts")]
public class DetectConflictsSkill : BaseSkillImplementation
{
    private readonly IPeriodValidationLoader _validationLoader;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;

    public DetectConflictsSkill(
        IPeriodValidationLoader validationLoader,
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        IClientVisibilityGuard clientVisibilityGuard)
    {
        _validationLoader = validationLoader;
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _clientVisibilityGuard = clientVisibilityGuard;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupId = GetRequiredGuid(parameters, "groupId");
        var fromStr = GetRequiredString(parameters, "fromDate");
        var untilStr = GetRequiredString(parameters, "untilDate");

        if (!DateOnly.TryParse(fromStr, out var fromDate))
        {
            return SkillResult.Error($"Invalid fromDate: {fromStr}.");
        }
        if (!DateOnly.TryParse(untilStr, out var untilDate))
        {
            return SkillResult.Error($"Invalid untilDate: {untilStr}.");
        }
        if (untilDate < fromDate)
        {
            return SkillResult.Error("untilDate must be on or after fromDate.");
        }

        if (!ScenarioScopeParameter.TryRead(parameters, out var analyseToken, out var scopeError))
        {
            return SkillResult.Error(scopeError!);
        }

        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        if (!scope.IsUnrestricted)
        {
            var group = await _groupRepository.Get(groupId);
            if (group == null || !scope.IsInScope(group))
            {
                return SkillResult.Error($"Group with ID '{groupId}' not found.");
            }
        }

        var loadedIssues = await _validationLoader.LoadAsync(
            fromDate, untilDate, groupId, analyseToken, cancellationToken: cancellationToken);
        var issues = scope.IsUnrestricted
            ? loadedIssues
            : await KeepVisibleClientsAsync(loadedIssues, cancellationToken);

        var conflicts = issues.Select(i => new
        {
            i.ClientId,
            i.ClientName,
            Date = i.Date.ToString(DateTimeFormats.IsoDate),
            Severity = i.Severity.ToString(),
            i.Code,
            i.MessageKey,
            Params = LocalizedCommentParams.ForLanguage(i.MessageParams, context.UserLanguage)
        }).ToList();

        var errors = issues.Count(i => i.Severity == ScheduleValidationType.Error);
        var warnings = issues.Count(i => i.Severity == ScheduleValidationType.Warning);
        var info = issues.Count(i => i.Severity == ScheduleValidationType.Info);
        var byCode = issues
            .GroupBy(i => i.Code)
            .ToDictionary(g => g.Key, g => g.Count());

        var data = new
        {
            GroupId = groupId,
            FromDate = fromDate.ToString(DateTimeFormats.IsoDate),
            UntilDate = untilDate.ToString(DateTimeFormats.IsoDate),
            AnalyseToken = analyseToken,
            IsScenario = analyseToken.HasValue,
            TotalConflicts = issues.Count,
            Errors = errors,
            Warnings = warnings,
            Info = info,
            ByCode = byCode,
            Conflicts = conflicts
        };

        var scenarioNote = analyseToken.HasValue ? " (scenario view)" : string.Empty;
        var message = issues.Count == 0
            ? $"No conflicts for group {groupId} between {fromDate:yyyy-MM-dd} and {untilDate:yyyy-MM-dd}{scenarioNote}."
            : $"{issues.Count} conflict(s) for group {groupId} between {fromDate:yyyy-MM-dd} and {untilDate:yyyy-MM-dd}{scenarioNote}: " +
              $"{errors} error(s), {warnings} warning(s).";

        return SkillResult.SuccessResult(data, message);
    }

    private async Task<List<PeriodIssueDto>> KeepVisibleClientsAsync(
        IReadOnlyCollection<PeriodIssueDto> issues, CancellationToken cancellationToken)
    {
        var visibleClientIds = (await _clientVisibilityGuard.FilterVisibleAsync(
                issues.Where(i => i.ClientId != Guid.Empty).ToList(), i => i.ClientId, cancellationToken))
            .Select(i => i.ClientId)
            .ToHashSet();

        return issues
            .Where(i => i.ClientId == Guid.Empty || visibleClientIds.Contains(i.ClientId))
            .ToList();
    }
}
