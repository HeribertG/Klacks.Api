// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Classifies an incoming email from a known client into a planning intent. Resolves the sender
/// via the Communication lookup; unassigned senders are not analyzed. Customers always classify
/// as CustomerMessage (summary only); employee/extern emails run through the LLM to detect work
/// cancellations, vacation requests, day-off wishes, availability announcements and shift-slot
/// preferences (only/no early, late or night duty) including the affected date range, hour window,
/// weekday pattern and schedule command keywords. The LLM also rates its own confidence
/// (high/low) in the extracted intent, which the orchestrator uses to withhold automatic
/// actions on ambiguous mail. A failed or unparsable LLM reply degrades to Intent=Other /
/// Confidence=Low with the failure recorded — never an exception.
/// </summary>

using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Infrastructure.Email;

public class EmailIntentAnalysisService : IEmailIntentAnalysisService
{
    private const int MaxBodyLengthForLlm = 4000;
    private const int MaxLlmAttempts = 2;
    private const int RawReplyLogLength = 1000;

    private static readonly HashSet<string> ValidScheduleCommandKeywords = new(StringComparer.Ordinal)
    {
        "EARLY", "-EARLY", "LATE", "-LATE", "NIGHT", "-NIGHT"
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };

    private readonly IEmailClientAssignmentService _assignmentService;
    private readonly IPlanningAudienceResolver _audienceResolver;
    private readonly ILLMService _llmService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<EmailIntentAnalysisService> _logger;

