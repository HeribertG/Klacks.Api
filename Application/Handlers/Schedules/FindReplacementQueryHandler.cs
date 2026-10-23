// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for <see cref="FindReplacementQuery"/>. Candidates are the active members of the requested
/// group and its subgroups; a candidate is hard-excluded when absent that day (a Break on the date whose
/// absence type is not on-call; an on-call Break whose window overlaps the slot makes the candidate rank first,
/// IsOnCall, while a non-overlapping one neither excludes nor prefers),
/// when explicitly unavailable for an hour the shift occupies (an opt-in availability window), when
/// assigning them would introduce ANY new Error-level conflict (collision, missing or expired mandatory
/// qualification, a mandatory qualification held below a BINDING minimum level, or a
/// Block-mode enforcement escalation such as restricted-time-window / period-cap / counter-rule /
/// rest-day-rotation), when a rest-time violation is found (excluding even in Warn mode), or when the
/// shift is blacklisted for them. Warning-level aggregate findings (overtime/consecutive/min-rest) are
/// a soft ranking signal (less headroom -> lower rank). On-call candidates rank first, then preferred
/// employees, then the candidates holding every mandatory qualification at the required level ahead of those
/// below a non-binding level (fewer missing levels first, LevelShortfall); among equally
/// clean candidates the one whose own row keeps its rhythm best ranks higher (RhythmDelta, the same row measure
/// the automatic replacement uses: a cover that closes a block of the same kind before a lonely shift), then the one
/// furthest below their period target hours (fairness, 3b/3c).
/// The candidate pool is resolved in two steps because IClientRepository filters on the group ROOT
/// (Group.Root ?? Group.Id): a subgroup id matches no root and would silently yield an empty pool, so
/// the root is resolved first and the root-wide result is then narrowed back to the requested group's
/// own subtree.
/// Group visibility is a security boundary: a group outside the caller's visibility is answered exactly like an
/// unknown group (empty result, the pool is never loaded), and within a visible group the pool is narrowed to the
/// clients the caller may see before anything else is evaluated, so neither the eligible nor the excluded list can
/// name a hidden employee. Admins and callers without a user are unrestricted (the guards decide).
/// </summary>
/// <param name="clientRepository">Resolves the active clients under the group's root (the raw pool)</param>
/// <param name="groupRepository">Resolves the requested group's root, because the client repository filters on the root id</param>
/// <param name="groupClientService">Narrows the root-wide pool back to the requested group's own subtree</param>
/// <param name="conflictChecker">Pre-commit guardrail used to test each candidate placement</param>
/// <param name="preferenceRepository">Preferred/blacklisted shift preferences</param>
/// <param name="scheduleEntriesService">Reads the day's grid to find absent (Break) members</param>
/// <param name="availabilityRepository">Reads the day's hour-granular client availability windows</param>
/// <param name="periodHoursService">Resolves each candidate's period target vs. already-assigned hours (fairness signal)</param>
/// <param name="overrideAuthorizer">Resolves whether a supervisor override is allowed at all (K1)</param>
/// <param name="httpContextAccessor">Resolves the caller's name for the override audit log</param>
/// <param name="absenceRepository">Resolves which absence types are on-call (not absent, preferred instead)</param>
/// <param name="groupVisibilityGuard">Decides whether the caller may see the requested group at all</param>
/// <param name="clientVisibilityGuard">Narrows the candidate pool to the employees the caller may see</param>
/// <param name="scenarioTokenAccessGuard">Answers a scenario token the caller may not see like an unknown group (empty result)</param>
/// <param name="logger">Logs a structured audit entry whenever a supervisor overrides a compliance exclusion</param>
using System.Globalization;
using System.Security.Claims;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Services.Schedules.Recovery;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Klacks.ScheduleRecovery.Engine;
using ShiftCategory = Klacks.ScheduleRecovery.Model.ShiftCategory;

namespace Klacks.Api.Application.Handlers.Schedules;

