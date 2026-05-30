// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IFindReplacementService"/>. Candidates are the active members of the group; a
/// candidate is hard-excluded when they are absent that day (a Break on the date), when assigning them
/// would introduce a new collision or rest-time violation (the precise pair-based L1 checks), or when
/// the shift is blacklisted for them. Aggregate findings (daily/weekly overtime, consecutive days, min
/// rest days) are a SOFT signal — fewer of them means more rule headroom, so they lower the rank rather
/// than exclude. Preferred employees rank first.
/// </summary>
/// <param name="clientRepository">Resolves the group's active members (the candidate pool)</param>
/// <param name="conflictChecker">Pre-commit guardrail used to test each candidate placement</param>
/// <param name="preferenceRepository">Preferred/blacklisted shift preferences</param>
/// <param name="scheduleEntriesService">Reads the day's grid to find absent (Break) members</param>
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class FindReplacementService : IFindReplacementService
{
    private const string CollisionKey = "schedule.error-list.collision";
    private const string RestViolationKey = "schedule.error-list.rest-violation";
    private const string BlacklistedReason = "blacklisted";
    private const string AbsentReason = "absent";

    private readonly IClientRepository _clientRepository;
    private readonly IPreCommitConflictChecker _conflictChecker;
    private readonly IClientShiftPreferenceRepository _preferenceRepository;
    private readonly IScheduleEntriesService _scheduleEntriesService;

    public FindReplacementService(
        IClientRepository clientRepository,
        IPreCommitConflictChecker conflictChecker,
        IClientShiftPreferenceRepository preferenceRepository,
        IScheduleEntriesService scheduleEntriesService)
    {
        _clientRepository = clientRepository;
        _conflictChecker = conflictChecker;
        _preferenceRepository = preferenceRepository;
        _scheduleEntriesService = scheduleEntriesService;
    }

    public async Task<ReplacementSearchResult> FindAsync(
        Guid shiftId,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        Guid groupId,
        Guid? analyseToken,
        CancellationToken cancellationToken = default)
    {
        var members = await _clientRepository.GetActiveClientsWithAddressesForGroupsAsync(
            new List<Guid> { groupId }, cancellationToken);
        if (members.Count == 0)
        {
            return new ReplacementSearchResult([], []);
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
            .Select(m => new PlannedWorkRow(m.Id, date, startTime, endTime, shiftId))
            .ToList();
        var check = await _conflictChecker.CheckAsync(plannedRows, analyseToken, cancellationToken);
        var conflictsByClient = check.NewConflicts
            .GroupBy(c => c.ClientId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var eligible = new List<ReplacementCandidate>();
        var excluded = new List<ExcludedCandidate>();

        foreach (var member in members)
        {
            var name = DisplayName(member);

            if (onLeaveClientIds.Contains(member.Id))
            {
                excluded.Add(new ExcludedCandidate(member.Id, name, AbsentReason));
                continue;
            }

            var conflicts = conflictsByClient.TryGetValue(member.Id, out var found)
                ? found
                : new List<ScheduleValidationNotificationDto>();

            var hardConflict = conflicts.FirstOrDefault(c => c.Comment is CollisionKey or RestViolationKey);
            if (hardConflict != null)
            {
                excluded.Add(new ExcludedCandidate(member.Id, name, hardConflict.Comment));
                continue;
            }

            // ShiftPreferenceType.Preferred is 0 (the enum default), so the bool result MUST gate the
            // lookup — otherwise a miss leaves preferenceType = Preferred and every member without a
            // preference would be treated as preferred.
            var hasPreference = preferenceByClient.TryGetValue(member.Id, out var preferenceType);
            if (hasPreference && preferenceType == ShiftPreferenceType.Blacklist)
            {
                excluded.Add(new ExcludedCandidate(member.Id, name, BlacklistedReason));
                continue;
            }

            eligible.Add(new ReplacementCandidate(
                member.Id,
                name,
                IsPreferred: hasPreference && preferenceType == ShiftPreferenceType.Preferred,
                SoftConflicts: conflicts));
        }

        var ranked = eligible
            .OrderByDescending(c => c.IsPreferred)
            .ThenBy(c => c.SoftConflicts.Count)
            .ThenBy(c => c.Name)
            .ToList();

        return new ReplacementSearchResult(ranked, excluded);
    }

    private static string DisplayName(Client client)
        => string.IsNullOrWhiteSpace(client.FirstName)
            ? client.Name
            : $"{client.FirstName} {client.Name}";
}
