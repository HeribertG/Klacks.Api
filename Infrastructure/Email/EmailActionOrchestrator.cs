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
/// all admin users (no admins = suggest only). Regardless of autonomy level, an action is only
/// executed when the LLM rated its own analysis as high-confidence; low or unknown confidence
/// always degrades to a suggestion. Executed skills run under the first admin's identity with the
/// audit name "Klacksy email-analysis" and bypass the regular gate — this mapping IS the gate for
/// this flow.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Email;

public class EmailActionOrchestrator : IEmailActionOrchestrator
{
    private const string AuditUserName = "Klacksy email-analysis";
    private const string FreeKeyword = "FREE";
    private const int MinHour = 0;
    private const int MaxHour = 23;
    private const int MaxAvailabilityRangeDays = 92;

    private static readonly string[] SicknessKeywords = ["krank", "sick", "malad", "malatt"];
    private static readonly string[] VacationKeywords = ["ferien", "urlaub", "vacation", "holiday", "vacanc", "vacanz", "congé"];

    private readonly IAgentAutonomyPreferenceRepository _autonomyPreferences;
    private readonly IPlanningAudienceResolver _audienceResolver;
    private readonly ISkillExecutor _skillExecutor;
    private readonly IGroupMembershipService _groupMembershipService;
    private readonly Application.Interfaces.IAbsenceRepository _absenceRepository;
    private readonly IClientContractDataProvider _contractDataProvider;
    private readonly ILogger<EmailActionOrchestrator> _logger;

    public EmailActionOrchestrator(
        IAgentAutonomyPreferenceRepository autonomyPreferences,
        IPlanningAudienceResolver audienceResolver,
        ISkillExecutor skillExecutor,
        IGroupMembershipService groupMembershipService,
        Application.Interfaces.IAbsenceRepository absenceRepository,
        IClientContractDataProvider contractDataProvider,
        ILogger<EmailActionOrchestrator> logger)
    {
        _autonomyPreferences = autonomyPreferences;
        _audienceResolver = audienceResolver;
        _skillExecutor = skillExecutor;
        _groupMembershipService = groupMembershipService;
        _absenceRepository = absenceRepository;
        _contractDataProvider = contractDataProvider;
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

        var result = await ExecuteSkillAsync(executingAdminId.Value, email, "cover_absence",
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
        var suggestion =
            $"Suggested action: record the vacation wish — ask Klacksy to run add_break_placeholder for this " +
            $"employee from {fromDate:yyyy-MM-dd} to {untilDate:yyyy-MM-dd}.";

        if (level < AutonomyLevel.FullyAutonomous || executingAdminId == null)
        {
            return new EmailActionOutcome(false, suggestion);
        }

        if (CheckConfidenceGate(analysis, suggestion) is { } confidenceOutcome)
        {
            return confidenceOutcome;
        }

        var absence = ResolveAbsenceByKeywords(await _absenceRepository.List(), VacationKeywords);
        if (absence == null)
        {
            return new EmailActionOutcome(false,
                "No unambiguous vacation absence type was found, so the wish was not recorded automatically. " + suggestion);
        }

        var result = await ExecuteSkillAsync(executingAdminId.Value, email, "add_break_placeholder",
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
            ? new EmailActionOutcome(true, $"Vacation wish recorded automatically: {result.Message}")
            : new EmailActionOutcome(false, $"Automatic placeholder failed: {result.Message}. {suggestion}");
    }

    private async Task<EmailActionOutcome> HandleDayOffWishAsync(
        Guid clientId, DateOnly fromDate, DateOnly untilDate, EmailAnalysis analysis, AutonomyLevel level,
        Guid? executingAdminId, ReceivedEmail email, CancellationToken cancellationToken)
    {
        var suggestion =
            $"Suggested action: place FREE planning commands — ask Klacksy to run add_schedule_commands_range " +
            $"for this employee from {fromDate:yyyy-MM-dd} to {untilDate:yyyy-MM-dd}.";

        if (level < AutonomyLevel.FullyAutonomous || executingAdminId == null)
        {
            return new EmailActionOutcome(false, suggestion);
        }

        if (CheckConfidenceGate(analysis, suggestion) is { } confidenceOutcome)
        {
            return confidenceOutcome;
        }

        var contractIssue = await DescribeZeroHourContractIssueAsync(clientId, fromDate);
        if (contractIssue != null)
        {
            return new EmailActionOutcome(false, contractIssue + suggestion);
        }

        var result = await ExecuteSkillAsync(executingAdminId.Value, email, "add_schedule_commands_range",
            new Dictionary<string, object>
            {
                ["clientId"] = clientId,
                ["fromDate"] = fromDate,
                ["untilDate"] = untilDate,
                ["commandKeyword"] = FreeKeyword
            },
            cancellationToken);

        return result.Success
            ? new EmailActionOutcome(true, $"FREE planning commands placed automatically: {result.Message}")
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

        var contractIssue = await DescribeZeroHourContractIssueAsync(clientId, fromDate);
        if (contractIssue != null)
        {
            return new EmailActionOutcome(false, contractIssue + suggestion);
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

        var result = await ExecuteSkillAsync(executingAdminId.Value, email, "set_client_availability",
            parameters, cancellationToken);

        return result.Success
            ? new EmailActionOutcome(true, $"Availability recorded automatically: {result.Message}")
            : new EmailActionOutcome(false, $"Automatic availability recording failed: {result.Message}. {suggestion}");
    }

    private async Task<EmailActionOutcome> HandleShiftPreferenceAsync(
        Guid clientId, DateOnly fromDate, DateOnly untilDate, EmailAnalysis analysis, AutonomyLevel level,
        Guid? executingAdminId, ReceivedEmail email, CancellationToken cancellationToken)
    {
        var keywords = (analysis.ScheduleCommands ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
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

        if (keywords.Any(k => keywords.Contains($"-{k}", StringComparer.OrdinalIgnoreCase)))
        {
            return new EmailActionOutcome(false,
                $"The detected planning commands contradict each other ({keywordList}), so nothing was " +
                "placed automatically. " + suggestion);
        }

        var contractIssue = await DescribeZeroHourContractIssueAsync(clientId, fromDate);
        if (contractIssue != null)
        {
            return new EmailActionOutcome(false, contractIssue + suggestion);
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
                var result = await ExecuteSkillAsync(executingAdminId.Value, email, "add_schedule_commands_range",
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
        Guid executingAdminId, ReceivedEmail email, string skillName,
        Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var context = new SkillExecutionContext
        {
            UserId = executingAdminId,
            TenantId = Guid.Empty,
            UserName = AuditUserName,
            UserPermissions = Permissions.GetPermissionsForRole(Roles.Admin).ToList(),
            SessionId = $"email-analysis:{email.Id}",
            BypassAutonomyGate = true
        };

        return await _skillExecutor.ExecuteAsync(
            new SkillInvocation { SkillName = skillName, Parameters = parameters },
            context,
            cancellationToken);
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

        return (minimum, Guid.Parse(adminIds[0]));
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
