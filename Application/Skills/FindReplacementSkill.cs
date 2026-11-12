// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Proposes rule-compliant replacement employees for a shift on a given day, ranked best-first.
/// Thin wrapper that dispatches <see cref="Klacks.Api.Application.Queries.Schedules.FindReplacementQuery"/>: it resolves the shift (for its start/end
/// times) and projects the ranked candidates + exclusions. The output is cut for the model: the first
/// MaxListedCandidates eligible candidates in the order the handler ranked them, each with at most
/// MaxListedSoftConflictsPerCandidate soft findings, Error-level ones first, and the full number in SoftConflictCount; the excluded as
/// numbers per reason, split into the own group and borrowed people, and the first MaxNamedExcluded by name, own
/// group first. EligibleCount and ExcludedCount stay complete, and the message says how many were left out. The
/// handler, the planner endpoint and the dialog are not cut. A candidate on call that day (an on-call
/// absence) ranks first and is flagged IsOnCall. A candidate is hard-excluded when absent
/// that day (any other Break on the date), when explicitly unavailable for an hour the shift occupies (an
/// opt-in availability window), when assigning them would introduce a collision or rest-time
/// violation, when they lack a mandatory qualification the shift requires (missing / expired / below
/// a BINDING minimum level), or when the shift is blacklisted for them; aggregate findings lower the rank
/// instead, and a candidate below a non-binding minimum level ranks after the sufficiently qualified ones
/// (LevelShortfall = missing levels, with the qualification-level warning in SoftConflicts). Among equally clean candidates the one furthest below their period target hours ranks
/// higher (fairness; TargetHoursDeficit is surfaced per candidate).
/// A shift outside the caller's group visibility is answered exactly like a missing shift: a shift is visible
/// when it belongs to no group at all or to at least one group the caller may see (the plan-view rule), so its
/// name never leaks. The candidate pool itself is filtered by the query handler.
/// </summary>
/// <param name="shiftId">Required. UUID of the shift to fill.</param>
/// <param name="date">Required. Workday in ISO yyyy-MM-dd.</param>
/// <param name="groupId">Required. UUID of the group that needs the replacement; its members and the borrowable people under its root are the candidate pool.</param>
/// <param name="analyseToken">Optional. UUID of a scenario; when set, candidates are checked against the isolated scenario.</param>
/// <param name="overrideBlock">Optional. K1 supervisor override for a Block-mode compliance escalation (e.g. an emergency); default false.</param>

using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("find_replacement")]
public class FindReplacementSkill : BaseSkillImplementation
{
    private const int MaxListedCandidates = 10;
    private const int MaxListedSoftConflictsPerCandidate = 3;
    private const int MaxNamedExcluded = 10;

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

        if (!ScenarioScopeParameter.TryRead(parameters, out var analyseToken, out var scopeError))
        {
            return SkillResult.Error(scopeError!);
        }
        var overrideBlock = GetParameter<bool?>(parameters, "overrideBlock") ?? false;

        var shift = await _shiftRepository.Get(shiftId);
        if (shift == null || !await ShiftGroupVisibility.IsVisibleAsync(shift, _groupVisibilityGuard, cancellationToken))
        {
            return SkillResult.Error($"Shift {shiftId} not found.");
        }

        var result = await _mediator.Send(
            new FindReplacementQuery(shiftId, date, shift.StartShift, shift.EndShift, groupId, analyseToken, overrideBlock),
            cancellationToken);

        var listed = result.Eligible
            .Take(MaxListedCandidates)
            .Select(c => new
            {
                c.ClientId,
                c.Name,
                c.IsOnCall,
                Borrowed = c.IsBorrowed,
                ContractDayOff = c.IsContractDayOff,
                c.IsPreferred,
                c.TargetHoursDeficit,
                c.LevelShortfall,
                SoftConflictCount = c.SoftConflicts.Count,
                SoftConflicts = c.SoftConflicts
                    .OrderByDescending(conflict => conflict.Type == ScheduleValidationType.Error)
                    .Take(MaxListedSoftConflictsPerCandidate)
                    .Select(conflict => Project(conflict, context.UserLanguage))
                    .ToList()
            })
            .ToList();

        var excludedByReason = result.Excluded
            .GroupBy(e => e.Reason)
            .Select(g => new
            {
                Reason = g.Key,
                OwnGroup = g.Count(e => !e.IsBorrowed),
                Borrowed = g.Count(e => e.IsBorrowed)
            })
            .OrderByDescending(r => r.OwnGroup + r.Borrowed)
            .ThenBy(r => r.Reason, StringComparer.Ordinal)
            .ToList();

        var namedExcluded = result.Excluded
            .OrderBy(e => e.IsBorrowed)
            .Take(MaxNamedExcluded)
            .Select(e => new { e.ClientId, e.Name, e.Reason, Borrowed = e.IsBorrowed })
            .ToList();

        var unlistedEligibleCount = result.Eligible.Count - listed.Count;
        var unnamedExcludedCount = result.Excluded.Count - namedExcluded.Count;

        var data = new
        {
            ShiftId = shiftId,
            ShiftName = shift.Name,
            Date = date.ToString(DateTimeFormats.IsoDate),
            GroupId = groupId,
            IsScenario = analyseToken.HasValue,
            EligibleCount = result.Eligible.Count,
            ExcludedCount = result.Excluded.Count,
            UnlistedEligibleCount = unlistedEligibleCount,
            Candidates = listed,
            ExcludedByReason = excludedByReason,
            UnnamedExcludedCount = unnamedExcludedCount,
            Excluded = namedExcluded
        };

        var borrowedCount = result.Eligible.Count(c => c.IsBorrowed);
        var dayOffCount = result.Eligible.Count(c => c.IsContractDayOff);

        var scenarioNote = analyseToken.HasValue ? " (scenario)" : string.Empty;
        var message =
            $"{result.Eligible.Count} eligible replacement(s) for shift '{shift.Name}' on {date:yyyy-MM-dd}{scenarioNote}; " +
            $"{result.Excluded.Count} excluded (absence / unavailable / collision / rest time / missing qualification / blacklist). " +
            $"{borrowedCount} of the eligible are borrowed from another group under the same root (Borrowed, ranked after the own group); " +
            $"{dayOffCount} have that weekday off by contract (ContractDayOff: marked, ask them first)." +
            ShortenedNote(listed.Count, unlistedEligibleCount, namedExcluded.Count, unnamedExcludedCount);

        return SkillResult.SuccessResult(data, message);
    }

    private static string ShortenedNote(int listedEligible, int unlistedEligible, int namedExcluded, int unnamedExcluded)
    {
        var note = string.Empty;
        if (unlistedEligible > 0)
        {
            note += $" The list is shortened: only the best {listedEligible} eligible are listed, {unlistedEligible} more are eligible " +
                "(the Cover absence dialog shows all of them).";
        }

        if (unnamedExcluded > 0)
        {
            note += $" Only {namedExcluded} of the excluded are named, own group first; {unnamedExcluded} more are counted per reason in ExcludedByReason.";
        }

        return note;
    }

    private static object Project(
        Klacks.Api.Application.DTOs.Notifications.ScheduleValidationNotificationDto conflict,
        string? language)
        => new
        {
            Severity = conflict.Type.ToString(),
            conflict.Comment,
            Date = conflict.Date.ToString(DateTimeFormats.IsoDate),
            CommentParams = LocalizedCommentParams.ForLanguage(conflict.CommentParams, language)
        };
}
