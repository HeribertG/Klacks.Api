// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Routes an analyzed employee/extern email intent to the matching planning action under the
/// email-flow autonomy mapping, which is deliberately STRICTER than the regular skill gate because
/// the trigger is an LLM reading of a third-party email, not a deliberate user request:
/// FullyAutonomous executes everything; Autonomous executes only the cover-absence scenario
/// (propose-only by design) and suggests the rest; Assisted/Propose only suggests. The three
/// schedule-writing actions (FREE commands, EARLY/LATE/NIGHT shift-slot commands, availability
/// slots) additionally require the employee to hold a zero-hour contract — for guaranteed-hours
/// contracts they are always suggested, never executed. The effective level is the MINIMUM over
/// all admin users (no admins = suggest only), additionally capped by the global proactive autonomy
/// level (KLACKSY_PROACTIVE_AUTONOMY_LEVEL), so from a global level below Autonomous nothing here
/// executes automatically no matter what the admins chose. Regardless of autonomy level, an action is only
/// executed when the LLM rated its own analysis as high-confidence; low or unknown confidence
/// always degrades to a suggestion. The vacation/availability/keyword-command actions (vacation,
/// day-off wish, availability, shift preference) additionally require the affected period to have
/// NO scheduled shifts yet — they only execute automatically in periods that are not yet planned.
/// Conversely, work cancellation (sickness/accident) is expected to hit an already-planned period
/// and is only blocked when the period is already sealed. Executed skills run under the first
/// admin's identity with the audit name "Klacksy email-analysis" and bypass the interactive autonomy
/// gate, because no one is present to confirm. This mapping is therefore the FIRST gate for the flow,
/// not the only one: every execution additionally passes IUnattendedSkillPolicy, the same fail-closed
/// check the scheduled-task and proactive paths use. The mapping decides WHETHER an intent may act at
/// all; the policy re-checks what the skill has since become — a skill reclassified as sensitive, or
/// removed outright, is refused here even though this mapping would still allow it.
/// </summary>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Domain.Models.Schedules;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Infrastructure.Email;

public class EmailActionOrchestrator : IEmailActionOrchestrator
{
    private const string AuditUserName = "Klacksy email-analysis";
    private const int MinHour = 0;
    private const int MaxHour = 23;
    private const int MaxAvailabilityRangeDays = 92;
    private const int MaxSealCheckRangeDays = 92;
    private const double DefaultAbsenceDailyValue = 1.0;

    private static readonly string[] SicknessKeywords = ["krank", "sick", "malad", "malatt"];
    private static readonly string[] VacationKeywords = ["ferien", "urlaub", "vacation", "holiday", "vacanc", "vacanz", "congé"];

    // Training is not its own EmailIntent: the LLM classifies a course request as VacationRequest too.
    // The absence type is picked from the wording instead, and the two keyword sets stay separate
    // because ResolveAbsenceByKeywords demands exactly one match - a merged list would match both the
    // vacation and the training type in any installation that has both, and record nothing at all.
    private static readonly string[] TrainingKeywords =
        ["schulung", "weiterbildung", "fortbildung", "kurs", "training", "course", "formation", "corso"];

    private readonly IAgentAutonomyPreferenceRepository _autonomyPreferences;
    private readonly IPlanningAudienceResolver _audienceResolver;
    private readonly ISkillExecutor _skillExecutor;
    private readonly IGroupMembershipService _groupMembershipService;
    private readonly Application.Interfaces.IAbsenceRepository _absenceRepository;
    private readonly Application.Interfaces.IWorkRepository _workRepository;
    private readonly ISealedDayRepository _sealedDayRepository;
    private readonly IScheduleCommandKeywordProvider _keywordProvider;
    private readonly IClientContractDataProvider _contractDataProvider;
    private readonly IEmailCapacityAdvisor _capacityAdvisor;
    private readonly IInternalTokenIssuer _internalTokenIssuer;
    private readonly IUnattendedSkillPolicy _unattendedSkillPolicy;
    private readonly IProactiveGovernanceResolver _governanceResolver;
    private readonly EmailAutomationOptions _automationOptions;
    private readonly ILogger<EmailActionOrchestrator> _logger;

