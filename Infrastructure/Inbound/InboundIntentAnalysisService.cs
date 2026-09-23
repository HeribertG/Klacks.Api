// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Classifies one inbound message (email or messenger) from a known client into a planning intent.
/// Customer messages always classify as CustomerMessage (summary only); employee/extern messages run
/// through the LLM to detect work cancellations, vacation requests, day-off wishes, availability
/// announcements and shift-slot preferences, including the affected date range, hour window, weekday
/// pattern and schedule command keywords, and whether the message is too unclear to act on without
/// asking the sender back (needsClarification plus a draft question). The LLM call goes through
/// IOneShotCompletionService (a single pipeline-free completion) and deliberately NOT through
/// ILLMService: the chat pipeline ran the recipe engine on this prompt and wrote the foreign message
/// into the first admin's conversation history and auto-memory. The Date line handed to the model is
/// the company-local calendar day of the received instant (via ICompanyClock). A work cancellation
/// without any date ("I am sick") is assumed to concern that received day with low confidence, so the
/// action orchestrator only suggests and never executes it; such a result is flagged DateAssumed
/// (not persisted) so the notifier and the clarification composer do not treat the day as stated. When only an until date parses (e.g. "sick
/// until Friday"), a later-than-default until date is kept and the from date defaults to the received
/// day; an until date before the default day is also defaulted, still with low confidence.
/// AnalyzeAnswerAsync re-analyses an answered clarification from original message, question and answer;
/// its result belongs to the answer source, never carries a clarification question (there is no further
/// round), and is forced to low confidence whenever the answer is still unclear (needsClarification
/// stays true), so an unresolved clarification can never auto-execute. Client resolution and the
/// enabled/disabled switch are the caller's responsibility; this service always returns a result, never
/// null: a failed LLM call or an unparsable reply degrades to Intent=Other/Confidence=Low (customer:
/// CustomerMessage/High) with the failure recorded.
/// </summary>
/// <param name="completionService">Runs the single tool-free LLM completion</param>
/// <param name="keywordProvider">Supplies the currently configured schedule command keywords</param>
/// <param name="companyClock">Resolves the company time zone for the received date</param>
/// <param name="logger">Logs every attempt with its raw reply and every failure</param>

using System.Globalization;
using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Inbound;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Inbound;

