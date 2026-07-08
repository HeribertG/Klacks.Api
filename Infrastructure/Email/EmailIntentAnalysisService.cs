// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Classifies an incoming email from a known client into a planning intent. Resolves the sender
/// via the Communication lookup; unassigned senders are not analyzed. Customers always classify
/// as CustomerMessage (summary only); employee/extern emails run through the LLM to detect work
/// cancellations, vacation requests and day-off wishes including the affected date range. A failed
/// or unparsable LLM reply degrades to Intent=Other with the failure recorded — never an exception.
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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IEmailClientAssignmentService _assignmentService;
    private readonly ILLMService _llmService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<EmailIntentAnalysisService> _logger;

    public EmailIntentAnalysisService(
        IEmailClientAssignmentService assignmentService,
        ILLMService llmService,
        ISettingsRepository settingsRepository,
        ILogger<EmailIntentAnalysisService> logger)
    {
        _assignmentService = assignmentService;
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
            var reply = await RunLlmAsync(email, clientType);
            ApplyLlmReply(analysis, clientType, reply);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Email intent analysis failed for email {EmailId}", email.Id);
            analysis.Intent = clientType == EntityTypeEnum.Customer ? EmailIntent.CustomerMessage : EmailIntent.Other;
            analysis.Summary = email.Subject;
            analysis.FailureReason = ex.Message;
        }

        return analysis;
    }

    private async Task<string> RunLlmAsync(ReceivedEmail email, EntityTypeEnum clientType)
    {
        var body = email.BodyText ?? email.BodyHtml ?? string.Empty;
        if (body.Length > MaxBodyLengthForLlm)
        {
            body = body[..MaxBodyLengthForLlm];
        }

        var context = new LLMContext
        {
            Message = BuildPrompt(email, clientType, body),
            ModelId = Settings.LLM_FALLBACK_MODEL_ID
        };

        var response = await _llmService.ProcessAsync(context);
        return response.Message;
    }

    private static string BuildPrompt(ReceivedEmail email, EntityTypeEnum clientType, string body)
    {
        var senderKind = clientType == EntityTypeEnum.Customer ? "customer" : "employee";
        return
            "Analyze this email sent to a workforce-planning system by a known " + senderKind + ".\n" +
            "Reply with ONLY a JSON object, no other text, in this exact shape:\n" +
            "{\"intent\":\"CustomerMessage|WorkCancellation|VacationRequest|DayOffWish|Other\"," +
            "\"summary\":\"2-3 sentence summary in the language of the email\"," +
            "\"fromDate\":\"yyyy-MM-dd or null\",\"untilDate\":\"yyyy-MM-dd or null\"}\n" +
            "Rules: a customer email is always intent CustomerMessage. WorkCancellation = the sender " +
            "cancels or cannot attend already planned work (sick, no-show, emergency). VacationRequest = " +
            "the sender asks for vacation/holidays. DayOffWish = the sender wishes specific days or a " +
            "period free without a formal vacation request. Use Other when none fits. fromDate/untilDate " +
            "cover the affected period when dates or ranges are mentioned (a single day has fromDate = " +
            "untilDate); use null when no date is identifiable.\n\n" +
            $"From: {email.FromAddress}\nDate: {email.ReceivedDate:yyyy-MM-dd}\nSubject: {email.Subject}\nBody: {body}";
    }

    private static void ApplyLlmReply(EmailAnalysis analysis, EntityTypeEnum clientType, string reply)
    {
        var parsed = ParseReply(reply);
        if (parsed == null)
        {
            analysis.Intent = clientType == EntityTypeEnum.Customer ? EmailIntent.CustomerMessage : EmailIntent.Other;
            analysis.Summary = Truncate(reply, 500);
            analysis.FailureReason = "LLM reply was not parsable JSON";
            return;
        }

        analysis.Intent = clientType == EntityTypeEnum.Customer
            ? EmailIntent.CustomerMessage
            : MapIntent(parsed.Intent);
        analysis.Summary = Truncate(parsed.Summary ?? string.Empty, 2000);
        analysis.FromDate = TryParseDate(parsed.FromDate);
        analysis.UntilDate = TryParseDate(parsed.UntilDate);
    }

    private static LlmReply? ParseReply(string reply)
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
        _ => EmailIntent.Other
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

    private sealed class LlmReply
    {
        public string? Intent { get; set; }

        public string? Summary { get; set; }

        public string? FromDate { get; set; }

        public string? UntilDate { get; set; }
    }
}