public sealed class FindReplacementQueryHandler : IRequestHandler<FindReplacementQuery, ReplacementSearchResult>
{
    private const string BlacklistedReason = "blacklisted";
    private const string AbsentReason = "absent";
    private const string UnavailableReason = "unavailable";

    private readonly IClientRepository _clientRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGetAllClientIdsFromGroupAndSubgroups _groupClientService;
    private readonly IPreCommitConflictChecker _conflictChecker;
    private readonly IClientShiftPreferenceRepository _preferenceRepository;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IClientAvailabilityRepository _availabilityRepository;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly ISupervisorOverrideAuthorizer _overrideAuthorizer;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IScenarioTokenAccessGuard _scenarioTokenAccessGuard;
    private readonly ILogger<FindReplacementQueryHandler> _logger;

    public FindReplacementQueryHandler(
        IClientRepository clientRepository,
        IGroupRepository groupRepository,
        IGetAllClientIdsFromGroupAndSubgroups groupClientService,
        IPreCommitConflictChecker conflictChecker,
        IClientShiftPreferenceRepository preferenceRepository,
        IScheduleEntriesService scheduleEntriesService,
        IClientAvailabilityRepository availabilityRepository,
        IPeriodHoursService periodHoursService,
        ISupervisorOverrideAuthorizer overrideAuthorizer,
        IHttpContextAccessor httpContextAccessor,
        IAbsenceRepository absenceRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        IClientVisibilityGuard clientVisibilityGuard,
        IScenarioTokenAccessGuard scenarioTokenAccessGuard,
        ILogger<FindReplacementQueryHandler> logger)
    {
        _scenarioTokenAccessGuard = scenarioTokenAccessGuard;
        _clientRepository = clientRepository;
        _groupRepository = groupRepository;
        _groupClientService = groupClientService;
        _conflictChecker = conflictChecker;
        _preferenceRepository = preferenceRepository;
        _scheduleEntriesService = scheduleEntriesService;
        _availabilityRepository = availabilityRepository;
        _periodHoursService = periodHoursService;
        _overrideAuthorizer = overrideAuthorizer;
        _httpContextAccessor = httpContextAccessor;
        _absenceRepository = absenceRepository;
        _groupVisibilityGuard = groupVisibilityGuard;
        _clientVisibilityGuard = clientVisibilityGuard;
        _logger = logger;
    }

    public async Task<ReplacementSearchResult> Handle(FindReplacementQuery request, CancellationToken cancellationToken)
    {
        if (!await _scenarioTokenAccessGuard.IsVisibleAsync(request.AnalyseToken, cancellationToken))
        {
            return new ReplacementSearchResult([], []);
        }

        var members = await ResolveVisibleGroupMembersAsync(request.GroupId, cancellationToken);
        if (members.Count == 0)
        {
            return new ReplacementSearchResult([], []);
        }

        var signals = await LoadCandidateSignalsAsync(request, members, cancellationToken);

        var eligible = new List<ReplacementCandidate>();
        var excluded = new List<ExcludedCandidate>();

        foreach (var member in members)
        {
            EvaluateCandidate(member, request, signals, eligible, excluded);
        }

        var ranked = Rank(eligible);

        return new ReplacementSearchResult(ranked, excluded);
    }

    /// <summary>
    /// Everything the per-candidate evaluation needs, loaded once for the whole pool: absences and on-call breaks,
    /// availability windows, shift preferences, period hours, the conflicts each placement would introduce,
    /// whether the caller may override a Block-mode escalation, and the members' row shift kinds around the slot.
    /// </summary>
    /// <param name="members">The visible candidate pool</param>
    private async Task<CandidateSignals> LoadCandidateSignalsAsync(
        FindReplacementQuery request, List<Client> members, CancellationToken cancellationToken)
    {
        var (onLeaveClientIds, onCallClientIds) = await ResolveAbsencesAsync(request, cancellationToken);

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
        var rowKinds = await LoadRowKindsAsync(request, cancellationToken);
        var slotKind = ShiftCategoryClassifier.CategoryOf(request.StartTime.ToTimeSpan(), request.EndTime.ToTimeSpan());

        return new CandidateSignals(
            onLeaveClientIds,
            onCallClientIds,
            availabilityByClient,
            preferenceByClient,
            periodHoursByClient,
            conflictsByClient,
            authorized,
            rowKinds,
            slotKind);
    }

