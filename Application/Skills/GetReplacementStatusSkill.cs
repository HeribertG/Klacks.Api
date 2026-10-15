// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Answers "who was already asked to cover this employee's absence, who said yes or no, is the shift covered?" from the
/// replacement request book (the listed rows use the same guards as GET Recovery/Requests: ListReplacementRequestsQuery,
/// group visibility of the candidate AND the absent employee) and from the escalation chains woken for the absence. Rows are
/// grouped per slot (scenario x date x shift) and each slot gets a coverage status; only a recorded contact is a
/// contact - a proposed candidate was NOT asked, and the book holds nothing for a call nobody entered. Escalation
/// chains (which planners were woken, who took over) are shown for the AbsenceCoverage purpose only, narrowed to the
/// caller's group visibility exactly like the intervention list (root-based through IPlanningAudienceResolver: a chain
/// is visible when its group's root is one of the caller's own visibility roots, or when the caller holds an open stage
/// of it; a chain without group is administrator-or-stage-holder only). A shift outside the caller's group visibility is
/// listed without its name; that check uses IGroupVisibilityGuard, i.e. the caller's expanded visible group ids with
/// subgroups, so the two checks can differ for a group below a visible root. The skill is withheld on the capped
/// channels (Slack owner bridge, MCP) because the chain check reads the database role, not the capped rights. A name that matches several employees is answered
/// with the candidate list, a hidden or unknown employee with the same not-found answer.
/// A slot's status is classified over every recorded row, including rows of candidates outside the caller's group
/// visibility (so a slot covered by a hidden colleague is not reported as open); those rows are only counted, never named.
/// Every instant is rendered in the company time zone, and the date range is reported as the search window, not as the
/// absence period - the days that actually have slots are listed separately.
/// </summary>
/// <param name="clientId">Optional. UUID of the absent employee; alternative to the name.</param>
/// <param name="firstName">Optional. First name of the absent employee.</param>
/// <param name="lastName">Optional. Last name of the absent employee.</param>
/// <param name="idNumber">Optional. Visible client number that disambiguates duplicate names.</param>
/// <param name="fromDate">Optional. First slot date (yyyy-MM-dd); default 7 days before today.</param>
/// <param name="untilDate">Optional. Last slot date (yyyy-MM-dd); default 14 days after today.</param>
/// <param name="analyseToken">Optional. UUID of one scenario; default is every scenario and the real plan.</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.Escalation;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_replacement_status")]
public class GetReplacementStatusSkill : BaseSkillImplementation
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string DateTimeFormat = "yyyy-MM-dd HH\\:mm";
    private const string TimeFormat = "HH\\:mm";
    private const int DefaultPastDays = 7;
    private const int DefaultFutureDays = 14;
    private const string FromDateParameter = "fromDate";
    private const string UntilDateParameter = "untilDate";
    private const string ContactProposed = "ProposedNotAsked";
    private const string ContactRequested = "AskedNoAnswerYet";
    private const string ContactAccepted = "Accepted";
    private const string ContactDeclined = "Declined";
    private const string ContactNotReached = "NotReached";

    private const string MissingClientMessage =
        "Say whose absence to look up: give firstName and lastName (optionally idNumber), or clientId.";

    private const string InvalidRangeMessage = "untilDate must not be before fromDate.";

    private const string RangeTooLongMessage = "The period may span at most {0} days.";

    private const string NoRowsMessage =
        "Nothing is recorded in the replacement request book for {0} between {1} and {2}. Contacts are only recorded " +
        "when they are entered in the recovery dialog (or proposed by a cover-absence run), so this does NOT prove that " +
        "nobody was called.";

    private const string RowsMessage =
        "{0}: {1} slot(s) in the replacement request book in the search window {2} to {3} ({4} covered); {5} escalation " +
        "chain(s) visible to you. Proposed means the candidate was NOT asked; only recorded answers count.";

    private const string SearchWindowNote =
        "SearchFrom/SearchUntil is only the window that was searched, NOT the absence period; DatesWithEntries lists the days " +
        "that have entries in the book (scenario slots included). All times are in the company time zone.";

    private const string HiddenEntriesNote =
        "Some slots include entries of employees outside your groups (HiddenEntries). They count for the slot status but " +
        "are not listed, so never say that nobody declined or accepted when HiddenEntries is above zero.";

    private const string TruncatedNote =
        "The request book holds more rows than the answer limit; narrow the period to see them all.";

    private const string MessengerSourceNote =
        "Source Messenger only says the absence was reported through a messenger; it is not a contact with the candidate.";

    private const string AnswerNote =
        "A candidate without a recorded answer may still have been called; the book only knows what was entered.";

    private readonly IMediator _mediator;
    private readonly IClientSearchRepository _searchRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IShiftRepository _shiftRepository;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IEscalationChainRepository _chainRepository;
    private readonly IEscalationChainVisibilityFilter _chainVisibilityFilter;
    private readonly ICompanyClock _companyClock;
    private readonly IReplacementRequestRepository _requestRepository;
    private readonly IScenarioTokenAccessGuard _scenarioTokenAccessGuard;

    public GetReplacementStatusSkill(
        IMediator mediator,
        IClientSearchRepository searchRepository,
        IClientRepository clientRepository,
        IClientVisibilityGuard clientVisibilityGuard,
        IShiftRepository shiftRepository,
        IGroupVisibilityGuard groupVisibilityGuard,
        IAbsenceRepository absenceRepository,
        IEscalationChainRepository chainRepository,
        IEscalationChainVisibilityFilter chainVisibilityFilter,
        ICompanyClock companyClock,
        IReplacementRequestRepository requestRepository,
        IScenarioTokenAccessGuard scenarioTokenAccessGuard)
    {
        _mediator = mediator;
        _searchRepository = searchRepository;
        _clientRepository = clientRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _shiftRepository = shiftRepository;
        _groupVisibilityGuard = groupVisibilityGuard;
        _absenceRepository = absenceRepository;
        _chainRepository = chainRepository;
        _chainVisibilityFilter = chainVisibilityFilter;
        _companyClock = companyClock;
        _requestRepository = requestRepository;
        _scenarioTokenAccessGuard = scenarioTokenAccessGuard;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientIdRaw = GetParameter<string>(parameters, "clientId");
        var firstName = GetParameter<string>(parameters, "firstName");
        var lastName = GetParameter<string>(parameters, "lastName");
        if (string.IsNullOrWhiteSpace(clientIdRaw) && string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
        {
            return SkillResult.Error(MissingClientMessage);
        }

        if (!ScenarioScopeParameter.TryRead(parameters, out var analyseToken, out var scopeError))
        {
            return SkillResult.Error(scopeError!);
        }

        var (absent, clientError) = await ClientResolver.ResolveByIdOrNameAsync(
            _searchRepository, _clientRepository, _clientVisibilityGuard, clientIdRaw, firstName, lastName,
            GetParameter<int?>(parameters, ClientResolver.IdNumberParameterName), cancellationToken);
        if (absent == null)
        {
            return SkillResult.Error(clientError!);
        }

        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        var fromDate = GetParameter<DateOnly?>(parameters, FromDateParameter) ?? today.AddDays(-DefaultPastDays);
        var untilDate = GetParameter<DateOnly?>(parameters, UntilDateParameter) ?? today.AddDays(DefaultFutureDays);
        if (untilDate < fromDate)
        {
            return SkillResult.Error(InvalidRangeMessage);
        }

        if (untilDate.DayNumber - fromDate.DayNumber >= ReplacementRequestLimits.MaxListSpanDays)
        {
            return SkillResult.Error(string.Format(RangeTooLongMessage, ReplacementRequestLimits.MaxListSpanDays));
        }

        var rows = await _mediator.Send(
            new ListReplacementRequestsQuery(absent.Id, fromDate, untilDate, analyseToken), cancellationToken);
        var timeZone = await _companyClock.GetTimeZoneAsync(cancellationToken);
        var outcomes = await ReadAllSlotOutcomesAsync(absent.Id, fromDate, untilDate, analyseToken, cancellationToken);
        var slots = await BuildSlotsAsync(rows, outcomes, timeZone, context.UserLanguage, cancellationToken);
        var chains = await BuildChainsAsync(context, absent.Id, fromDate, untilDate, timeZone, cancellationToken);

        var absentName = DisplayName(absent);
        var data = new
        {
            AbsentClient = new { absent.Id, Name = absentName },
            SearchFrom = fromDate.ToString(DateFormat),
            SearchUntil = untilDate.ToString(DateFormat),
            DatesWithEntries = slots.Select(s => s.Date).Distinct().ToList(),
            TimeZone = timeZone.Id,
            SlotCount = slots.Count,
            Slots = slots,
            EscalationChains = chains,
            Notes = BuildNotes(rows, slots, outcomes.Values.Sum(o => o.TotalEntries))
        };

        var message = slots.Count == 0 && chains.Count == 0
            ? string.Format(NoRowsMessage, absentName, fromDate.ToString(DateFormat), untilDate.ToString(DateFormat))
            : string.Format(
                RowsMessage, absentName, slots.Count, fromDate.ToString(DateFormat), untilDate.ToString(DateFormat),
                slots.Count(s => s.Status == nameof(ReplacementSlotStatus.Covered)), chains.Count);

        return SkillResult.SuccessResult(data, message);
    }

    private static List<string> BuildNotes(
        IReadOnlyList<ReplacementRequestResource> rows, IReadOnlyList<SlotView> slots, int allRowCount)
    {
        var notes = new List<string> { AnswerNote, SearchWindowNote };
        if (slots.Any(s => s.HiddenEntries > 0))
        {
            notes.Add(HiddenEntriesNote);
        }

        if (rows.Any(r => r.Source == ReplacementRequestSource.Messenger))
        {
            notes.Add(MessengerSourceNote);
        }

        if (Math.Max(rows.Count, allRowCount) >= ReplacementRequestLimits.MaxListRows)
        {
            notes.Add(TruncatedNote);
        }

        return notes;
    }

    /// <summary>
    /// Status and row count per slot over EVERY row of the book in the same bounds, the hidden candidates included, so
    /// the visible list can say "covered" or "n entries hidden" without naming anybody. A scenario the caller may not see
    /// yields nothing, exactly like the visible list.
    /// </summary>
    private async Task<Dictionary<SlotKey, SlotOutcome>> ReadAllSlotOutcomesAsync(
        Guid absentClientId, DateOnly fromDate, DateOnly untilDate, Guid? analyseToken, CancellationToken cancellationToken)
    {
        if (!await _scenarioTokenAccessGuard.IsVisibleAsync(analyseToken, cancellationToken))
        {
            return [];
        }

        var allRows = await _requestRepository.ListAsync(
            new ReplacementRequestFilter(absentClientId, fromDate, untilDate, analyseToken),
            ReplacementRequestLimits.MaxListRows,
            cancellationToken);

        return allRows
            .GroupBy(r => new SlotKey(r.AnalyseToken, r.Date, r.ShiftId))
            .ToDictionary(
                group => group.Key,
                group => new SlotOutcome(
                    ReplacementSlotStatusPolicy.Classify(group.Select(r => (r.Outcome, r.AppliedAtUtc.HasValue)).ToList()),
                    group.Count()));
    }

    private async Task<List<SlotView>> BuildSlotsAsync(
        IReadOnlyList<ReplacementRequestResource> rows,
        Dictionary<SlotKey, SlotOutcome> outcomes,
        TimeZoneInfo timeZone,
        string? language,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var candidateNames = (await _clientRepository.GetByIdsAsync(rows.Select(r => r.CandidateClientId).Distinct(), cancellationToken))
            .ToDictionary(c => c.Id, DisplayName);
        var shiftNames = await ResolveShiftNamesAsync(rows, cancellationToken);
        var absenceNames = await ResolveAbsenceNamesAsync(rows, language);

        return rows
            .GroupBy(r => (r.AnalyseToken, r.Date, r.ShiftId))
            .Select(group => ToSlot(group.ToList(), outcomes, timeZone, candidateNames, shiftNames, absenceNames))
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .ToList();
    }

    private static SlotView ToSlot(
        List<ReplacementRequestResource> group,
        Dictionary<SlotKey, SlotOutcome> outcomes,
        TimeZoneInfo timeZone,
        Dictionary<Guid, string> candidateNames,
        Dictionary<Guid, string?> shiftNames,
        Dictionary<Guid, string?> absenceNames)
    {
        var first = group[0];
        var visibleStatus = ReplacementSlotStatusPolicy.Classify(
            group.Select(r => (r.Outcome, r.AppliedAtUtc.HasValue)).ToList());
        var hasOutcome = outcomes.TryGetValue(new SlotKey(first.AnalyseToken, first.Date, first.ShiftId), out var outcome);
        var status = hasOutcome ? outcome!.Status : visibleStatus;
        var hiddenEntries = hasOutcome ? Math.Max(0, outcome!.TotalEntries - group.Count) : 0;
        var absenceId = group.Select(r => r.AbsenceId).FirstOrDefault(id => id.HasValue);

        return new SlotView(
            first.Date.ToString(DateFormat),
            first.Date.DayOfWeek.ToString(),
            first.StartTime.ToString(TimeFormat),
            first.EndTime.ToString(TimeFormat),
            first.ShiftId,
            shiftNames.GetValueOrDefault(first.ShiftId),
            first.AnalyseToken,
            first.AnalyseToken.HasValue,
            absenceId.HasValue ? absenceNames.GetValueOrDefault(absenceId.Value) : null,
            status.ToString(),
            hiddenEntries,
            group.Any(r => r.AppliedAtUtc.HasValue),
            group.OrderBy(r => r.ReportedAtUtc).Select(r => new CandidateView(
                r.CandidateClientId,
                candidateNames.GetValueOrDefault(r.CandidateClientId),
                r.Source.ToString(),
                ContactOf(r.Outcome),
                r.Outcome.ToString(),
                ToCompanyTime(r.OutcomeAtUtc, timeZone),
                r.AppliedAtUtc.HasValue,
                r.IsShortNotice)).ToList());
    }

    private static string ContactOf(ReplacementRequestOutcome outcome) => outcome switch
    {
        ReplacementRequestOutcome.Requested => ContactRequested,
        ReplacementRequestOutcome.Accepted => ContactAccepted,
        ReplacementRequestOutcome.Declined => ContactDeclined,
        ReplacementRequestOutcome.NotReached => ContactNotReached,
        _ => ContactProposed
    };

    private async Task<Dictionary<Guid, string?>> ResolveShiftNamesAsync(
        IReadOnlyList<ReplacementRequestResource> rows, CancellationToken cancellationToken)
    {
        var names = new Dictionary<Guid, string?>();
        foreach (var shiftId in rows.Select(r => r.ShiftId).Distinct())
        {
            var shift = await _shiftRepository.Get(shiftId);
            var visible = shift != null && await ShiftGroupVisibility.IsVisibleAsync(shift, _groupVisibilityGuard, cancellationToken);
            names[shiftId] = visible ? shift!.Name : null;
        }

        return names;
    }

    private async Task<Dictionary<Guid, string?>> ResolveAbsenceNamesAsync(
        IReadOnlyList<ReplacementRequestResource> rows, string? language)
    {
        var names = new Dictionary<Guid, string?>();
        foreach (var absenceId in rows.Where(r => r.AbsenceId.HasValue).Select(r => r.AbsenceId!.Value).Distinct())
        {
            var absence = await _absenceRepository.Get(absenceId);
            names[absenceId] = absence?.Name?.GetValueOrFirstAvailable(language);
        }

        return names;
    }

    private async Task<List<ChainView>> BuildChainsAsync(
        SkillExecutionContext context,
        Guid absentClientId,
        DateOnly fromDate,
        DateOnly untilDate,
        TimeZoneInfo timeZone,
        CancellationToken cancellationToken)
    {
        var fromUtc = CompanyWallClockToUtcConverter.ConvertToUtc(fromDate.ToDateTime(TimeOnly.MinValue), timeZone);
        var untilUtc = CompanyWallClockToUtcConverter.ConvertToUtc(untilDate.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone);

        var chains = await _chainRepository.GetAbsenceCoverageChainsByAbsentClientAsync(
            absentClientId, fromUtc, untilUtc, cancellationToken);
        var visible = await _chainVisibilityFilter.FilterVisibleAsync(chains, context.UserId.ToString(), cancellationToken);

        var mayReadAnyReason = context.UserPermissions.Contains(Roles.Admin) || context.UserPermissions.Contains(Roles.Authorised);
        return visible.Select(chain => ToChain(chain, MayReadCancelReason(chain, context, mayReadAnyReason), timeZone)).ToList();
    }

    private static bool MayReadCancelReason(EscalationChain chain, SkillExecutionContext context, bool mayReadAnyReason) =>
        mayReadAnyReason || string.Equals(chain.CancelledByUserId, context.UserId.ToString(), StringComparison.OrdinalIgnoreCase);

    private static ChainView ToChain(EscalationChain chain, bool mayReadCancelReason, TimeZoneInfo timeZone) => new(
        chain.Id,
        chain.Status.ToString(),
        ToCompanyTime(chain.ShiftStartUtc, timeZone),
        ToCompanyTime(chain.DeadlineUtc, timeZone),
        chain.WorkId,
        chain.AcknowledgedByUserName,
        ToCompanyTime(chain.AcknowledgedAtUtc, timeZone),
        chain.CancelledByUserName,
        mayReadCancelReason ? chain.CancelReason : null,
        chain.OutcomeReason,
        chain.Stages.OrderBy(s => s.Rank).Select(s => new StageView(
            s.Rank,
            s.UserDisplayName,
            s.Status.ToString(),
            ToCompanyTime(s.NotifiedAtUtc, timeZone),
            ToCompanyTime(s.RespondedAtUtc, timeZone),
            s.DeliveryChannel)).ToList());

    private static string? ToCompanyTime(DateTime? utc, TimeZoneInfo timeZone) =>
        utc is DateTime value
            ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(value, DateTimeKind.Utc), timeZone)
                .ToString(DateTimeFormat, CultureInfo.InvariantCulture)
            : null;

    private static string DisplayName(Client client) => $"{client.FirstName} {client.Name}".Trim();

    private sealed record SlotKey(Guid? AnalyseToken, DateOnly Date, Guid ShiftId);

    private sealed record SlotOutcome(ReplacementSlotStatus Status, int TotalEntries);

    public sealed record SlotView(
        string Date,
        string Weekday,
        string StartTime,
        string EndTime,
        Guid ShiftId,
        string? ShiftName,
        Guid? ScenarioToken,
        bool IsScenario,
        string? AbsenceType,
        string Status,
        int HiddenEntries,
        bool AppliedToPlan,
        List<CandidateView> Candidates);

    public sealed record CandidateView(
        Guid ClientId,
        string? Name,
        string Source,
        string Contact,
        string Outcome,
        string? AnsweredAt,
        bool AppliedToPlan,
        bool IsShortNotice);

    public sealed record ChainView(
        Guid Id,
        string Status,
        string? ShiftStart,
        string? Deadline,
        Guid? WorkId,
        string? TakenOverBy,
        string? TakenOverAt,
        string? CancelledBy,
        string? CancelReason,
        string? OutcomeReason,
        List<StageView> WokenPlanners);

    public sealed record StageView(
        int Rank,
        string PlannerName,
        string Status,
        string? NotifiedAt,
        string? RespondedAt,
        string? DeliveryChannel);
}
