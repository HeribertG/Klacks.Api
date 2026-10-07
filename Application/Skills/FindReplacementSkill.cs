// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Proposes rule-compliant replacement employees for a shift on a given day, ranked best-first.
/// Thin wrapper that dispatches <see cref="Klacks.Api.Application.Queries.Schedules.FindReplacementQuery"/>: it resolves the shift (for its start/end
/// times) and projects the ranked candidates + exclusions. A candidate on call that day (an on-call
/// absence) ranks first and is flagged IsOnCall. A candidate is hard-excluded when absent
/// that day (any other Break on the date), when explicitly unavailable for an hour the shift occupies (an
/// opt-in availability window), when assigning them would introduce a collision or rest-time
/// violation, when they lack a mandatory qualification the shift requires (missing / expired / below
/// the required level), or when the shift is blacklisted for them; aggregate findings lower the rank
/// instead. Among equally clean candidates the one furthest below their period target hours ranks
/// higher (fairness; TargetHoursDeficit is surfaced per candidate).
/// A shift outside the caller's group visibility is answered exactly like a missing shift: a shift is visible
/// when it belongs to no group at all or to at least one group the caller may see (the plan-view rule), so its
/// name never leaks. The candidate pool itself is filtered by the query handler.
/// </summary>
/// <param name="shiftId">Required. UUID of the shift to fill.</param>
/// <param name="date">Required. Workday in ISO yyyy-MM-dd.</param>
/// <param name="groupId">Required. UUID of the group whose members are the candidate pool.</param>
/// <param name="analyseToken">Optional. UUID of a scenario; when set, candidates are checked against the isolated scenario.</param>
/// <param name="overrideBlock">Optional. K1 supervisor override for a Block-mode compliance escalation (e.g. an emergency); default false.</param>

using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("find_replacement")]
public class FindReplacementSkill : BaseSkillImplementation
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMediator _mediator;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;

    public FindReplacementSkill(
        IShiftRepository shiftRepository,
        IMediator mediator,
        IGroupVisibilityGuard groupVisibilityGuard)
    {
        _shiftRepository = shiftRepository;
        _mediator = mediator;
        _groupVisibilityGuard = groupVisibilityGuard;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var shiftId = GetRequiredGuid(parameters, "shiftId");
        var date = GetParameter<DateOnly?>(parameters, "date")
            ?? throw new ArgumentException("Required parameter 'date' is missing");
        var groupId = GetRequiredGuid(parameters, "groupId");
        var analyseTokenRaw = GetParameter<string>(parameters, "analyseToken");

        Guid? analyseToken = null;
        if (!string.IsNullOrWhiteSpace(analyseTokenRaw))
        {
            if (!Guid.TryParse(analyseTokenRaw, out var parsedToken))
            {
                return SkillResult.Error($"Invalid analyseToken: {analyseTokenRaw}.");
            }
            analyseToken = parsedToken;
        }
        var overrideBlock = GetParameter<bool?>(parameters, "overrideBlock") ?? false;

        var shift = await _shiftRepository.Get(shiftId);
        if (shift == null || !await IsShiftVisibleAsync(shift, cancellationToken))
        {
            return SkillResult.Error($"Shift {shiftId} not found.");
        }

        var result = await _mediator.Send(
            new FindReplacementQuery(shiftId, date, shift.StartShift, shift.EndShift, groupId, analyseToken, overrideBlock),
            cancellationToken);

        var ranked = result.Eligible.Select(c => new
        {
            c.ClientId,
            c.Name,
            c.IsOnCall,
            c.IsPreferred,
            c.TargetHoursDeficit,
            SoftConflictCount = c.SoftConflicts.Count,
            SoftConflicts = c.SoftConflicts.Select(conflict => Project(conflict, context.UserLanguage))
        });

        var data = new
        {
            ShiftId = shiftId,
            ShiftName = shift.Name,
            Date = date.ToString("yyyy-MM-dd"),
            GroupId = groupId,
            IsScenario = analyseToken.HasValue,
            EligibleCount = result.Eligible.Count,
            ExcludedCount = result.Excluded.Count,
            Candidates = ranked,
            Excluded = result.Excluded.Select(e => new { e.ClientId, e.Name, e.Reason })
        };

        var scenarioNote = analyseToken.HasValue ? " (scenario)" : string.Empty;
        var message =
            $"{result.Eligible.Count} eligible replacement(s) for shift '{shift.Name}' on {date:yyyy-MM-dd}{scenarioNote}; " +
            $"{result.Excluded.Count} excluded (absence / unavailable / collision / rest time / missing qualification / blacklist).";

        return SkillResult.SuccessResult(data, message);
    }

    private async Task<bool> IsShiftVisibleAsync(Shift shift, CancellationToken cancellationToken)
    {
        var groupIds = shift.GroupItems
            .Where(item => !item.IsDeleted)
            .Select(item => item.GroupId)
            .Distinct()
            .ToList();
        if (groupIds.Count == 0 || await _groupVisibilityGuard.IsUnrestrictedAsync(cancellationToken))
        {
            return true;
        }

        foreach (var groupId in groupIds)
        {
            if (await _groupVisibilityGuard.IsGroupVisibleAsync(groupId, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static object Project(
        Klacks.Api.Application.DTOs.Notifications.ScheduleValidationNotificationDto conflict,
        string? language)
        => new
        {
            Severity = conflict.Type.ToString(),
            conflict.Comment,
            Date = conflict.Date.ToString("yyyy-MM-dd"),
            CommentParams = LocalizedCommentParams.ForLanguage(conflict.CommentParams, language)
        };
}
