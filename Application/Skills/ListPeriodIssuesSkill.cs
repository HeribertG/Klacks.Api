// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the validation findings of a billing period (the issues card of the period-closing
/// page): errors, warnings and notes per day and employee — the pre-flight check to run
/// BEFORE close_period. Optionally group-scoped.
/// </summary>
/// <param name="startDate">Period start in ISO yyyy-MM-dd (inclusive).</param>
/// <param name="endDate">Period end in ISO yyyy-MM-dd (inclusive).</param>
/// <param name="groupId">Optional. UUID of the group scope; omitted = all clients.</param>
/// <param name="groupName">Optional. Display name of the group; resolved with fuzzy matching.</param>
/// <param name="limit">Optional. Maximum number of findings to return (default 50).</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.PeriodClosing;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_period_issues")]
public class ListPeriodIssuesSkill : BaseSkillImplementation
{
    private const int DefaultLimit = 50;

    private readonly IMediator _mediator;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;

    public ListPeriodIssuesSkill(
        IMediator mediator,
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard)
    {
        _mediator = mediator;
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var startDate = GetParameter<DateOnly?>(parameters, "startDate")
            ?? throw new ArgumentException("Required parameter 'startDate' is missing");
        var endDate = GetParameter<DateOnly?>(parameters, "endDate")
            ?? throw new ArgumentException("Required parameter 'endDate' is missing");
        if (startDate > endDate)
        {
            return SkillResult.Error($"startDate ({startDate}) must be on or before endDate ({endDate}).");
        }

        var (groupId, groupName, groupError) = await PeriodClosingGroupResolver.ResolveAsync(
            GetParameter<string>(parameters, "groupId"),
            GetParameter<string>(parameters, "groupName"),
            _groupRepository, _groupScopeGuard, context, cancellationToken);
        if (groupError != null)
        {
            return SkillResult.Error(groupError);
        }

        var limit = GetParameter<int?>(parameters, "limit") ?? DefaultLimit;
        if (limit < 1)
        {
            limit = DefaultLimit;
        }

        var issues = await _mediator.Send(
            new GetPeriodIssuesQuery(startDate, endDate, groupId), cancellationToken);

        var bySeverity = issues
            .GroupBy(i => i.Severity.ToString())
            .ToDictionary(g => g.Key, g => g.Count());

        var listed = issues
            .OrderByDescending(i => i.Severity)
            .ThenBy(i => i.Date)
            .Take(limit)
            .Select(i => new
            {
                Date = i.Date.ToString("yyyy-MM-dd"),
                i.ClientId,
                i.ClientName,
                Severity = i.Severity.ToString(),
                i.Code,
                i.MessageKey,
                i.MessageParams
            })
            .ToList();

        var scopeLabel = groupId.HasValue ? $" for group '{groupName}'" : string.Empty;
        var severitySummary = bySeverity.Count > 0
            ? string.Join(", ", bySeverity.Select(kv => $"{kv.Value} {kv.Key}"))
            : "none";
        var truncatedNote = issues.Count > limit
            ? $" Showing the first {limit} of {issues.Count} findings."
            : string.Empty;
        var verdict = issues.Count == 0
            ? " The period is clean — sealing is safe from a validation point of view."
            : " Resolve or consciously accept these findings before sealing with close_period.";

        return SkillResult.SuccessResult(
            new
            {
                StartDate = startDate,
                EndDate = endDate,
                GroupId = groupId,
                GroupName = groupName,
                TotalFindings = issues.Count,
                BySeverity = bySeverity,
                Findings = listed
            },
            $"Period {startDate}..{endDate}{scopeLabel}: {issues.Count} validation finding(s) ({severitySummary})." +
            truncatedNote + verdict);
    }
}
