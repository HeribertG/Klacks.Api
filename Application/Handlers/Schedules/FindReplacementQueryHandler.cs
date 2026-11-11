// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for <see cref="FindReplacementQuery"/>. Candidates are the pool the automatic absence cover uses
/// (<see cref="IReplacementCandidatePoolResolver"/>): the members of the requested group and its subgroups and, marked
/// IsBorrowed, the other people under the same root. The caller's visibility is applied first, then the borrowed part
/// is cut to the first CrossGroupPoolCap people by id. A candidate is hard-excluded when absent while the slot is occupied (a Break on the date
/// whose absence type is not on-call and whose real span overlaps the slot with its lead and trail; an on-call Break whose window overlaps the slot makes the candidate rank first,
/// IsOnCall, while a non-overlapping one neither excludes nor prefers),
/// when explicitly unavailable for an hour the shift occupies (an opt-in availability window), when
/// assigning them would introduce ANY new Error-level conflict (collision, missing or expired mandatory
/// qualification, a mandatory qualification held below a BINDING minimum level, or a
/// Block-mode enforcement escalation such as restricted-time-window / period-cap / counter-rule /
/// rest-day-rotation), when a rest-time violation is found (excluding even in Warn mode), or when the
/// shift is blacklisted for them. A candidate whose contract lists the weekday of the slot as a day off is not
/// excluded but marked (IsContractDayOff; an on-call day counts as a working day, as in the absence cover).
/// Warning-level aggregate findings (overtime/consecutive/min-rest) are
/// a soft ranking signal (less headroom -> lower rank). Members of the group rank before borrowed people, within
/// each on-call candidates first, then the ones without a contract day off, then preferred
/// employees, then the candidates holding every mandatory qualification at the required level ahead of those
/// below a non-binding level (fewer missing levels first, LevelShortfall); among equally
/// clean candidates the one whose own row keeps its rhythm best ranks higher (RhythmDelta, the same row measure
/// the automatic replacement uses: a cover that closes a block of the same kind before a lonely shift), then the one
/// furthest below their period target hours (fairness, 3b/3c).
/// With a scenario token the checks read the scenario, which holds the plan of the requested group only. The home
/// works and absences of a borrowed person live in the real plan, so borrowed candidates are checked against the real
/// plan as well and a finding of either plan counts. The ranking signals (period hours, row rhythm) come from the plan
/// of the request. Absent and unavailable candidates are sorted out before the conflict check runs.
/// Group visibility is a security boundary: a group outside the caller's visibility is answered exactly like an
/// unknown group (empty result, the pool is never loaded), and within a visible group the pool is narrowed to the
/// clients the caller may see before anything else is evaluated, so neither the eligible nor the excluded list can
/// name a hidden employee. Admins and callers without a user are unrestricted (the guards decide).
/// </summary>
/// <param name="poolResolver">Resolves the members of the group and the borrowable people under its root</param>
/// <param name="conflictChecker">Pre-commit guardrail used to test each candidate placement</param>
/// <param name="preferenceRepository">Preferred/blacklisted shift preferences</param>
/// <param name="scheduleEntriesService">Reads the day's grid to find absent (Break) members</param>
/// <param name="availabilityRepository">Reads the day's hour-granular client availability windows</param>
/// <param name="periodHoursService">Resolves each candidate's period target vs. already-assigned hours (fairness signal)</param>
/// <param name="contractProvider">Reads the effective contract data of the pool for the weekday mark</param>
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
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Associations;
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

    private readonly IReplacementCandidatePoolResolver _poolResolver;
    private readonly IPreCommitConflictChecker _conflictChecker;
    private readonly IClientShiftPreferenceRepository _preferenceRepository;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IClientAvailabilityRepository _availabilityRepository;
    private readonly IPeriodHoursService _periodHoursService;
    private readonly IClientContractDataProvider _contractProvider;
    private readonly ISupervisorOverrideAuthorizer _overrideAuthorizer;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IScenarioTokenAccessGuard _scenarioTokenAccessGuard;
    private readonly ILogger<FindReplacementQueryHandler> _logger;

    public FindReplacementQueryHandler(
        IReplacementCandidatePoolResolver poolResolver,
        IPreCommitConflictChecker conflictChecker,
        IClientShiftPreferenceRepository preferenceRepository,
        IScheduleEntriesService scheduleEntriesService,
        IClientAvailabilityRepository availabilityRepository,
        IPeriodHoursService periodHoursService,
        IClientContractDataProvider contractProvider,
        ISupervisorOverrideAuthorizer overrideAuthorizer,
        IHttpContextAccessor httpContextAccessor,
        IAbsenceRepository absenceRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        IClientVisibilityGuard clientVisibilityGuard,
        IScenarioTokenAccessGuard scenarioTokenAccessGuard,
        ILogger<FindReplacementQueryHandler> logger)
    {
        _scenarioTokenAccessGuard = scenarioTokenAccessGuard;
        _poolResolver = poolResolver;
        _conflictChecker = conflictChecker;
        _preferenceRepository = preferenceRepository;
        _scheduleEntriesService = scheduleEntriesService;
        _availabilityRepository = availabilityRepository;
        _periodHoursService = periodHoursService;
        _contractProvider = contractProvider;
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

        var pool = await ResolveVisiblePoolAsync(request, cancellationToken);
        if (pool.Members.Count == 0)
        {
            return new ReplacementSearchResult([], []);
        }

        var signals = await LoadCandidateSignalsAsync(request, pool, cancellationToken);

        var eligible = new List<ReplacementCandidate>();
        var excluded = new List<ExcludedCandidate>();

        foreach (var member in pool.Members)
        {
            EvaluateCandidate(member, request, signals, eligible, excluded);
        }

        var ranked = Rank(eligible);

        return new ReplacementSearchResult(ranked, excluded);
    }

    /// <summary>
    /// Everything the per-candidate evaluation needs, loaded once for the whole pool: absences and on-call breaks,
    /// availability windows, shift preferences, period hours, the conflicts each placement would introduce,
    /// whether the caller may override a Block-mode escalation, the members' row shift kinds around the slot and their
    /// contract data. The conflict check only runs for candidates who are neither absent nor unavailable.
    /// </summary>
    /// <param name="pool">The visible candidate pool</param>
    private async Task<CandidateSignals> LoadCandidateSignalsAsync(
        FindReplacementQuery request, CandidatePool pool, CancellationToken cancellationToken)
    {
        var members = pool.Members;
        var (onLeaveClientIds, onCallClientIds) = await ResolveAbsencesAsync(request, pool.BorrowedIds, cancellationToken);

        var availabilityEntries = await _availabilityRepository.GetByDateRange(request.Date, request.Date.AddDays(1));
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

        var checkable = members
            .Where(m => !onLeaveClientIds.Contains(m.Id) && !IsUnavailable(availabilityByClient, m.Id, request))
            .ToList();
        var conflictsByClient = await CheckPlacementsAsync(request, checkable, pool.BorrowedIds, cancellationToken);
        var contracts = await _contractProvider.GetEffectiveContractDataForClientsAsync(
            members.Select(m => m.Id).ToList(), request.Date);

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
            slotKind,
            pool.BorrowedIds,
            contracts);
    }

    /// <summary>
    /// The conflicts each placement would introduce, per candidate. A borrowed candidate of a scenario request is also
    /// checked against the real plan, where their home works live; a finding of the real plan that the scenario already
    /// reports (ConflictFingerprint) is not added a second time. Findings of one plan are never merged.
    /// </summary>
    private async Task<Dictionary<Guid, List<ScheduleValidationNotificationDto>>> CheckPlacementsAsync(
        FindReplacementQuery request, List<Client> candidates, HashSet<Guid> borrowedIds, CancellationToken cancellationToken)
    {
        var plannedRows = candidates
            .Select(m => new PlannedWorkRow(
                m.Id, request.Date, request.StartTime, request.EndTime, request.ShiftId,
                request.LeadHours, request.TrailHours, request.ExtraPaidHours))
            .ToList();
        var conflicts = (await _conflictChecker.CheckAsync(plannedRows, request.AnalyseToken, cancellationToken))
            .NewConflicts.ToList();

        var borrowedRows = plannedRows.Where(row => borrowedIds.Contains(row.ClientId)).ToList();
        if (request.AnalyseToken is not null && borrowedRows.Count > 0)
        {
            var reported = conflicts.Select(ConflictFingerprint.Of).ToHashSet();
            var realPlan = (await _conflictChecker.CheckAsync(borrowedRows, null, cancellationToken)).NewConflicts;
            conflicts.AddRange(realPlan.Where(c => !reported.Contains(ConflictFingerprint.Of(c))));
        }

        return conflicts
            .GroupBy(c => c.ClientId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>
    /// The clients absent on the requested day (a Break whose absence type is not on-call) and the clients on call
    /// during the requested slot (an on-call Break whose window overlaps it). For a scenario request the breaks of the
    /// borrowed people are read from the real plan in addition.
    /// </summary>
    private async Task<(HashSet<Guid> OnLeaveClientIds, HashSet<Guid> OnCallClientIds)> ResolveAbsencesAsync(
        FindReplacementQuery request, HashSet<Guid> borrowedIds, CancellationToken cancellationToken)
    {
        var onLeaveCells = await ReadBreaksAsync(request, request.AnalyseToken, null, cancellationToken);
        if (request.AnalyseToken is not null && borrowedIds.Count > 0)
        {
            var realPlanCells = await ReadBreaksAsync(request, null, borrowedIds, cancellationToken);
            onLeaveCells = [.. onLeaveCells, .. realPlanCells];
        }

        var onCallAbsenceIds = await _absenceRepository.GetOnCallAbsenceIdsAsync(cancellationToken);
        var onLeaveClientIds = onLeaveCells
            .Where(c => !onCallAbsenceIds.Contains(c.EntryId)
                && OverlapsOccupiedTime(c, request))
            .Select(c => c.ClientId)
            .ToHashSet();
        var onCallClientIds = onLeaveCells
            .Where(c => onCallAbsenceIds.Contains(c.EntryId)
                && OverlapsSlot(c, request.Date, request.StartTime, request.EndTime))
            .Select(c => c.ClientId)
            .ToHashSet();

        return (onLeaveClientIds, onCallClientIds);
    }

    private Task<IReadOnlyList<ScheduleCell>> ReadBreaksAsync(
        FindReplacementQuery request, Guid? analyseToken, IReadOnlyCollection<Guid>? clientIds, CancellationToken cancellationToken)
        => _scheduleEntriesService.GetScheduleEntriesAsync(
            new ScheduleEntriesCriteria(request.Date, request.Date, [request.GroupId], analyseToken)
            {
                ClientIds = clientIds,
                EntryType = ScheduleEntryType.Break,
            },
            cancellationToken);

    private static bool IsUnavailable(
        Dictionary<Guid, IReadOnlyList<ClientAvailability>> availabilityByClient, Guid clientId, FindReplacementQuery request)
        => availabilityByClient.TryGetValue(clientId, out var availability)
            && AvailabilityMatcher.IsUnavailable(availability, request.Date, request.StartTime, request.EndTime);

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
        var isBorrowed = signals.BorrowedIds.Contains(member.Id);

        if (signals.OnLeaveClientIds.Contains(member.Id))
        {
            excluded.Add(new ExcludedCandidate(member.Id, name, AbsentReason, isBorrowed));
            return;
        }

        if (IsUnavailable(signals.AvailabilityByClient, member.Id, request))
        {
            excluded.Add(new ExcludedCandidate(member.Id, name, UnavailableReason, isBorrowed));
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
                excluded.Add(new ExcludedCandidate(member.Id, name, nonOverridable.Comment, isBorrowed));
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
            excluded.Add(new ExcludedCandidate(member.Id, name, BlacklistedReason, isBorrowed));
            return;
        }

        // Missing entry or a zero target means "no fairness signal" (deficit 0), mirroring the
        // opt-in availability discipline; the candidate is never excluded on this basis.
        var deficit = signals.PeriodHoursByClient.TryGetValue(member.Id, out var hours)
            ? hours.GuaranteedHours - hours.Hours
            : 0m;

        var isOnCall = signals.OnCallClientIds.Contains(member.Id);
        var contract = signals.Contracts.TryGetValue(member.Id, out var data) ? data : new EffectiveContractData();

        eligible.Add(new ReplacementCandidate(
            member.Id,
            name,
            IsPreferred: hasPreference && preferenceType == ShiftPreferenceType.Preferred,
            SoftConflicts: conflicts,
            TargetHoursDeficit: deficit,
            IsOnCall: isOnCall,
            IsBorrowed: isBorrowed,
            IsContractDayOff: !isOnCall && !ContractWeekday.WorksOn(contract, request.Date.DayOfWeek),
            LevelShortfall: SumLevelShortfall(conflicts),
            RhythmDelta: RowRhythm.CoverDelta(
                day => signals.RowKinds.TryGetValue((member.Id, day), out var kind) ? kind : ShiftCategory.Free,
                request.Date,
                signals.SlotKind)));
    }

    /// <summary>
    /// Members of the group before borrowed people, on-call first, a contract day off last, then preferred, fewer
    /// missing qualification levels, fewer soft conflicts, the smaller row-rhythm delta, the larger target-hours
    /// deficit, and finally the name.
    /// </summary>
    private static List<ReplacementCandidate> Rank(List<ReplacementCandidate> eligible)
        => eligible
            .OrderBy(c => c.IsBorrowed)
            .ThenByDescending(c => c.IsOnCall)
            .ThenBy(c => c.IsContractDayOff)
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
    /// and is rejected before any group or client is loaded; otherwise the resolved pool is narrowed to the
    /// visible clients, so every later step (absences, conflicts, period hours, excluded reasons) only ever sees
    /// employees the caller is allowed to know about. The borrowed part is cut to the cap after that, by id, and a
    /// query for one person is answered from that cut pool, so it judges nobody the full list would not show. The
    /// automatic cover cuts before the visibility, so both agree for a caller who sees everybody.
    /// </summary>
    private async Task<CandidatePool> ResolveVisiblePoolAsync(FindReplacementQuery request, CancellationToken cancellationToken)
    {
        var groupId = request.GroupId;
        if (!await _groupVisibilityGuard.IsGroupVisibleAsync(groupId, cancellationToken))
        {
            _logger.LogWarning("find_replacement was called for group {GroupId} outside the caller's visibility; no candidates.", groupId);
            return CandidatePool.Empty;
        }

        var pool = await _poolResolver.ResolveAsync(groupId, cancellationToken);
        if (pool is null)
        {
            _logger.LogWarning("find_replacement was called for unknown group {GroupId}; no candidates.", groupId);
            return CandidatePool.Empty;
        }

        var crossGroupIds = pool.CrossGroup.Select(c => c.Id).ToHashSet();
        var visible = await _clientVisibilityGuard.FilterVisibleAsync(
            pool.InGroup.Concat(pool.CrossGroup).ToList(), c => c.Id, cancellationToken);
        var borrowed = visible
            .Where(c => crossGroupIds.Contains(c.Id))
            .OrderBy(c => c.Id)
            .Take(RecoveryBuildConstants.CrossGroupPoolCap)
            .Where(c => IsAskedFor(request, c))
            .ToList();

        return new CandidatePool(
            [.. visible.Where(c => !crossGroupIds.Contains(c.Id) && IsAskedFor(request, c)), .. borrowed],
            borrowed.Select(c => c.Id).ToHashSet());
    }

    private static bool IsAskedFor(FindReplacementQuery request, Client client)
        => request.OnlyClientId is not { } only || client.Id == only;

    /// <summary>
    /// True when the absence's real span overlaps the time the slot occupies: the slot with its lead before and its trail
    /// after. An absence on the requested day that lies outside it, an appointment in the morning before a night slot,
    /// does not make the candidate absent.
    /// </summary>
    private static bool OverlapsOccupiedTime(ScheduleCell cell, FindReplacementQuery request)
    {
        var (absenceStart, absenceEnd) = WallInterval(
            request.Date, TimeOnly.FromTimeSpan(cell.StartTime), TimeOnly.FromTimeSpan(cell.EndTime));
        var (slotStart, slotEnd) = WallInterval(request.Date, request.StartTime, request.EndTime);
        var occupiedStart = slotStart - TimeSpan.FromTicks((long)(request.LeadHours * TimeSpan.TicksPerHour));
        var occupiedEnd = slotEnd + TimeSpan.FromTicks((long)(request.TrailHours * TimeSpan.TicksPerHour));
        return absenceStart < occupiedEnd && occupiedStart < absenceEnd;
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
        ShiftCategory SlotKind,
        HashSet<Guid> BorrowedIds,
        Dictionary<Guid, EffectiveContractData> Contracts);

    /// <summary>The visible candidates, members of the group first, and which of them are borrowed.</summary>
    private sealed record CandidatePool(List<Client> Members, HashSet<Guid> BorrowedIds)
    {
        public static CandidatePool Empty { get; } = new([], []);
    }
}
