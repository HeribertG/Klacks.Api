// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Proposes rule-compliant replacement employees for a shift on a given day, ranked best-first.
/// Thin wrapper that dispatches <see cref="Klacks.Api.Application.Queries.Schedules.FindReplacementQuery"/>: it resolves the shift (for its start/end
/// times) and projects the ranked candidates + exclusions. A candidate is hard-excluded when absent
/// that day (a Break on the date), when assigning them would introduce a collision or rest-time
/// violation, when they lack a mandatory qualification the shift requires (missing / expired / below
/// the required level), or when the shift is blacklisted for them; aggregate findings lower the rank
/// instead. Richer availability windows (P5) are not yet considered.
/// </summary>
/// <param name="shiftId">Required. UUID of the shift to fill.</param>
/// <param name="date">Required. Workday in ISO yyyy-MM-dd.</param>
/// <param name="groupId">Required. UUID of the group whose members are the candidate pool.</param>
/// <param name="analyseToken">Optional. UUID of a scenario; when set, candidates are checked against the isolated scenario.</param>

using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("find_replacement")]
public class FindReplacementSkill : BaseSkillImplementation
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMediator _mediator;

    public FindReplacementSkill(
        IShiftRepository shiftRepository,
        IMediator mediator)
    {
        _shiftRepository = shiftRepository;
        _mediator = mediator;
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

        var shift = await _shiftRepository.Get(shiftId);
        if (shift == null)
        {
            return SkillResult.Error($"Shift {shiftId} not found.");
        }

        var result = await _mediator.Send(
            new FindReplacementQuery(shiftId, date, shift.StartShift, shift.EndShift, groupId, analyseToken),
            cancellationToken);

        var ranked = result.Eligible.Select(c => new
        {
            c.ClientId,
            c.Name,
            c.IsPreferred,
            SoftConflictCount = c.SoftConflicts.Count,
            SoftConflicts = c.SoftConflicts.Select(Project)
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
            $"{result.Excluded.Count} excluded (absence / collision / rest time / missing qualification / blacklist).";

        return SkillResult.SuccessResult(data, message);
    }

    private static object Project(Klacks.Api.Application.DTOs.Notifications.ScheduleValidationNotificationDto conflict)
        => new
        {
            Severity = conflict.Type.ToString(),
            conflict.Comment,
            Date = conflict.Date.ToString("yyyy-MM-dd"),
            conflict.CommentParams
        };
}