    /// <summary>
    /// The clients absent on the requested day (a Break whose absence type is not on-call) and the clients on call
    /// during the requested slot (an on-call Break whose window overlaps it).
    /// </summary>
    private async Task<(HashSet<Guid> OnLeaveClientIds, HashSet<Guid> OnCallClientIds)> ResolveAbsencesAsync(
        FindReplacementQuery request, CancellationToken cancellationToken)
    {
        var onLeaveCells = await _scheduleEntriesService.GetScheduleEntriesAsync(
            new ScheduleEntriesCriteria(request.Date, request.Date, [request.GroupId], request.AnalyseToken)
            {
                EntryType = ScheduleEntryType.Break,
            },
            cancellationToken);
        var onCallAbsenceIds = await _absenceRepository.GetOnCallAbsenceIdsAsync(cancellationToken);
        var onLeaveClientIds = onLeaveCells
            .Where(c => !onCallAbsenceIds.Contains(c.EntryId))
            .Select(c => c.ClientId)
            .ToHashSet();
        var onCallClientIds = onLeaveCells
            .Where(c => onCallAbsenceIds.Contains(c.EntryId)
                && OverlapsSlot(c, request.Date, request.StartTime, request.EndTime))
            .Select(c => c.ClientId)
            .ToHashSet();

        return (onLeaveClientIds, onCallClientIds);
    }

    /// <summary>
    /// Adds the member either to the excluded list with the first reason that applies (absent, unavailable, a hard
    /// conflict that may not be overridden, blacklisted) or to the eligible list with its ranking signals; an
    /// overridden Block-mode escalation is audit-logged.
    /// </summary>
    private void EvaluateCandidate(
        Client member,
        FindReplacementQuery request,
        CandidateSignals signals,
        List<ReplacementCandidate> eligible,
        List<ExcludedCandidate> excluded)
    {
        var name = DisplayName(member);

        if (signals.OnLeaveClientIds.Contains(member.Id))
        {
            excluded.Add(new ExcludedCandidate(member.Id, name, AbsentReason));
            return;
        }

        if (signals.AvailabilityByClient.TryGetValue(member.Id, out var availability) &&
            AvailabilityMatcher.IsUnavailable(availability, request.Date, request.StartTime, request.EndTime))
        {
            excluded.Add(new ExcludedCandidate(member.Id, name, UnavailableReason));
            return;
        }

        var conflicts = signals.ConflictsByClient.TryGetValue(member.Id, out var found)
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
            var nonOverridable = hardConflicts.FirstOrDefault(c => !signals.IsOverrideAuthorized || !IsOverridableBlocking(c));
            if (nonOverridable != null)
            {
                excluded.Add(new ExcludedCandidate(member.Id, name, nonOverridable.Comment));
                return;
            }
            LogOverride(member.Id, hardConflicts);
        }

        // ShiftPreferenceType.Preferred is 0 (the enum default), so the bool result MUST gate the
        // lookup — otherwise a miss leaves preferenceType = Preferred and every member without a
        // preference would be treated as preferred.
        var hasPreference = signals.PreferenceByClient.TryGetValue(member.Id, out var preferenceType);
        if (hasPreference && preferenceType == ShiftPreferenceType.Blacklist)
        {
            excluded.Add(new ExcludedCandidate(member.Id, name, BlacklistedReason));
            return;
        }

