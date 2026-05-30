// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Surfaces schedule rule violations for a group + period so Klacksy can JUDGE a plan, not just
/// read it: rest-time, daily/weekly overtime, consecutive days, min rest days and collisions.
/// Reuses the same engine as the period-closing validator and the pre-commit guardrail. When an
/// analyseToken is supplied the isolated scenario is checked instead of the real plan.
/// </summary>
/// <param name="groupId">Required. UUID of the group / planning blade.</param>
/// <param name="fromDate">Required. ISO date yyyy-MM-dd (period start).</param>
/// <param name="untilDate">Required. ISO date yyyy-MM-dd (period end, inclusive).</param>
/// <param name="analyseToken">Optional. UUID of a scenario; when set the isolated scenario is validated instead of the real plan.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.PeriodClosing;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("detect_conflicts")]
public class DetectConflictsSkill : BaseSkillImplementation
{
    private readonly IPeriodValidationLoader _validationLoader;

    public DetectConflictsSkill(IPeriodValidationLoader validationLoader)
    {
        _validationLoader = validationLoader;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupId = GetRequiredGuid(parameters, "groupId");
        var fromStr = GetRequiredString(parameters, "fromDate");
        var untilStr = GetRequiredString(parameters, "untilDate");
        var analyseTokenStr = GetParameter<string>(parameters, "analyseToken");

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

        Guid? analyseToken = null;
        if (!string.IsNullOrWhiteSpace(analyseTokenStr))
        {
            if (!Guid.TryParse(analyseTokenStr, out var parsedToken))
            {
                return SkillResult.Error($"Invalid analyseToken: {analyseTokenStr}.");
            }
            analyseToken = parsedToken;
        }

        var issues = await _validationLoader.LoadAsync(
            fromDate, untilDate, groupId, analyseToken, cancellationToken);

        var conflicts = issues.Select(i => new
        {
            i.ClientId,
            i.ClientName,
            Date = i.Date.ToString("yyyy-MM-dd"),
            Severity = i.Severity.ToString(),
            i.Code,
            i.MessageKey,
            Params = i.MessageParams
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
            FromDate = fromDate.ToString("yyyy-MM-dd"),
            UntilDate = untilDate.ToString("yyyy-MM-dd"),
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
}