    public EmailIntentAnalysisService(
        IEmailClientAssignmentService assignmentService,
        IPlanningAudienceResolver audienceResolver,
        ILLMService llmService,
        ISettingsRepository settingsRepository,
        ILogger<EmailIntentAnalysisService> logger)
    {
        _assignmentService = assignmentService;
        _audienceResolver = audienceResolver;
        _llmService = llmService;
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    public async Task<EmailAnalysis?> AnalyzeAsync(ReceivedEmail email, CancellationToken cancellationToken = default)
    {
        if (!await IsEnabledAsync())
        {
            return null;
        }

        var client = await _assignmentService.ResolveClientAsync(email, cancellationToken);
        if (client == null)
        {
            return null;
        }

        var (clientId, clientType) = client.Value;
        var analysis = new EmailAnalysis
        {
            ReceivedEmailId = email.Id,
            ClientId = clientId,
            ClientType = clientType,
            AnalyzedAt = DateTime.UtcNow
        };

        try
        {
            var reply = string.Empty;
            LlmReply? parsed = null;
            for (var attempt = 1; attempt <= MaxLlmAttempts && parsed == null; attempt++)
            {
                reply = await RunLlmAsync(email, clientType, cancellationToken);
                parsed = ParseReply(reply);
                _logger.LogInformation(
                    "Email intent analysis attempt {Attempt}/{Max} for email {EmailId}: parsed={Parsed}, raw reply: {Reply}",
                    attempt, MaxLlmAttempts, email.Id, parsed != null, Truncate(reply, RawReplyLogLength));
                if (parsed == null && attempt < MaxLlmAttempts)
                {
                    _logger.LogWarning(
                        "Email intent analysis attempt {Attempt}/{Max} returned unparsable JSON for email {EmailId}, retrying",
                        attempt, MaxLlmAttempts, email.Id);
                }
            }

            ApplyParsedReply(analysis, clientType, parsed, reply);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email intent analysis failed for email {EmailId}", email.Id);
            analysis.Intent = clientType == EntityTypeEnum.Customer ? EmailIntent.CustomerMessage : EmailIntent.Other;
            analysis.Confidence = clientType == EntityTypeEnum.Customer ? EmailConfidence.High : EmailConfidence.Low;
            analysis.Summary = email.Subject;
            analysis.FailureReason = ex.Message;
        }

        return analysis;
    }

    private async Task<string> RunLlmAsync(ReceivedEmail email, EntityTypeEnum clientType, CancellationToken cancellationToken)
    {
        var body = email.BodyText ?? email.BodyHtml ?? string.Empty;
        if (body.Length > MaxBodyLengthForLlm)
        {
            body = body[..MaxBodyLengthForLlm];
        }

        var context = new LLMContext
        {
            Message = BuildPrompt(email, clientType, body),
            UserId = await _audienceResolver.GetFirstAdminUserIdAsync(cancellationToken),
            IsNonConversational = true
        };

        var response = await _llmService.ProcessAsync(context);
        return response.Message;
    }

    internal static string BuildPrompt(ReceivedEmail email, EntityTypeEnum clientType, string body)
    {
        var senderKind = clientType == EntityTypeEnum.Customer ? "customer" : "employee";
        return
            "Analyze this email sent to a workforce-planning system by a known " + senderKind + ".\n" +
            "This is a single non-conversational data-extraction call: there are no tools or functions " +
            "available to you here, nothing else reads a text reply, and no further turn will follow. " +
            "Do not explain your reasoning, ask questions, mention tools, or add any text outside the " +
            "object. Your entire response must be exactly one JSON object and nothing else, in this " +
            "exact shape:\n" +
            "{\"intent\":\"CustomerMessage|WorkCancellation|VacationRequest|DayOffWish|AvailabilityAnnouncement|ShiftPreference|Other\"," +
            "\"confidence\":\"high|low\"," +
            "\"summary\":\"2-3 sentence summary in the language of the email\"," +
            "\"fromDate\":\"yyyy-MM-dd or null\",\"untilDate\":\"yyyy-MM-dd or null\"," +
            "\"startHour\":\"0-23 or null\",\"endHour\":\"0-23 or null\",\"weekdays\":\"ISO weekday numbers 1-7 comma-separated (1=Monday) or null\"," +
            "\"scheduleCommands\":\"comma-separated keywords from EARLY,-EARLY,LATE,-LATE,NIGHT,-NIGHT or null\"}\n" +
            "Rules: a customer email is always intent CustomerMessage. WorkCancellation = the sender " +
            "cancels or cannot attend already planned work (sick, no-show, emergency). VacationRequest = " +
            "the sender asks for vacation/holidays. DayOffWish = the sender wishes specific days or a " +
            "period free without a formal vacation request. AvailabilityAnnouncement = the sender states " +
            "when they CAN work in a future period as clock-time windows (e.g. 08:00-17:00). " +
            "ShiftPreference = the sender restricts which shift slots they can or cannot work on specific " +
            "dates: morning/early shift, evening/late shift or night shift. Map to scheduleCommands: can " +
            "ONLY work mornings = EARLY, cannot work mornings = -EARLY, only evenings = LATE, no evenings " +
            "= -LATE, only nights = NIGHT, no nights = -NIGHT; 'only mornings or evenings' = -NIGHT. " +
            "Whole days completely free = DayOffWish, clock-time windows = AvailabilityAnnouncement, " +
            "shift-slot restrictions = ShiftPreference. Use Other when none fits. " +
            "fromDate/untilDate cover the affected period when dates or ranges are mentioned (a single day " +
            "has fromDate = untilDate); use null when no date is identifiable. startHour/endHour describe " +
            "the daily availability window as full hours, endHour inclusive (available 08:00-17:00 means " +
            "startHour 8, endHour 16); weekdays lists the recurring days when mentioned (e.g. Mon-Fri = " +
            "\"1,2,3,4,5\"). Use null for startHour/endHour/weekdays/scheduleCommands when not stated or " +
            "not applicable. confidence = high only when the email states a concrete, unambiguous " +
            "date/period, time window or shift-slot restriction as an actual statement of intent; use low " +
            "when the topic is only mentioned in passing, phrased as a question, hypothetical, or " +
            "otherwise unclear. CustomerMessage is always high.\n\n" +
            $"From: {email.FromAddress}\nDate: {email.ReceivedDate:yyyy-MM-dd}\nSubject: {email.Subject}\nBody: {body}";
    }

    private static void ApplyParsedReply(EmailAnalysis analysis, EntityTypeEnum clientType, LlmReply? parsed, string rawReply)
    {
        if (parsed == null)
        {
            analysis.Intent = clientType == EntityTypeEnum.Customer ? EmailIntent.CustomerMessage : EmailIntent.Other;
            analysis.Confidence = EmailConfidence.Low;
            analysis.Summary = Truncate(rawReply, 500);
            analysis.FailureReason = $"LLM reply was not parsable JSON after {MaxLlmAttempts} attempts";
            return;
        }

        analysis.Intent = clientType == EntityTypeEnum.Customer
            ? EmailIntent.CustomerMessage
            : MapIntent(parsed.Intent);
        analysis.Confidence = clientType == EntityTypeEnum.Customer
            ? EmailConfidence.High
            : MapConfidence(parsed.Confidence);
        analysis.Summary = Truncate(parsed.Summary ?? string.Empty, 2000);
        analysis.FromDate = TryParseDate(parsed.FromDate);
        analysis.UntilDate = TryParseDate(parsed.UntilDate);

        var (startHour, endHour) = NormalizeHourWindow(parsed.StartHour, parsed.EndHour);
        analysis.StartHour = startHour;
        analysis.EndHour = endHour;
        analysis.Weekdays = NormalizeWeekdays(parsed.Weekdays);
        analysis.ScheduleCommands = NormalizeScheduleCommands(parsed.ScheduleCommands);
    }

    private static string? NormalizeScheduleCommands(string? scheduleCommands)
    {
        if (string.IsNullOrWhiteSpace(scheduleCommands))
        {
            return null;
        }

        var keywords = scheduleCommands
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => token.ToUpperInvariant())
            .Where(ValidScheduleCommandKeywords.Contains)
            .Distinct()
            .ToList();

        return keywords.Count > 0 ? string.Join(',', keywords) : null;
    }

