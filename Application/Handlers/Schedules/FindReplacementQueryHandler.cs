// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for <see cref="FindReplacementQuery"/>. Candidates are the active members of the group; a
/// candidate is hard-excluded when absent that day (a Break on the date), when explicitly unavailable
/// for an hour the shift occupies (an opt-in availability window), when assigning them would introduce
/// ANY new Error-level conflict (collision, missing/expired/insufficient mandatory qualification, or a
/// Block-mode enforcement escalation such as restricted-time-window / period-cap / counter-rule /
/// rest-day-rotation), when a rest-time violation is found (excluding even in Warn mode), or when the
/// shift is blacklisted for them. Warning-level aggregate findings (overtime/consecutive/min-rest) are
/// a soft ranking signal (less headroom -> lower rank). Preferred employees rank first; among equally
/// clean candidates the one furthest below their period target hours ranks higher (fairness, 3b/3c).
/// </summary>
/// <param name="clientRepository">Resolves the group's active members (the candidate pool)</param>
/// <param name="conflictChecker">Pre-commit guardrail used to test each candidate placement</param>
/// <param name="preferenceRepository">Preferred/blacklisted shift preferences</param>
/// <param name="scheduleEntriesService">Reads the day's grid to find absent (Break) members</param>
/// <param name="availabilityRepository">Reads the day's hour-granular client availability windows</param>
/// <param name="periodHoursService">Resolves each candidate's period target vs. already-assigned hours (fairness signal)</param>
/// <param name="overrideAuthorizer">Resolves whether a supervisor override is allowed at all (K1)</param>
/// <param name="httpContextAccessor">Resolves the caller's name for the override audit log</param>
/// <param name="logger">Logs a structured audit entry whenever a supervisor overrides a compliance exclusion</param>
using System.Security.Claims;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Schedules;

public sealed class FindReplacementQueryHandler : IRequestHandler<FindReplacementQuery, ReplacementSearchResult>
{
    private const string BlacklistedReason = "blacklisted";
    private const string AbsentReason = "absent";
    private const string UnavailableReason = "unavailable";

    private readonly IClientRepository _clientRepository;
    private readonly IPreCommitConflictChecker _conflictChecker;
    private readonly IClientShiftPreferenceRepository _preferenceRepository;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IClientAvailabilityRepository _availabilityRepository;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly ISupervisorOverrideAuthorizer _overrideAuthorizer;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<FindReplacementQueryHandler> _logger;

