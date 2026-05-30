// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Proposes rule-compliant replacement employees for a shift on a given day, ranked best-first.
/// v1 scope: candidates are the active members of the group; a candidate is HARD-excluded only when
/// placing them would introduce a new collision or rest-time violation (the precise, pair-based L1
/// checks) or when the shift is blacklisted for them. Aggregate findings (daily/weekly overtime,
/// consecutive days, min rest days) are treated as a SOFT signal — fewer of them means more rule
/// headroom, so they push a candidate down the ranking rather than out (playbook step 3a). Preferred
/// shift assignments are ranked first. Candidates who are absent that day (a Break on the date) are
/// excluded as on-leave; the richer availability windows (ClientAvailability) are deferred to P5.
/// TODO v2 (P4): qualification/eligibility matching (L3) — required-qualification @ date + min level.
/// TODO v2: target-hours deficit and fairness in the ranking (playbook step 3b/3c).
/// TODO P5: availability windows / preferences beyond hard absence (set_client_availability).
/// </summary>
/// <param name="shiftId">Required. UUID of the shift to fill.</param>
/// <param name="date">Required. Workday in ISO yyyy-MM-dd.</param>
/// <param name="groupId">Required. UUID of the group whose members are the candidate pool.</param>
/// <param name="analyseToken">Optional. UUID of a scenario; when set, candidates are checked against the isolated scenario.</param>

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("find_replacement")]
public class FindReplacementSkill : BaseSkillImplementation
{
    private const string CollisionKey = "schedule.error-list.collision";
    private const string RestViolationKey = "schedule.error-list.rest-violation";
    private const string BlacklistedReason = "blacklisted";
    private const string AbsentReason = "absent";

    private readonly IShiftRepository _shiftRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IPreCommitConflictChecker _conflictChecker;
    private readonly IClientShiftPreferenceRepository _preferenceRepository;
    private readonly IScheduleEntriesService _scheduleEntriesService;

    public FindReplacementSkill(
        IShiftRepository shiftRepository,
        IClientRepository clientRepository,
        IPreCommitConflictChecker conflictChecker,
        IClientShiftPreferenceRepository preferenceRepository,
        IScheduleEntriesService scheduleEntriesService)
    {
        _shiftRepository = shiftRepository;
        _clientRepository = clientRepository;
        _conflictChecker = conflictChecker;
        _preferenceRepository = preferenceRepository;
        _scheduleEntriesService = scheduleEntriesService;
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

        var members = await _clientRepository.GetActiveClientsWithAddressesForGroupsAsync(
            new List<Guid> { groupId }, cancellationToken);
        if (members.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { ShiftId = shiftId, GroupId = groupId, EligibleCount = 0, Candidates = Array.Empty<object>() },
                $"No active members in group {groupId} to consider for shift '{shift.Name}'.");
        }

        var onLeaveCells = await _scheduleEntriesService
            .GetScheduleEntriesQuery(date, date, new List<Guid> { groupId }, analyseToken)
            .Where(c => c.EntryType == (int)ScheduleEntryType.Break)
            .ToListAsync(cancellationToken);
        var onLeaveClientIds = onLeaveCells.Select(c => c.ClientId).ToHashSet();

        var preferences = await _preferenceRepository.GetByShiftIdAsync(shiftId, cancellationToken);
        var preferenceByClient = preferences
            .GroupBy(p => p.ClientId)
            .ToDictionary(g => g.Key, g => g.First().PreferenceType);

        var plannedRows = members
            .Select(m => new PlannedWorkRow(m.Id, date, shift.StartShift, shift.EndShift, shiftId))
            .ToList();
        var check = await _conflictChecker.CheckAsync(plannedRows, analyseToken, cancellationToken);
        var conflictsByClient = check.NewConflicts
            .GroupBy(c => c.ClientId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var eligible = new List<CandidateScore>();
        var excluded = new List<object>();

        foreach (var member in members)
        {
            var name = DisplayName(member);

            if (onLeaveClientIds.Contains(member.Id))
            {
                excluded.Add(new { ClientId = member.Id, Name = name, Reason = AbsentReason });
                continue;
            }

            var conflicts = conflictsByClient.TryGetValue(member.Id, out var found)
                ? found
                : new List<ScheduleValidationNotificationDto>();

            var hardConflicts = conflicts
                .Where(c => c.Comment is CollisionKey or RestViolationKey)
                .ToList();
            if (hardConflicts.Count > 0)
            {
                excluded.Add(new
                {
                    ClientId = member.Id,
                    Name = name,
                    Reason = hardConflicts[0].Comment,
                    Conflicts = Project(hardConflicts)
                });
                continue;
            }

            // ShiftPreferenceType.Preferred is 0 (the enum default), so the bool result MUST gate the
            // lookup — otherwise a TryGetValue miss leaves preferenceType = Preferred and every member
            // without an explicit preference would be treated as preferred.
            var hasPreference = preferenceByClient.TryGetValue(member.Id, out var preferenceType);
            if (hasPreference && preferenceType == ShiftPreferenceType.Blacklist)
            {
                excluded.Add(new { ClientId = member.Id, Name = name, Reason = BlacklistedReason });
                continue;
            }

            eligible.Add(new CandidateScore(
                member.Id,
                name,
                IsPreferred: hasPreference && preferenceType == ShiftPreferenceType.Preferred,
                SoftConflicts: conflicts));
        }

        var ranked = eligible
            .OrderByDescending(c => c.IsPreferred)
            .ThenBy(c => c.SoftConflicts.Count)
            .ThenBy(c => c.Name)
            .Select(c => new
            {
                c.ClientId,
                c.Name,
                c.IsPreferred,
                SoftConflictCount = c.SoftConflicts.Count,
                SoftConflicts = Project(c.SoftConflicts)
            })
            .ToList();

        var scenarioNote = analyseToken.HasValue ? " (scenario)" : string.Empty;
        var data = new
        {
            ShiftId = shiftId,
            ShiftName = shift.Name,
            Date = date.ToString("yyyy-MM-dd"),
            GroupId = groupId,
            IsScenario = analyseToken.HasValue,
            EligibleCount = ranked.Count,
            ExcludedCount = excluded.Count,
            Candidates = ranked,
            Excluded = excluded
        };

        var message =
            $"{ranked.Count} eligible replacement(s) for shift '{shift.Name}' on {date:yyyy-MM-dd}{scenarioNote}; " +
            $"{excluded.Count} excluded (absence / collision / rest time / blacklist). " +
            "Ranking does not yet consider qualifications (v2).";

        return SkillResult.SuccessResult(data, message);
    }

    private static string DisplayName(Client client)
        => string.IsNullOrWhiteSpace(client.FirstName)
            ? client.Name
            : $"{client.FirstName} {client.Name}";

    private static List<object> Project(IEnumerable<ScheduleValidationNotificationDto> conflicts)
        => conflicts
            .Select(c => (object)new
            {
                Severity = c.Type.ToString(),
                c.Comment,
                Date = c.Date.ToString("yyyy-MM-dd"),
                c.CommentParams
            })
            .ToList();

    private sealed record CandidateScore(
        Guid ClientId,
        string Name,
        bool IsPreferred,
        IReadOnlyList<ScheduleValidationNotificationDto> SoftConflicts);
}
