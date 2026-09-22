// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Classifies one inbound message (email or messenger) from a known client into a planning intent.
/// Customer messages always classify as CustomerMessage (summary only); employee/extern messages run
/// through the LLM to detect work cancellations, vacation requests, day-off wishes, availability
/// announcements and shift-slot preferences, including the affected date range, hour window, weekday
/// pattern and schedule command keywords. Client resolution and the enabled/disabled switch are the
/// caller's responsibility (each channel has its own); this service always returns a result, never
/// null — a failed or unparsable LLM reply degrades to Intent=Other/Confidence=Low with the failure
/// recorded, never an exception.
/// </summary>

using System.Text.Json;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Inbound;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Inbound;

public class InboundIntentAnalysisService : IInboundIntentAnalysisService
{
    private const int MaxBodyLengthForLlm = 4000;
    private const int MaxLlmAttempts = 2;
    private const int RawReplyLogLength = 1000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };

    private readonly IPlanningAudienceResolver _audienceResolver;
    private readonly ILLMService _llmService;
    private readonly IScheduleCommandKeywordProvider _keywordProvider;
    private readonly ILogger<InboundIntentAnalysisService> _logger;

    public InboundIntentAnalysisService(
        IPlanningAudienceResolver audienceResolver,
        ILLMService llmService,
        IScheduleCommandKeywordProvider keywordProvider,
        ILogger<InboundIntentAnalysisService> logger)
    {
        _audienceResolver = audienceResolver;
        _llmService = llmService;
        _keywordProvider = keywordProvider;
        _logger = logger;
    }

    public async Task<InboundAnalysis> AnalyzeAsync(
        Guid clientId, EntityTypeEnum clientType, InboundSource source, CancellationToken cancellationToken = default)
    {
        var analysis = new InboundAnalysis
        {
            SourceKind = source.SourceKind,
            SourceId = source.SourceId,
            Channel = source.Channel,
            ClientId = clientId,
            ClientType = clientType,
            AnalyzedAt = DateTime.UtcNow
        };

        try
        {
            var configuredKeywords = await _keywordProvider.GetAsync(cancellationToken);
            var reply = string.Empty;
            LlmReply? parsed = null;
            for (var attempt = 1; attempt <= MaxLlmAttempts && parsed == null; attempt++)
            {
                reply = await RunLlmAsync(source, clientType, configuredKeywords, cancellationToken);
                parsed = ParseReply(reply);
                _logger.LogInformation(
                    "Inbound intent analysis attempt {Attempt}/{Max} for {Channel} source {SourceId}: parsed={Parsed}, raw reply: {Reply}",
                    attempt, MaxLlmAttempts, source.Channel, source.SourceId, parsed != null, Truncate(reply, RawReplyLogLength));
                if (parsed == null && attempt < MaxLlmAttempts)
                {
                    _logger.LogWarning(
                        "Inbound intent analysis attempt {Attempt}/{Max} returned unparsable JSON for {Channel} source {SourceId}, retrying",
                        attempt, MaxLlmAttempts, source.Channel, source.SourceId);
                }
            }

            ApplyParsedReply(analysis, clientType, parsed, reply, configuredKeywords);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inbound intent analysis failed for {Channel} source {SourceId}", source.Channel, source.SourceId);
            analysis.Intent = clientType == EntityTypeEnum.Customer ? EmailIntent.CustomerMessage : EmailIntent.Other;
            analysis.Confidence = clientType == EntityTypeEnum.Customer ? EmailConfidence.High : EmailConfidence.Low;
            analysis.Summary = source.Subject ?? string.Empty;
            analysis.FailureReason = ex.Message;
        }

        return analysis;
    }

    private async Task<string> RunLlmAsync(
        InboundSource source, EntityTypeEnum clientType, ScheduleCommandKeywordSet keywords, CancellationToken cancellationToken)
    {
        var body = source.Body;
        if (body.Length > MaxBodyLengthForLlm)
        {
            body = body[..MaxBodyLengthForLlm];
        }

        var context = new LLMContext
        {
            Message = BuildPrompt(source, clientType, body, keywords),
            UserId = await _audienceResolver.GetFirstAdminUserIdAsync(cancellationToken),
            IsNonConversational = true
        };

        var response = await _llmService.ProcessAsync(context);
        return response.Message;
    }

    internal static string BuildPrompt(InboundSource source, EntityTypeEnum clientType, string body, ScheduleCommandKeywordSet keywords)
    {
        var senderKind = clientType == EntityTypeEnum.Customer ? "customer" : "employee";
        var subjectLine = string.IsNullOrWhiteSpace(source.Subject) ? string.Empty : $"Subject: {source.Subject}\n";
        return
            "Analyze this message sent to a workforce-planning system by a known " + senderKind + ".\n" +
            "This is a single non-conversational data-extraction call: there are no tools or functions " +
            "available to you here, nothing else reads a text reply, and no further turn will follow. " +
            "Do not explain your reasoning, ask questions, mention tools, or add any text outside the " +
            "object. Your entire response must be exactly one JSON object and nothing else, in this " +
            "exact shape:\n" +
            "{\"intent\":\"CustomerMessage|WorkCancellation|VacationRequest|DayOffWish|AvailabilityAnnouncement|ShiftPreference|Other\"," +
            "\"confidence\":\"high|low\"," +
            "\"summary\":\"2-3 sentence summary in the language of the message\"," +
            "\"fromDate\":\"yyyy-MM-dd or null\",\"untilDate\":\"yyyy-MM-dd or null\"," +
            "\"startHour\":\"0-23 or null\",\"endHour\":\"0-23 or null\",\"weekdays\":\"ISO weekday numbers 1-7 comma-separated (1=Monday) or null\"," +
            $"\"scheduleCommands\":\"comma-separated keywords from {keywords.FreeToken},{keywords.NegFreeToken}," +
            $"{keywords.EarlyToken},{keywords.NegEarlyToken},{keywords.LateToken},{keywords.NegLateToken}," +
            $"{keywords.NightToken},{keywords.NegNightToken} or null\"}}\n" +
            "Rules: a customer message is always intent CustomerMessage. WorkCancellation = the sender " +
            "cancels or cannot attend a SPECIFIC shift that has already been scheduled/rostered (sick, " +
            "no-show, emergency) — the message references an existing, previously assigned shift or duty. " +
            "DayOffWish = the sender is unavailable for, or wishes free, specific future days or a period " +
            "WITHOUT referencing an already-scheduled shift and without a formal vacation request (e.g. " +
            "'I cannot work from X to Y', 'please keep me off the roster then') — map to scheduleCommands " +
            $"{keywords.FreeToken}; the opposite — wanting to work as much as possible, or preferring " +
            $"additional shifts in that period — maps to scheduleCommands {keywords.NegFreeToken}. " +
            "VacationRequest = the sender asks for vacation/holidays. AvailabilityAnnouncement = the " +
            "sender states when they CAN work in a future period as clock-time windows (e.g. 08:00-17:00). " +
            "ShiftPreference = the sender restricts which shift slots they can or cannot work on specific " +
            $"dates: morning/early shift, evening/late shift or night shift. Map to scheduleCommands: can " +
            $"ONLY work mornings = {keywords.EarlyToken}, cannot work mornings = {keywords.NegEarlyToken}, " +
            $"only evenings = {keywords.LateToken}, no evenings = {keywords.NegLateToken}, only nights = " +
            $"{keywords.NightToken}, no nights = {keywords.NegNightToken}; 'only mornings or evenings' = " +
            $"{keywords.NegNightToken}. " +
            "Whole days completely free or unavailable = DayOffWish, clock-time windows = " +
            "AvailabilityAnnouncement, shift-slot restrictions = ShiftPreference. Use Other when none fits. " +
            "fromDate/untilDate cover the affected period when dates or ranges are mentioned (a single day " +
            "has fromDate = untilDate); use null when no date is identifiable. startHour/endHour describe " +
            "the daily availability window as full hours, endHour inclusive (available 08:00-17:00 means " +
            "startHour 8, endHour 16); weekdays lists the recurring days when mentioned (e.g. Mon-Fri = " +
            "\"1,2,3,4,5\"). Use null for startHour/endHour/weekdays/scheduleCommands when not stated or " +
            "not applicable. confidence = high only when the message states a concrete, unambiguous " +
            "date/period, time window or shift-slot restriction as an actual statement of intent; use low " +
            "when the topic is only mentioned in passing, phrased as a question, hypothetical, or " +
            "otherwise unclear. CustomerMessage is always high.\n\n" +
            $"From: {source.SenderDisplay}\nDate: {source.ReceivedAt:yyyy-MM-dd}\n{subjectLine}Body: {body}";
    }

    private static void ApplyParsedReply(
        InboundAnalysis analysis, EntityTypeEnum clientType, LlmReply? parsed, string rawReply, ScheduleCommandKeywordSet keywords)
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
        analysis.ScheduleCommands = NormalizeScheduleCommands(parsed.ScheduleCommands, keywords);
    }

    private static string? NormalizeScheduleCommands(string? scheduleCommands, ScheduleCommandKeywordSet keywords)
    {
        if (string.IsNullOrWhiteSpace(scheduleCommands))
        {
            return null;
        }

        var validTokens = keywords.ValidTokens;
        var normalized = scheduleCommands
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(token => validTokens.FirstOrDefault(t => string.Equals(t, token, StringComparison.OrdinalIgnoreCase)))
            .Where(token => token != null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return normalized.Count > 0 ? string.Join(',', normalized) : null;
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