    public FindReplacementQueryHandler(
        IClientRepository clientRepository,
        IPreCommitConflictChecker conflictChecker,
        IClientShiftPreferenceRepository preferenceRepository,
        IScheduleEntriesService scheduleEntriesService,
        IClientAvailabilityRepository availabilityRepository,
        IPeriodHoursService periodHoursService,
        ISupervisorOverrideAuthorizer overrideAuthorizer,
        IHttpContextAccessor httpContextAccessor,
        ILogger<FindReplacementQueryHandler> logger)
    {
        _clientRepository = clientRepository;
        _conflictChecker = conflictChecker;
        _preferenceRepository = preferenceRepository;
        _scheduleEntriesService = scheduleEntriesService;
        _availabilityRepository = availabilityRepository;
        _periodHoursService = periodHoursService;
        _overrideAuthorizer = overrideAuthorizer;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<ReplacementSearchResult> Handle(FindReplacementQuery request, CancellationToken cancellationToken)
    {
        var members = await _clientRepository.GetActiveClientsWithAddressesForGroupsAsync(
            new List<Guid> { request.GroupId }, cancellationToken);
        if (members.Count == 0)
        {
            return new ReplacementSearchResult([], []);
        }

        var onLeaveCells = await _scheduleEntriesService
            .GetScheduleEntriesQuery(request.Date, request.Date, new List<Guid> { request.GroupId }, request.AnalyseToken)
            .Where(c => c.EntryType == (int)ScheduleEntryType.Break)
            .ToListAsync(cancellationToken);
        var onLeaveClientIds = onLeaveCells.Select(c => c.ClientId).ToHashSet();

        var availabilityEntries = await _availabilityRepository.GetByDateRange(request.Date, request.Date);
        var availabilityByClient = availabilityEntries
            .GroupBy(a => a.ClientId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<ClientAvailability>)g.ToList());

        var preferences = await _preferenceRepository.GetByShiftIdAsync(request.ShiftId, cancellationToken);
        var preferenceByClient = preferences
            .GroupBy(p => p.ClientId)
            .ToDictionary(g => g.Key, g => g.First().PreferenceType);

        var (periodFrom, periodUntil) = await _periodHoursService.GetPeriodBoundariesAsync(request.Date);
        var periodHoursByClient = await _periodHoursService.GetPeriodHoursAsync(
            members.Select(m => m.Id).ToList(), periodFrom, periodUntil, request.AnalyseToken);

        var plannedRows = members
            .Select(m => new PlannedWorkRow(m.Id, request.Date, request.StartTime, request.EndTime, request.ShiftId))
            .ToList();
        var check = await _conflictChecker.CheckAsync(plannedRows, request.AnalyseToken, cancellationToken);
        var conflictsByClient = check.NewConflicts
            .GroupBy(c => c.ClientId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var authorized = await _overrideAuthorizer.IsAuthorizedAsync(request.OverrideBlock);

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

            if (availabilityByClient.TryGetValue(member.Id, out var availability) &&
                AvailabilityMatcher.IsUnavailable(availability, request.Date, request.StartTime, request.EndTime))
            {
                excluded.Add(new ExcludedCandidate(member.Id, name, UnavailableReason));
                continue;
            }

            var conflicts = conflictsByClient.TryGetValue(member.Id, out var found)
                ? found
                : new List<ScheduleValidationNotificationDto>();

            // Hard exclusion: ANY Error-level conflict the placement would newly introduce (structural
            // or Block-mode enforcement escalation), plus the precise pair checks that exclude even in
            // Warn mode (rest violation) and the qualification gaps. Only a Block-mode escalation
            // (Type=Error tagged with EnforcementRuleParamKey) may be overridden by an authorized
            // supervisor; a structural Error never can.
            var hardConflicts = conflicts
                .Where(c => c.Type == ScheduleValidationType.Error || IsAlwaysExcluding(c))
                .ToList();
            if (hardConflicts.Count > 0)
            {
                var nonOverridable = hardConflicts.FirstOrDefault(c => !authorized || !IsOverridableBlocking(c));
                if (nonOverridable != null)
                {
                    excluded.Add(new ExcludedCandidate(member.Id, name, nonOverridable.Comment));
                    continue;
                }
                LogOverride(member.Id, hardConflicts);
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

            // Missing entry or a zero target means "no fairness signal" (deficit 0), mirroring the
            // opt-in availability discipline; the candidate is never excluded on this basis.
            var deficit = periodHoursByClient.TryGetValue(member.Id, out var hours)
                ? hours.GuaranteedHours - hours.Hours
                : 0m;

            eligible.Add(new ReplacementCandidate(
                member.Id,
                name,
                IsPreferred: hasPreference && preferenceType == ShiftPreferenceType.Preferred,
                SoftConflicts: conflicts,
                TargetHoursDeficit: deficit));
        }

        var ranked = eligible
            .OrderByDescending(c => c.IsPreferred)
            .ThenBy(c => c.SoftConflicts.Count)
            .ThenByDescending(c => c.TargetHoursDeficit)
            .ThenBy(c => c.Name)
            .ToList();

        return new ReplacementSearchResult(ranked, excluded);
    }

    private static bool IsAlwaysExcluding(ScheduleValidationNotificationDto conflict) =>
        conflict.Comment is ScheduleValidationKeys.Collision or ScheduleValidationKeys.RestViolation
            or QualificationValidationKeys.Missing
            or QualificationValidationKeys.Expired
            or QualificationValidationKeys.InsufficientLevel;

    private static bool IsOverridableBlocking(ScheduleValidationNotificationDto conflict) =>
        conflict.Type == ScheduleValidationType.Error
        && conflict.CommentParams.ContainsKey(ComplianceRuleNames.EnforcementRuleParamKey);

    private void LogOverride(Guid candidateId, IReadOnlyList<ScheduleValidationNotificationDto> conflicts)
    {
        var overriddenRules = conflicts
            .Where(c => c.CommentParams.ContainsKey(ComplianceRuleNames.EnforcementRuleParamKey))
            .Select(c => c.CommentParams[ComplianceRuleNames.EnforcementRuleParamKey])
            .Distinct()
            .ToList();
        var userName = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown";

        _logger.LogWarning(
            "Compliance override: user {User} overrode {RuleCount} blocked rule(s) ({Rules}) to keep candidate {CandidateId} eligible in find_replacement.",
            userName,
            overriddenRules.Count,
            string.Join(",", overriddenRules),
            candidateId);
    }

    private static string DisplayName(Client client)
        => string.IsNullOrWhiteSpace(client.FirstName)
            ? client.Name
            : $"{client.FirstName} {client.Name}";
}