    public EmailActionOrchestrator(
        IAgentAutonomyPreferenceRepository autonomyPreferences,
        IPlanningAudienceResolver audienceResolver,
        ISkillExecutor skillExecutor,
        IGroupMembershipService groupMembershipService,
        Application.Interfaces.IAbsenceRepository absenceRepository,
        Application.Interfaces.IWorkRepository workRepository,
        ISealedDayRepository sealedDayRepository,
        IScheduleCommandKeywordProvider keywordProvider,
        IClientContractDataProvider contractDataProvider,
        IEmailCapacityAdvisor capacityAdvisor,
        IInternalTokenIssuer internalTokenIssuer,
        IUnattendedSkillPolicy unattendedSkillPolicy,
        IProactiveGovernanceResolver governanceResolver,
        IOptions<EmailAutomationOptions> automationOptions,
        ILogger<EmailActionOrchestrator> logger)
    {
        _autonomyPreferences = autonomyPreferences;
        _audienceResolver = audienceResolver;
        _skillExecutor = skillExecutor;
        _groupMembershipService = groupMembershipService;
        _absenceRepository = absenceRepository;
        _workRepository = workRepository;
        _sealedDayRepository = sealedDayRepository;
        _keywordProvider = keywordProvider;
        _contractDataProvider = contractDataProvider;
        _capacityAdvisor = capacityAdvisor;
        _internalTokenIssuer = internalTokenIssuer;
        _unattendedSkillPolicy = unattendedSkillPolicy;
        _governanceResolver = governanceResolver;
        _automationOptions = automationOptions.Value;
        _logger = logger;
    }