    private static (int? StartHour, int? EndHour) NormalizeHourWindow(int? startHour, int? endHour)
    {
        if (startHour is < 0 or > 23 || endHour is < 0 or > 23)
        {
            return (null, null);
        }

        if (startHour != null && endHour != null && startHour > endHour)
        {
            return (null, null);
        }

        return (startHour, endHour);
    }

    private static string? NormalizeWeekdays(string? weekdays)
    {
        if (string.IsNullOrWhiteSpace(weekdays))
        {
            return null;
        }

        var days = weekdays
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => int.TryParse(token, out var day) ? day : -1)
            .Where(day => day is >= 1 and <= 7)
            .Distinct()
            .OrderBy(day => day)
            .ToList();

        return days.Count > 0 ? string.Join(',', days) : null;
    }

    internal static LlmReply? ParseReply(string reply)
    {
        var start = reply.IndexOf('{');
        var end = reply.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<LlmReply>(reply[start..(end + 1)], JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static EmailIntent MapIntent(string? intent) => intent?.Trim().ToLowerInvariant() switch
    {
        "customermessage" => EmailIntent.CustomerMessage,
        "workcancellation" => EmailIntent.WorkCancellation,
        "vacationrequest" => EmailIntent.VacationRequest,
        "dayoffwish" => EmailIntent.DayOffWish,
        "availabilityannouncement" => EmailIntent.AvailabilityAnnouncement,
        "shiftpreference" => EmailIntent.ShiftPreference,
        _ => EmailIntent.Other
    };

    private static EmailConfidence MapConfidence(string? confidence) => confidence?.Trim().ToLowerInvariant() switch
    {
        "high" => EmailConfidence.High,
        "low" => EmailConfidence.Low,
        _ => EmailConfidence.Unknown
    };

    private static DateOnly? TryParseDate(string? value) =>
        DateOnly.TryParse(value, System.Globalization.CultureInfo.InvariantCulture, out var date) ? date : null;

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private async Task<bool> IsEnabledAsync()
    {
        var setting = await _settingsRepository.GetSetting(Settings.EMAIL_ANALYSIS_ENABLED);
        return setting?.Value != null && bool.TryParse(setting.Value, out var enabled) && enabled;
    }

    internal sealed class LlmReply
    {
        public string? Intent { get; set; }

        public string? Confidence { get; set; }

        public string? Summary { get; set; }

        public string? FromDate { get; set; }

        public string? UntilDate { get; set; }

        public int? StartHour { get; set; }

        public int? EndHour { get; set; }

        public string? Weekdays { get; set; }

        public string? ScheduleCommands { get; set; }
    }
}