public class InboundIntentAnalysisService : IInboundIntentAnalysisService
{
    private const int MaxBodyLengthForLlm = 4000;
    private const int MaxLlmAttempts = 2;
    private const int RawReplyLogLength = 1000;
    private const int MaxSummaryLength = 2000;
    private const int MaxUnparsedSummaryLength = 500;
    private const string LlmCallFailedPrefix = "LLM call failed: ";
    private const string ReceivedDateFormat = "yyyy-MM-dd";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString
    };

    private readonly IOneShotCompletionService _completionService;
    private readonly IScheduleCommandKeywordProvider _keywordProvider;
    private readonly ICompanyClock _companyClock;
    private readonly ILogger<InboundIntentAnalysisService> _logger;

    public InboundIntentAnalysisService(
        IOneShotCompletionService completionService,
        IScheduleCommandKeywordProvider keywordProvider,
        ICompanyClock companyClock,
        ILogger<InboundIntentAnalysisService> logger)
    {
        _completionService = completionService;
        _keywordProvider = keywordProvider;
        _companyClock = companyClock;
        _logger = logger;
    }

    public async Task<InboundAnalysis> AnalyzeAsync(
        Guid clientId, EntityTypeEnum clientType, InboundSource source, CancellationToken cancellationToken = default)
    {
        var analysis = NewAnalysis(clientId, clientType, source);

        try
        {
            var configuredKeywords = await _keywordProvider.GetAsync(cancellationToken);
            var companyTimeZone = await _companyClock.GetTimeZoneAsync(cancellationToken);
            var receivedDate = ToCompanyLocalDate(source.ReceivedAt, companyTimeZone);
            var prompt = BuildPrompt(source, clientType, TruncateBody(source.Body), receivedDate, configuredKeywords);
            await RunExtractionAsync(analysis, clientType, source, prompt, receivedDate, configuredKeywords, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inbound intent analysis failed for {Channel} source {SourceId}", source.Channel, source.SourceId);
            ApplyFailure(analysis, clientType, source, ex.Message);
        }

        return analysis;
    }

    public async Task<InboundAnalysis> AnalyzeAnswerAsync(
        Guid clientId,
        EntityTypeEnum clientType,
        InboundSource answerSource,
        ClarificationHistory history,
        CancellationToken cancellationToken = default)
    {
        var analysis = NewAnalysis(clientId, clientType, answerSource);

        try
        {
            var configuredKeywords = await _keywordProvider.GetAsync(cancellationToken);
            var companyTimeZone = await _companyClock.GetTimeZoneAsync(cancellationToken);
            var originalDate = ToCompanyLocalDate(history.OriginalReceivedAt, companyTimeZone);
            var answerDate = ToCompanyLocalDate(answerSource.ReceivedAt, companyTimeZone);
            var systemPrompt = BuildSystemPrompt(clientType, configuredKeywords) + InboundClarificationPromptParts.AnswerInstructions;
            var userMessage = InboundClarificationPromptParts.BuildAnswerUserMessage(
                answerSource.SenderDisplay,
                TruncateBody(history.OriginalText),
                FormatDateLine(originalDate),
                history.Question,
                TruncateBody(answerSource.Body),
                FormatDateLine(answerDate));
            await RunExtractionAsync(
                analysis, clientType, answerSource, (systemPrompt, userMessage), originalDate, configuredKeywords, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Inbound answer analysis failed for {Channel} source {SourceId}", answerSource.Channel, answerSource.SourceId);
            ApplyFailure(analysis, clientType, answerSource, ex.Message);
        }

        analysis.ClarificationQuestion = null;
        if (analysis.NeedsClarification)
        {
            analysis.Confidence = EmailConfidence.Low;
        }

        return analysis;
    }

    private async Task RunExtractionAsync(
        InboundAnalysis analysis,
        EntityTypeEnum clientType,
        InboundSource source,
        (string SystemPrompt, string UserMessage) prompt,
        DateOnly defaultDate,
        ScheduleCommandKeywordSet keywords,
        CancellationToken cancellationToken)
    {
        var reply = string.Empty;
        LlmReply? parsed = null;
        for (var attempt = 1; attempt <= MaxLlmAttempts && parsed == null; attempt++)
        {
            var completion = await _completionService.CompleteAsync(
                prompt.SystemPrompt, prompt.UserMessage, null, cancellationToken);
            if (!completion.Success)
            {
                _logger.LogWarning(
                    "Inbound intent analysis LLM call failed for {Channel} source {SourceId}: {Error}",
                    source.Channel, source.SourceId, completion.Error);
                ApplyFailure(analysis, clientType, source, LlmCallFailedPrefix + completion.Error);
                return;
            }

            reply = completion.Content;
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

        ApplyParsedReply(analysis, clientType, parsed, reply, keywords, defaultDate);
    }

    private static InboundAnalysis NewAnalysis(Guid clientId, EntityTypeEnum clientType, InboundSource source) => new()
    {
        SourceKind = source.SourceKind,
        SourceId = source.SourceId,
        Channel = source.Channel,
        ClientId = clientId,
        ClientType = clientType,
        AnalyzedAt = DateTime.UtcNow
    };

    /// <summary>
    /// Converts the received instant to the company's local calendar day. A Kind=Unspecified value is
    /// read as UTC (both channels store UTC; Npgsql returns timestamptz as UTC).
    /// </summary>
    /// <param name="receivedAt">The instant the message was received</param>
    /// <param name="companyTimeZone">The company's configured time zone</param>
    internal static DateOnly ToCompanyLocalDate(DateTime receivedAt, TimeZoneInfo companyTimeZone)
    {
        var utc = receivedAt.Kind switch
        {
            DateTimeKind.Utc => receivedAt,
            DateTimeKind.Local => receivedAt.ToUniversalTime(),
            _ => DateTime.SpecifyKind(receivedAt, DateTimeKind.Utc)
        };

        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, companyTimeZone));
    }

    internal static string FormatDateLine(DateOnly date) =>
        $"{date.ToString(ReceivedDateFormat, CultureInfo.InvariantCulture)} ({date.DayOfWeek})";

    private static string TruncateBody(string body) =>
        body.Length > MaxBodyLengthForLlm ? body[..MaxBodyLengthForLlm] : body;

    private static void ApplyFailure(InboundAnalysis analysis, EntityTypeEnum clientType, InboundSource source, string failureReason)
    {
        analysis.Intent = clientType == EntityTypeEnum.Customer ? EmailIntent.CustomerMessage : EmailIntent.Other;
        analysis.Confidence = clientType == EntityTypeEnum.Customer ? EmailConfidence.High : EmailConfidence.Low;
        analysis.Summary = source.Subject ?? string.Empty;
        analysis.FailureReason = failureReason;
    }

    /// <summary>
    /// Builds the two halves of the extraction call: all instructions, the JSON schema and the rules go
    /// into the system prompt; the user message carries only the inbound data (From/Date/Subject/Body).
    /// </summary>
    /// <param name="source">The inbound message whose sender, date and subject are rendered</param>
    /// <param name="clientType">Customer or employee/extern, named in the instructions</param>
    /// <param name="body">The (already truncated) message body</param>
    /// <param name="receivedDate">Company-local calendar day the message was received (see ToCompanyLocalDate)</param>
    /// <param name="keywords">The configured schedule command keywords the model may emit</param>
    internal static (string SystemPrompt, string UserMessage) BuildPrompt(
        InboundSource source, EntityTypeEnum clientType, string body, DateOnly receivedDate, ScheduleCommandKeywordSet keywords)
    {
        var subjectLine = string.IsNullOrWhiteSpace(source.Subject) ? string.Empty : $"Subject: {source.Subject}\n";
        var userMessage = $"From: {source.SenderDisplay}\nDate: {FormatDateLine(receivedDate)}\n{subjectLine}Body: {body}";
        return (BuildSystemPrompt(clientType, keywords), userMessage);
    }

    internal static string BuildSystemPrompt(EntityTypeEnum clientType, ScheduleCommandKeywordSet keywords)
    {
        var senderKind = clientType == EntityTypeEnum.Customer ? "customer" : "employee";
        return
            "Analyze the message in the user turn, sent to a workforce-planning system by a known " + senderKind + ".\n" +
            "This is a single non-conversational data-extraction call: there are no tools or functions " +
            "available to you here, nothing else reads a text reply, and no further turn will follow. " +
            "Do not explain your reasoning, ask questions, mention tools, or write any text outside the " +
            "object. Your entire response must be exactly one JSON object and nothing else, in this " +
            "exact shape:\n" +
            "{\"intent\":\"CustomerMessage|WorkCancellation|VacationRequest|DayOffWish|AvailabilityAnnouncement|ShiftPreference|Other\"," +
            "\"confidence\":\"high|low\"," +
            "\"summary\":\"2-3 sentence summary in the language of the message\"," +
            "\"fromDate\":\"yyyy-MM-dd or null\",\"untilDate\":\"yyyy-MM-dd or null\"," +
            "\"startHour\":\"0-23 or null\",\"endHour\":\"0-23 or null\",\"weekdays\":\"ISO weekday numbers 1-7 comma-separated (1=Monday) or null\"," +
            $"\"scheduleCommands\":\"comma-separated keywords from {keywords.FreeToken},{keywords.NegFreeToken}," +
            $"{keywords.EarlyToken},{keywords.NegEarlyToken},{keywords.LateToken},{keywords.NegLateToken}," +
            $"{keywords.NightToken},{keywords.NegNightToken} or null\"" +
            InboundClarificationPromptParts.SchemaFields + "}\n" +
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
            "otherwise unclear. CustomerMessage is always high. Resolve relative date expressions (e.g. " +
            "today, tomorrow, next Monday, in any language) against the Date line of the message, which " +
            "is the day the message was received in the company's local time zone." +
            InboundClarificationPromptParts.Rules;
    }

    private static void ApplyParsedReply(
        InboundAnalysis analysis,
        EntityTypeEnum clientType,
        LlmReply? parsed,
        string rawReply,
        ScheduleCommandKeywordSet keywords,
        DateOnly defaultDate)
    {
        if (parsed == null)
        {
            analysis.Intent = clientType == EntityTypeEnum.Customer ? EmailIntent.CustomerMessage : EmailIntent.Other;
            analysis.Confidence = EmailConfidence.Low;
            analysis.Summary = Truncate(rawReply, MaxUnparsedSummaryLength);
            analysis.FailureReason = $"LLM reply was not parsable JSON after {MaxLlmAttempts} attempts";
            return;
        }

        analysis.Intent = clientType == EntityTypeEnum.Customer
            ? EmailIntent.CustomerMessage
            : MapIntent(parsed.Intent);
        analysis.Confidence = clientType == EntityTypeEnum.Customer
            ? EmailConfidence.High
            : MapConfidence(parsed.Confidence);
        analysis.Summary = Truncate(parsed.Summary ?? string.Empty, MaxSummaryLength);
        analysis.FromDate = TryParseDate(parsed.FromDate);
        analysis.UntilDate = TryParseDate(parsed.UntilDate);

        var (startHour, endHour) = NormalizeHourWindow(parsed.StartHour, parsed.EndHour);
        analysis.StartHour = startHour;
        analysis.EndHour = endHour;
        analysis.Weekdays = NormalizeWeekdays(parsed.Weekdays);
        analysis.ScheduleCommands = NormalizeScheduleCommands(parsed.ScheduleCommands, keywords);

        analysis.NeedsClarification = clientType != EntityTypeEnum.Customer && ReadFlag(parsed.NeedsClarification);
        analysis.ClarificationQuestion = analysis.NeedsClarification && !string.IsNullOrWhiteSpace(parsed.ClarificationQuestion)
            ? Truncate(parsed.ClarificationQuestion.Trim(), InboundClarificationConstants.MaxDraftQuestionLength)
            : null;

        if (analysis.Intent == EmailIntent.WorkCancellation && analysis.FromDate == null)
        {
            analysis.FromDate = defaultDate;
            analysis.UntilDate = analysis.UntilDate >= defaultDate ? analysis.UntilDate : defaultDate;
            analysis.Confidence = EmailConfidence.Low;
            analysis.DateAssumed = true;
        }
    }

    private static bool ReadFlag(JsonElement? element) => element switch
    {
        { ValueKind: JsonValueKind.True } => true,
        { ValueKind: JsonValueKind.String } text => bool.TryParse(text.GetString(), out var flag) && flag,
        _ => false
    };

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
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, out var date) ? date : null;

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

        public JsonElement? NeedsClarification { get; set; }

        public string? ClarificationQuestion { get; set; }
    }
}