    public async Task<EmailActionOutcome?> ExecuteAsync(
        ReceivedEmail email, EmailAnalysis analysis, CancellationToken cancellationToken = default)
    {
        if (analysis.ClientId == null
            || analysis.ClientType == EntityTypeEnum.Customer
            || analysis.Intent is not (EmailIntent.WorkCancellation or EmailIntent.VacationRequest
                or EmailIntent.DayOffWish or EmailIntent.AvailabilityAnnouncement or EmailIntent.ShiftPreference))
        {
            return null;
        }

        if (analysis.FromDate == null)
        {
            return new EmailActionOutcome(false,
                "No usable date range was detected in the email, so no planning action was prepared. " +
                "Ask Klacksy to place the action manually once the dates are known.");
        }

        var clientId = analysis.ClientId.Value;
        var fromDate = analysis.FromDate.Value;
        var untilDate = analysis.UntilDate ?? fromDate;

        var (level, executingAdminId) = await ResolveEffectiveLevelAsync(cancellationToken);

        try
        {
            return analysis.Intent switch
            {
                EmailIntent.WorkCancellation => await HandleWorkCancellationAsync(
                    clientId, fromDate, untilDate, analysis, level, executingAdminId, email, cancellationToken),
                EmailIntent.VacationRequest => await HandleVacationRequestAsync(
                    clientId, fromDate, untilDate, analysis, level, executingAdminId, email, cancellationToken),
                EmailIntent.DayOffWish => await HandleDayOffWishAsync(
                    clientId, fromDate, untilDate, analysis, level, executingAdminId, email, cancellationToken),
                EmailIntent.AvailabilityAnnouncement => await HandleAvailabilityAnnouncementAsync(
                    clientId, fromDate, untilDate, analysis, level, executingAdminId, email, cancellationToken),
                EmailIntent.ShiftPreference => await HandleShiftPreferenceAsync(
                    clientId, fromDate, untilDate, analysis, level, executingAdminId, email, cancellationToken),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email action orchestration failed for email {EmailId}", email.Id);
            return new EmailActionOutcome(false,
                $"Automatic action failed ({ex.Message}). Ask Klacksy to place the action manually.");
        }
    }

    private async Task<EmailActionOutcome> HandleWorkCancellationAsync(
        Guid clientId, DateOnly fromDate, DateOnly untilDate, EmailAnalysis analysis, AutonomyLevel level,
        Guid? executingAdminId, ReceivedEmail email, CancellationToken cancellationToken)
    {
        var suggestion =
            $"Suggested action: create a cover scenario — ask Klacksy to run cover_absence for this " +
            $"employee from {fromDate:yyyy-MM-dd} to {untilDate:yyyy-MM-dd}.";

        if (level < AutonomyLevel.Autonomous || executingAdminId == null)
        {
            return new EmailActionOutcome(false, suggestion);
        }

        if (CheckConfidenceGate(analysis, suggestion) is { } confidenceOutcome)
        {
            return confidenceOutcome;
        }

        if (IssueOutcome(await DescribeSealedPeriodIssueAsync(clientId, fromDate, untilDate, cancellationToken), suggestion) is { } sealedOutcome)
        {
            return sealedOutcome;
        }

        var groups = (await _groupMembershipService.GetClientGroupsAsync(clientId)).ToList();
        if (groups.Count != 1)
        {
            return new EmailActionOutcome(false,
                groups.Count == 0
                    ? "The employee belongs to no group, so no cover scenario could be created automatically. " + suggestion
                    : $"The employee belongs to {groups.Count} groups ({string.Join(", ", groups.Select(g => g.Name))}) — " +
                      "pick one and " + suggestion);
        }

        var absence = ResolveAbsenceByKeywords(await _absenceRepository.List(), SicknessKeywords);
        if (absence == null)
        {
            return new EmailActionOutcome(false,
                "No unambiguous sickness absence type was found, so no cover scenario was created automatically. " + suggestion);
        }

        var result = await ExecuteSkillAsync(executingAdminId.Value, email, "cover_absence", level,
            new Dictionary<string, object>
            {
                ["clientId"] = clientId,
                ["date"] = fromDate,
                ["untilDate"] = (DateOnly?)untilDate,
                ["groupId"] = groups[0].Id,
                ["absenceId"] = absence.Id
            },
            cancellationToken);

        return result.Success
            ? new EmailActionOutcome(true, $"Cover scenario created automatically: {result.Message}")
            : new EmailActionOutcome(false, $"Automatic cover scenario failed: {result.Message}. {suggestion}");
    }

    private async Task<EmailActionOutcome> HandleVacationRequestAsync(
        Guid clientId, DateOnly fromDate, DateOnly untilDate, EmailAnalysis analysis, AutonomyLevel level,
        Guid? executingAdminId, ReceivedEmail email, CancellationToken cancellationToken)
    {
        var wantsTraining = MentionsTraining(email, analysis);
        var absenceKind = wantsTraining ? "training" : "vacation";

        var absence = ResolveAbsenceByKeywords(
            await _absenceRepository.List(), wantsTraining ? TrainingKeywords : VacationKeywords);

        var dailyValue = absence is { DefaultValue: > 0 } ? absence.DefaultValue : DefaultAbsenceDailyValue;
        var capacity = await _capacityAdvisor.JudgeAsync(clientId, fromDate, untilDate, dailyValue, cancellationToken);

        var suggestion =
            $"Suggested action: record the {absenceKind} wish — ask Klacksy to run add_break_placeholder for this " +
            $"employee from {fromDate:yyyy-MM-dd} to {untilDate:yyyy-MM-dd}." +
            (capacity.Evaluated ? Environment.NewLine + capacity.Note : string.Empty);

        // A capacity gap always blocks the automatic path, whatever the autonomy level: recording the
        // placeholder is what consumes the reserve, and the planner is the one who may accept that.
        if (capacity.HasGap)
        {
            return new EmailActionOutcome(false, suggestion);
        }

        if (level < AutonomyLevel.FullyAutonomous || executingAdminId == null)
        {
            return new EmailActionOutcome(false, suggestion);
        }

        if (CheckConfidenceGate(analysis, suggestion) is { } confidenceOutcome)
        {
            return confidenceOutcome;
        }

        if (IssueOutcome(await DescribeAlreadyPlannedPeriodIssueAsync(clientId, fromDate, untilDate, cancellationToken), suggestion) is { } plannedOutcome)
        {
            return plannedOutcome;
        }

        if (absence == null)
        {
            return new EmailActionOutcome(false,
                $"No unambiguous {absenceKind} absence type was found, so the wish was not recorded automatically. " + suggestion);
        }

        var result = await ExecuteSkillAsync(executingAdminId.Value, email, "add_break_placeholder", level,
            new Dictionary<string, object>
            {
                ["clientId"] = clientId,
                ["absenceId"] = absence.Id,
                ["fromDate"] = fromDate,
                ["untilDate"] = untilDate,
                ["information"] = $"From email: {email.Subject}"
            },
            cancellationToken);

        return result.Success
            ? new EmailActionOutcome(true,
                $"{char.ToUpperInvariant(absenceKind[0])}{absenceKind[1..]} wish recorded automatically: {result.Message}" +
                (capacity.Evaluated ? Environment.NewLine + capacity.Note : string.Empty))
            : new EmailActionOutcome(false, $"Automatic placeholder failed: {result.Message}. {suggestion}");
    }

    private static bool MentionsTraining(ReceivedEmail email, EmailAnalysis analysis)
    {
        var haystack = $"{email.Subject} {analysis.Summary}".ToLowerInvariant();
        return TrainingKeywords.Any(k => haystack.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<EmailActionOutcome> HandleDayOffWishAsync(
        Guid clientId, DateOnly fromDate, DateOnly untilDate, EmailAnalysis analysis, AutonomyLevel level,
        Guid? executingAdminId, ReceivedEmail email, CancellationToken cancellationToken)
    {
        var configuredKeywords = await _keywordProvider.GetAsync(cancellationToken);
        var dayOffPairs = new (string Positive, string Negative)[]
        {
            (configuredKeywords.FreeToken, configuredKeywords.NegFreeToken)
        };
        var dayOffKeywords = (analysis.ScheduleCommands ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(k => string.Equals(k, configuredKeywords.FreeToken, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(k, configuredKeywords.NegFreeToken, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var keyword = dayOffKeywords.Count == 1 ? dayOffKeywords[0] : configuredKeywords.FreeToken;

        var suggestion =
            $"Suggested action: place {keyword} planning commands — ask Klacksy to run add_schedule_commands_range " +
            $"for this employee from {fromDate:yyyy-MM-dd} to {untilDate:yyyy-MM-dd}.";

        if (level < AutonomyLevel.FullyAutonomous || executingAdminId == null)
        {
            return new EmailActionOutcome(false, suggestion);
        }

        if (CheckConfidenceGate(analysis, suggestion) is { } confidenceOutcome)
        {
            return confidenceOutcome;
        }

        if (HasContradiction(dayOffKeywords, dayOffPairs))
        {
            return new EmailActionOutcome(false,
                $"The detected planning commands contradict each other ({string.Join(", ", dayOffKeywords)}), " +
                "so nothing was placed automatically. " + suggestion);
        }

        if (IssueOutcome(await DescribeAlreadyPlannedPeriodIssueAsync(clientId, fromDate, untilDate, cancellationToken), suggestion) is { } plannedOutcome)
        {
            return plannedOutcome;
        }

        if (IssueOutcome(await DescribeZeroHourContractIssueAsync(clientId, fromDate), suggestion) is { } contractOutcome)
        {
            return contractOutcome;
        }

        var result = await ExecuteSkillAsync(executingAdminId.Value, email, "add_schedule_commands_range", level,
            new Dictionary<string, object>
            {
                ["clientId"] = clientId,
                ["fromDate"] = fromDate,
                ["untilDate"] = untilDate,
                ["commandKeyword"] = keyword
            },
            cancellationToken);

        return result.Success
            ? new EmailActionOutcome(true, $"{keyword} planning commands placed automatically: {result.Message}")
            : new EmailActionOutcome(false, $"Automatic planning commands failed: {result.Message}. {suggestion}");
    }

    private async Task<EmailActionOutcome> HandleAvailabilityAnnouncementAsync(
        Guid clientId, DateOnly fromDate, DateOnly untilDate, EmailAnalysis analysis, AutonomyLevel level,
        Guid? executingAdminId, ReceivedEmail email, CancellationToken cancellationToken)
    {
        var hourWindow = analysis.StartHour != null || analysis.EndHour != null
            ? $", hours {analysis.StartHour ?? MinHour}-{analysis.EndHour ?? MaxHour}"
            : string.Empty;
        var suggestion =
            $"Suggested action: record the announced availability — ask Klacksy to run set_client_availability " +
            $"for this employee from {fromDate:yyyy-MM-dd} to {untilDate:yyyy-MM-dd}{hourWindow}.";

        if (level < AutonomyLevel.FullyAutonomous || executingAdminId == null)
        {
            return new EmailActionOutcome(false, suggestion);
        }

        if (CheckConfidenceGate(analysis, suggestion) is { } confidenceOutcome)
        {
            return confidenceOutcome;
        }

        if (IssueOutcome(await DescribeAlreadyPlannedPeriodIssueAsync(clientId, fromDate, untilDate, cancellationToken), suggestion) is { } plannedOutcome)
        {
            return plannedOutcome;
        }

        if (IssueOutcome(await DescribeZeroHourContractIssueAsync(clientId, fromDate), suggestion) is { } contractOutcome)
        {
            return contractOutcome;
        }

        var rangeDays = untilDate.DayNumber - fromDate.DayNumber + 1;
        if (rangeDays > MaxAvailabilityRangeDays)
        {
            return new EmailActionOutcome(false,
                $"The announced period spans {rangeDays} days (maximum {MaxAvailabilityRangeDays}), " +
                "so the availability was not recorded automatically. " + suggestion);
        }

        var parameters = new Dictionary<string, object>
        {
            ["clientId"] = clientId,
            ["isAvailable"] = true
        };

        var weekdays = ParseWeekdays(analysis.Weekdays);
        if (weekdays.Count > 0)
        {
            var dates = Enumerable.Range(0, rangeDays)
                .Select(fromDate.AddDays)
                .Where(day => weekdays.Contains(day.DayOfWeek))
                .ToList();

            if (dates.Count == 0)
            {
                return new EmailActionOutcome(false,
                    "The announced weekdays do not occur in the announced period, so the availability was " +
                    "not recorded automatically. " + suggestion);
            }

            parameters["dates"] = string.Join(",", dates.Select(d =>
                d.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture)));
        }
        else
        {
            parameters["startDate"] = fromDate;
            parameters["endDate"] = untilDate;
        }

        if (analysis.StartHour != null)
        {
            parameters["startHour"] = analysis.StartHour.Value;
        }

        if (analysis.EndHour != null)
        {
            parameters["endHour"] = analysis.EndHour.Value;
        }

        var result = await ExecuteSkillAsync(executingAdminId.Value, email, "set_client_availability", level,
            parameters, cancellationToken);

        return result.Success
            ? new EmailActionOutcome(true, $"Availability recorded automatically: {result.Message}")
            : new EmailActionOutcome(false, $"Automatic availability recording failed: {result.Message}. {suggestion}");
    }

    private async Task<EmailActionOutcome> HandleShiftPreferenceAsync(
        Guid clientId, DateOnly fromDate, DateOnly untilDate, EmailAnalysis analysis, AutonomyLevel level,
        Guid? executingAdminId, ReceivedEmail email, CancellationToken cancellationToken)
    {
        var configuredKeywords = await _keywordProvider.GetAsync(cancellationToken);
        var shiftSlotPairs = new (string Positive, string Negative)[]
        {
            (configuredKeywords.EarlyToken, configuredKeywords.NegEarlyToken),
            (configuredKeywords.LateToken, configuredKeywords.NegLateToken),
            (configuredKeywords.NightToken, configuredKeywords.NegNightToken)
        };
        var shiftSlotTokens = new HashSet<string>(
            shiftSlotPairs.SelectMany(pair => new[] { pair.Positive, pair.Negative }), StringComparer.OrdinalIgnoreCase);
        var keywords = (analysis.ScheduleCommands ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(shiftSlotTokens.Contains)
            .ToList();

        if (keywords.Count == 0)
        {
            return new EmailActionOutcome(false,
                "A shift-slot preference was detected but no unambiguous planning command could be derived. " +
                "Ask Klacksy to run add_schedule_commands_range manually once the restriction is clear.");
        }

        var keywordList = string.Join(", ", keywords);
        var suggestion =
            $"Suggested action: place the planning commands {keywordList} — ask Klacksy to run " +
            $"add_schedule_commands_range for this employee from {fromDate:yyyy-MM-dd} to {untilDate:yyyy-MM-dd}.";

        if (level < AutonomyLevel.FullyAutonomous || executingAdminId == null)
        {
            return new EmailActionOutcome(false, suggestion);
        }

        if (CheckConfidenceGate(analysis, suggestion) is { } confidenceOutcome)
        {
            return confidenceOutcome;
        }

        if (HasContradiction(keywords, shiftSlotPairs))
        {
            return new EmailActionOutcome(false,
                $"The detected planning commands contradict each other ({keywordList}), so nothing was " +
                "placed automatically. " + suggestion);
        }

        if (IssueOutcome(await DescribeAlreadyPlannedPeriodIssueAsync(clientId, fromDate, untilDate, cancellationToken), suggestion) is { } plannedOutcome)
        {
            return plannedOutcome;
        }

        if (IssueOutcome(await DescribeZeroHourContractIssueAsync(clientId, fromDate), suggestion) is { } contractOutcome)
        {
            return contractOutcome;
        }

        var rangeDays = untilDate.DayNumber - fromDate.DayNumber + 1;
        if (rangeDays > MaxAvailabilityRangeDays)
        {
            return new EmailActionOutcome(false,
                $"The announced period spans {rangeDays} days (maximum {MaxAvailabilityRangeDays}), " +
                "so no planning commands were placed automatically. " + suggestion);
        }

        var ranges = BuildCommandDateRanges(fromDate, untilDate, ParseWeekdays(analysis.Weekdays), rangeDays);
        if (ranges.Count == 0)
        {
            return new EmailActionOutcome(false,
                "The announced weekdays do not occur in the announced period, so no planning commands were " +
                "placed automatically. " + suggestion);
        }

        var placedTotal = 0;
        foreach (var keyword in keywords)
        {
            foreach (var (rangeFrom, rangeUntil) in ranges)
            {
                var result = await ExecuteSkillAsync(executingAdminId.Value, email, "add_schedule_commands_range", level,
                    new Dictionary<string, object>
                    {
                        ["clientId"] = clientId,
                        ["fromDate"] = rangeFrom,
                        ["untilDate"] = rangeUntil,
                        ["commandKeyword"] = keyword
                    },
                    cancellationToken);

                if (!result.Success)
                {
                    return new EmailActionOutcome(false,
                        $"Automatic planning commands failed at {keyword} ({result.Message}). {suggestion}");
                }

                placedTotal += rangeUntil.DayNumber - rangeFrom.DayNumber + 1;
            }
        }

        return new EmailActionOutcome(true,
            $"Planning commands {keywordList} placed automatically on {placedTotal / keywords.Count} day(s) " +
            $"between {fromDate:yyyy-MM-dd} and {untilDate:yyyy-MM-dd}. Wizards will respect these constraints.");
    }

    private static EmailActionOutcome? CheckConfidenceGate(EmailAnalysis analysis, string suggestion) =>
        analysis.Confidence != EmailConfidence.High
            ? new EmailActionOutcome(false,
                "The email content was ambiguous or not explicit enough to act on automatically. " + suggestion)
            : null;

    private static EmailActionOutcome? IssueOutcome(string? issue, string suggestion) =>
        issue is null ? null : new EmailActionOutcome(false, issue + suggestion);

    private static bool HasContradiction(IReadOnlyCollection<string> keywords, (string Positive, string Negative)[] pairs) =>
        pairs.Any(pair =>
            keywords.Contains(pair.Positive, StringComparer.OrdinalIgnoreCase)
            && keywords.Contains(pair.Negative, StringComparer.OrdinalIgnoreCase));

    private async Task<string?> DescribeZeroHourContractIssueAsync(Guid clientId, DateOnly fromDate)
    {
        var contract = await _contractDataProvider.GetEffectiveContractDataAsync(clientId, fromDate);
        if (contract.HasActiveContract && contract.GuaranteedHours == 0)
        {
            return null;
        }

        return contract.HasActiveContract
            ? $"The employee has a contract with {contract.GuaranteedHours} guaranteed hours — this action " +
              "is only executed automatically for zero-hour contracts. "
            : "The employee has no active contract for the announced period. ";
    }

    private async Task<string?> DescribeAlreadyPlannedPeriodIssueAsync(
        Guid clientId, DateOnly fromDate, DateOnly untilDate, CancellationToken cancellationToken)
    {
        var work = await _workRepository.GetByClientAndDateRangeAsync(
            clientId, fromDate.ToDateTime(TimeOnly.MinValue), untilDate.ToDateTime(TimeOnly.MinValue), cancellationToken);

        return work.Count > 0
            ? "The employee already has scheduled shifts in this period — vacation, availability and " +
              "planning-command wishes are only executed automatically for periods that are not yet planned. "
            : null;
    }

    private async Task<string?> DescribeSealedPeriodIssueAsync(
        Guid clientId, DateOnly fromDate, DateOnly untilDate, CancellationToken cancellationToken)
    {
        var days = untilDate.DayNumber - fromDate.DayNumber + 1;
        if (days > MaxSealCheckRangeDays)
        {
            return $"The period spans {days} days (maximum {MaxSealCheckRangeDays}), so the seal check " +
                   "was not performed and the action was not executed automatically. ";
        }

        var lockedDate = await _sealedDayRepository.FindFirstLockedDateForClientAsync(fromDate, untilDate, clientId, cancellationToken);
        return lockedDate != null
            ? $"The period is already sealed as of {lockedDate:yyyy-MM-dd} — this action is only " +
              "executed automatically for periods that are not yet sealed. "
            : null;
    }

    private static List<(DateOnly From, DateOnly Until)> BuildCommandDateRanges(
        DateOnly fromDate, DateOnly untilDate, HashSet<DayOfWeek> weekdays, int rangeDays)
    {
        if (weekdays.Count == 0)
        {
            return [(fromDate, untilDate)];
        }

        var ranges = new List<(DateOnly From, DateOnly Until)>();
        var matching = Enumerable.Range(0, rangeDays)
            .Select(fromDate.AddDays)
            .Where(day => weekdays.Contains(day.DayOfWeek));

        foreach (var day in matching)
        {
            if (ranges.Count > 0 && ranges[^1].Until.AddDays(1) == day)
            {
                ranges[^1] = (ranges[^1].From, day);
            }
            else
            {
                ranges.Add((day, day));
            }
        }

        return ranges;
    }

    private static HashSet<DayOfWeek> ParseWeekdays(string? weekdays)
    {
        if (string.IsNullOrWhiteSpace(weekdays))
        {
            return [];
        }

        return weekdays
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => int.TryParse(token, out var isoDay) ? isoDay : -1)
            .Where(isoDay => isoDay is >= 1 and <= 7)
            .Select(isoDay => isoDay == 7 ? DayOfWeek.Sunday : (DayOfWeek)isoDay)
            .ToHashSet();
    }

    private async Task<SkillResult> ExecuteSkillAsync(
        Guid executingAdminId, ReceivedEmail email, string skillName, AutonomyLevel level,
        Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        // The automation acts under a configured service account when one is set, otherwise under the
        // admin it already resolved for its autonomy threshold. Either way it runs on a real token with
        // that account's CURRENT roles instead of a synthesised admin permission list, so the write is
        // attributable and a role change takes effect immediately.
        var actingUserId = ResolveActingUserId(executingAdminId);
        var token = await _internalTokenIssuer.IssueForOwnerAsync(actingUserId, cancellationToken: cancellationToken);
        if (!token.Success)
        {
            _logger.LogWarning(
                "E-mail automation skipped skill {SkillName} for email {EmailId}: {Reason}",
                skillName, email.Id, token.Reason);
            return SkillResult.Error(token.Reason!);
        }

        // Deliberate: the permissions come from the acting account's live token, the autonomy level is
        // the MINIMUM across all admins additionally capped by the global autonomy level
        // (ResolveEffectiveLevelAsync). Two different principals in one
        // request — the strictest level any admin has chosen governs, while the permission check stays
        // bound to the identity that actually performs the write.
        var decision = _unattendedSkillPolicy.Decide(new UnattendedSkillRequest(
            skillName,
            Permissions.ExpandRoles(token.Roles),
            level,
            UnattendedExecutionKind.EmailAutomation,
            AllowIrreversibleUnattended: false));
        if (!decision.Allowed)
        {
            _logger.LogWarning(
                "E-mail automation refused skill {SkillName} for email {EmailId} ({DenyReason}): {Reason}",
                skillName, email.Id, decision.DenyReason, decision.Reason);
            return SkillResult.Error(decision.Reason!);
        }

        var context = new SkillExecutionContext
        {
            UserId = actingUserId,
            TenantId = Guid.Empty,
            UserName = AuditUserName,
            UserPermissions = Permissions.ExpandRoles(token.Roles),
            AccessToken = token.Token,
            SessionId = $"email-analysis:{email.Id}",
            BypassAutonomyGate = true
        };

        return await _skillExecutor.ExecuteAsync(
            new SkillInvocation { SkillName = skillName, Parameters = parameters },
            context,
            cancellationToken);
    }

    /// <summary>
    /// Prefers the configured service account; falls back to the resolved admin when none is set, which
    /// keeps existing installations running until the account is configured.
    /// </summary>
    /// <param name="executingAdminId">The admin the autonomy threshold was resolved against</param>
    private Guid ResolveActingUserId(Guid executingAdminId)
    {
        if (Guid.TryParse(_automationOptions.ServiceAccountId, out var serviceAccountId))
        {
            return serviceAccountId;
        }

        if (!string.IsNullOrWhiteSpace(_automationOptions.ServiceAccountId))
        {
            _logger.LogWarning(
                "EmailAutomation:ServiceAccountId is set but not a Guid; falling back to the resolved admin");
        }

        return executingAdminId;
    }

    private async Task<(AutonomyLevel Level, Guid? ExecutingAdminId)> ResolveEffectiveLevelAsync(
        CancellationToken cancellationToken)
    {
        var adminIds = (await _audienceResolver.GetAdminUserIdsAsync(cancellationToken))
            .Where(id => Guid.TryParse(id, out _))
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (adminIds.Count == 0)
        {
            return (AutonomyLevel.Propose, null);
        }

        var minimum = AutonomyLevel.FullyAutonomous;
        foreach (var adminId in adminIds)
        {
            var row = await _autonomyPreferences.GetAsync(adminId, cancellationToken);
            var level = row?.Level ?? AutonomyDefaults.DefaultLevel;
            if (level < minimum)
            {
                minimum = level;
            }
        }

        var globalLevel = await _governanceResolver.GetGlobalAutonomyLevelAsync(cancellationToken);
        return (globalLevel < minimum ? globalLevel : minimum, Guid.Parse(adminIds[0]));
    }

    private static Absence? ResolveAbsenceByKeywords(IEnumerable<Absence> absences, string[] keywords)
    {
        var matches = absences
            .Where(a => !a.IsDeleted && MatchesAnyKeyword(a, keywords))
            .ToList();

        return matches.Count == 1 ? matches[0] : null;
    }

    private static bool MatchesAnyKeyword(Absence absence, string[] keywords)
    {
        var names = new[] { absence.Name?.De, absence.Name?.En, absence.Name?.Fr, absence.Name?.It };
        return names.Any(name =>
            !string.IsNullOrWhiteSpace(name) &&
            keywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase)));
    }
}