        // Missing entry or a zero target means "no fairness signal" (deficit 0), mirroring the
        // opt-in availability discipline; the candidate is never excluded on this basis.
        var deficit = signals.PeriodHoursByClient.TryGetValue(member.Id, out var hours)
            ? hours.GuaranteedHours - hours.Hours
            : 0m;

        eligible.Add(new ReplacementCandidate(
            member.Id,
            name,
            IsPreferred: hasPreference && preferenceType == ShiftPreferenceType.Preferred,
            SoftConflicts: conflicts,
            TargetHoursDeficit: deficit,
            IsOnCall: signals.OnCallClientIds.Contains(member.Id),
            LevelShortfall: SumLevelShortfall(conflicts),
            RhythmDelta: RowRhythm.CoverDelta(
                day => signals.RowKinds.TryGetValue((member.Id, day), out var kind) ? kind : ShiftCategory.Free,
                request.Date,
                signals.SlotKind)));
    }

    /// <summary>
    /// On-call first, then preferred, then fewer missing qualification levels, fewer soft conflicts, the smaller
    /// row-rhythm delta, the larger target-hours deficit, and finally the name.
    /// </summary>
    private static List<ReplacementCandidate> Rank(List<ReplacementCandidate> eligible)
        => eligible
            .OrderByDescending(c => c.IsOnCall)
            .ThenByDescending(c => c.IsPreferred)
            .ThenBy(c => c.LevelShortfall)
            .ThenBy(c => c.SoftConflicts.Count)
            .ThenBy(c => c.RhythmDelta)
            .ThenByDescending(c => c.TargetHoursDeficit)
            .ThenBy(c => c.Name)
            .ToList();

    /// <summary>
    /// The shift kind of every work the group's members start in the days around the slot (the reach of the row
    /// rhythm plus one day on each side, the neighbours the measure looks at), keyed by member and day; for a day
    /// with several works the earliest start decides. Read on the same plan as the rest of the search (the real plan
    /// or the requested scenario).
    /// </summary>
    private async Task<IReadOnlyDictionary<(Guid ClientId, DateOnly Date), ShiftCategory>> LoadRowKindsAsync(
        FindReplacementQuery request, CancellationToken cancellationToken)
    {
        var reach = RowRhythm.Reach + 1;
        var cells = await _scheduleEntriesService.GetScheduleEntriesAsync(
            new ScheduleEntriesCriteria(request.Date.AddDays(-reach), request.Date.AddDays(reach), [request.GroupId], request.AnalyseToken)
            {
                EntryType = ScheduleEntryType.Work,
            },
            cancellationToken);

        return cells
            .Where(c => c.EntryType == (int)ScheduleEntryType.Work)
            .GroupBy(c => (c.ClientId, DateOnly.FromDateTime(c.EntryDate)))
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var first = g.OrderBy(c => c.StartTime).First();
                    return ShiftCategoryClassifier.CategoryOf(first.StartTime, first.EndTime);
                });
    }

    /// <summary>
    /// The candidate pool as the caller may see it. A hidden group yields the same empty pool as an unknown one
    /// and is rejected before any group or client is loaded; otherwise the resolved members are narrowed to the
    /// visible clients, so every later step (absences, conflicts, period hours, excluded reasons) only ever sees
    /// employees the caller is allowed to know about.
    /// </summary>
    /// <param name="groupId">Group whose members form the candidate pool; may be a root or a subgroup</param>
    private async Task<List<Client>> ResolveVisibleGroupMembersAsync(Guid groupId, CancellationToken cancellationToken)
    {
        if (!await _groupVisibilityGuard.IsGroupVisibleAsync(groupId, cancellationToken))
        {
            _logger.LogWarning("find_replacement was called for group {GroupId} outside the caller's visibility; no candidates.", groupId);
            return [];
        }

        var members = await ResolveGroupMembersAsync(groupId, cancellationToken);
        return await _clientVisibilityGuard.FilterVisibleAsync(members, c => c.Id, cancellationToken);
    }

    /// <summary>
    /// Resolves the candidate pool for the requested group. <see cref="IClientRepository"/> filters on the
    /// group ROOT (<c>Group.Root ?? Group.Id</c>), so handing it a subgroup id matches nothing and returns
    /// an empty pool without any error - the same root resolution the recovery snapshot builder performs.
    /// The root-wide result is then narrowed back to the requested group's own subtree, because the query
    /// asks for the members of THAT group, not for everyone under its root.
    /// </summary>
    /// <param name="groupId">Group whose members form the candidate pool; may be a root or a subgroup</param>
    private async Task<List<Client>> ResolveGroupMembersAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.Get(groupId);
        if (group == null)
        {
            _logger.LogWarning("find_replacement was called for unknown group {GroupId}; no candidates.", groupId);
            return [];
        }

        var underRoot = await _clientRepository.GetActiveClientsWithAddressesForGroupsAsync(
            new List<Guid> { group.Root ?? groupId }, cancellationToken);
        if (underRoot.Count == 0)
        {
            return [];
        }

        var memberIds = (await _groupClientService.GetAllClientIdsFromGroupAndSubgroups(groupId)).ToHashSet();
        return underRoot.Where(c => memberIds.Contains(c.Id)).ToList();
    }

    /// <summary>
    /// True when the on-call break's window overlaps the requested slot. Both intervals live on the requested
    /// date; an end at or before the start continues on the next day (start == end is a full 24 hours), the
    /// same convention the timeline uses for breaks and night shifts.
    /// </summary>
    private static bool OverlapsSlot(ScheduleCell cell, DateOnly date, TimeOnly slotStart, TimeOnly slotEnd)
    {
        var (breakStart, breakEnd) = WallInterval(date, TimeOnly.FromTimeSpan(cell.StartTime), TimeOnly.FromTimeSpan(cell.EndTime));
        var (requestStart, requestEnd) = WallInterval(date, slotStart, slotEnd);
        return breakStart < requestEnd && requestStart < breakEnd;
    }

    private static (DateTime Start, DateTime End) WallInterval(DateOnly date, TimeOnly start, TimeOnly end)
        => (date.ToDateTime(start), end <= start ? date.AddDays(1).ToDateTime(end) : date.ToDateTime(end));

    private static bool IsAlwaysExcluding(ScheduleValidationNotificationDto conflict) =>
        conflict.Comment is ScheduleValidationKeys.Collision or ScheduleValidationKeys.RestViolation
            or QualificationValidationKeys.Missing
            or QualificationValidationKeys.Expired;

    /// <summary>
    /// Missing levels over the non-blocking InsufficientLevel warnings of a candidate; a binding level is an Error and
    /// excludes the candidate before ranking, so only non-binding levels reach this sum.
    /// </summary>
    internal static int SumLevelShortfall(IEnumerable<ScheduleValidationNotificationDto> conflicts) =>
        conflicts
            .Where(c => c.Comment == QualificationValidationKeys.InsufficientLevel
                && c.Type == ScheduleValidationType.Warning)
            .Sum(c => c.CommentParams.TryGetValue(QualificationValidationParamKeys.LevelDelta, out var raw)
                && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var delta)
                    ? delta
                    : 0);

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

    private sealed record CandidateSignals(
        HashSet<Guid> OnLeaveClientIds,
        HashSet<Guid> OnCallClientIds,
        Dictionary<Guid, IReadOnlyList<ClientAvailability>> AvailabilityByClient,
        Dictionary<Guid, ShiftPreferenceType> PreferenceByClient,
        Dictionary<Guid, Domain.DTOs.Schedules.PeriodHoursResource> PeriodHoursByClient,
        Dictionary<Guid, List<ScheduleValidationNotificationDto>> ConflictsByClient,
        bool IsOverrideAuthorized,
        IReadOnlyDictionary<(Guid ClientId, DateOnly Date), ShiftCategory> RowKinds,
        ShiftCategory SlotKind);
}
