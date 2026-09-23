// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Orchestrates the inbound clarification dialog around the regular analysis of one inbound message.
/// BeforeAnalysisAsync correlates an answer with the client's open clarification (per client, across
/// channels; a retry of the very message that raised the question is ignored), re-analyses original
/// message, question and answer in one pipeline-free call, and resolves the clarification conditionally
/// (Answered, or Unresolved when still unclear or the call failed: there is never a second question, and
/// an unresolved answer analysis is always low confidence so it can never auto-execute); losing the race
/// against the expiry sweep, or finding a recently expired question (email: via the In-Reply-To/References
/// headers, otherwise within RecentExpiryNoteWindowHours), only adds a note and leaves the message to the
/// regular analysis. AfterAnalysisAsync applies ClarificationPolicy to an analysis that needs
/// clarification: Ask resolves the stored personal contact (a failing lookup counts as no contact),
/// composes the question from the in-memory analysis, stores the Open clarification first (the
/// one-open-per-client index prevents a second question), sends it privately to exactly the resolved
/// target and informs the planners at once, so the adapter skips the action orchestrator; a failed send
/// marks the clarification Unresolved and tells the planners on the regular path; Suggest records the
/// question without a recipient and adds it as a note for the planners; a missing personal contact adds
/// a hint; every other failure degrades to the regular path. No exception other than cancellation
/// escapes. All instants come from ICompanyClock, so "now" and the company-local times never skew.
/// Deliberately independent of ILLMService (the re-analysis goes through IInboundIntentAnalysisService's
/// one-shot path) and never injected into a messenger observer (MessagingService sits in the LLM graph).
/// </summary>
/// <param name="settingsReader">Reads INBOUND_CLARIFICATION_ENABLED</param>
/// <param name="governanceResolver">Global autonomy level and kill switch</param>
/// <param name="clarificationRepository">Self-committing persistence of clarifications</param>
/// <param name="questionComposer">Composes and guards the question</param>
/// <param name="replySenders">One sender per channel kind (email, messenger)</param>
/// <param name="intentAnalysisService">Re-analyses an answered clarification</param>
/// <param name="analysisNotifier">Delivers planner notices</param>
/// <param name="companyClock">Current instant and company time zone for planner-facing times</param>
/// <param name="logger">Logs every degradation</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Inbound;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Inbound;
using Klacks.Api.Domain.Services.Inbound;
using AppSettings = Klacks.Api.Application.Constants.Settings;

namespace Klacks.Api.Infrastructure.Inbound;

public sealed class ClarificationCoordinator : IClarificationCoordinator
{
    private static readonly char[] MessageIdBrackets = ['<', '>'];

    private readonly ISettingsReader _settingsReader;
    private readonly IProactiveGovernanceResolver _governanceResolver;
    private readonly IInboundClarificationRepository _clarificationRepository;
    private readonly IClarificationQuestionComposer _questionComposer;
    private readonly IEnumerable<IInboundReplySender> _replySenders;
    private readonly IInboundIntentAnalysisService _intentAnalysisService;
    private readonly IInboundAnalysisNotifier _analysisNotifier;
    private readonly ICompanyClock _companyClock;
    private readonly ILogger<ClarificationCoordinator> _logger;

    public ClarificationCoordinator(
        ISettingsReader settingsReader,
        IProactiveGovernanceResolver governanceResolver,
        IInboundClarificationRepository clarificationRepository,
        IClarificationQuestionComposer questionComposer,
        IEnumerable<IInboundReplySender> replySenders,
        IInboundIntentAnalysisService intentAnalysisService,
        IInboundAnalysisNotifier analysisNotifier,
        ICompanyClock companyClock,
        ILogger<ClarificationCoordinator> logger)
    {
        _settingsReader = settingsReader;
        _governanceResolver = governanceResolver;
        _clarificationRepository = clarificationRepository;
        _questionComposer = questionComposer;
        _replySenders = replySenders;
        _intentAnalysisService = intentAnalysisService;
        _analysisNotifier = analysisNotifier;
        _companyClock = companyClock;
        _logger = logger;
    }

    /// <summary>
    /// Checks whether this message answers the client's open clarification round, independently of
    /// INBOUND_CLARIFICATION_ENABLED: the setting is only read in AfterAnalysisAsync, before a NEW
    /// question is asked. So flipping the setting off never orphans a round that is already Open —
    /// the reply is still correlated, re-analyzed and resolved (Answered/Unresolved) normally; only
    /// the ability to start a new round is gated. This is a deliberate product decision, not an
    /// oversight.
    /// </summary>
    public async Task<ClarificationPreAnalysis> BeforeAnalysisAsync(ClarificationRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ClientType == EntityTypeEnum.Customer)
        {
            return ClarificationPreAnalysis.None;
        }

        try
        {
            var open = await _clarificationRepository.GetOpenByClientAsync(request.ClientId, cancellationToken);
            if (open == null)
            {
                return await DescribeExpiredPredecessorAsync(request, cancellationToken);
            }

            if (open.OriginalSourceId == request.Source.SourceId)
            {
                return ClarificationPreAnalysis.None;
            }

            return await ResolveAnswerAsync(request, open, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Clarification answer check failed for client {ClientId}; the message is analyzed normally", request.ClientId);
            return ClarificationPreAnalysis.None;
        }
    }

    public async Task<ClarificationPostAnalysis> AfterAnalysisAsync(
        ClarificationRequest request, InboundAnalysis analysis, CancellationToken cancellationToken = default)
    {
        if (!analysis.NeedsClarification)
        {
            return ClarificationPostAnalysis.Continue;
        }

        try
        {
            if (!await IsFeatureEnabledAsync())
            {
                return ClarificationPostAnalysis.Continue;
            }

            var nowUtc = await UtcNowAsync(cancellationToken);
            var facts = await GatherFactsAsync(request, analysis, nowUtc, cancellationToken);
            var decision = ClarificationPolicy.Decide(facts);
            var sender = _replySenders.FirstOrDefault(s => s.SourceKind == request.Source.SourceKind);
            InboundReplyTarget? target = null;

            if (decision.Kind == ClarificationDecisionKind.Ask)
            {
                target = sender == null ? null : await ResolveTargetSafelyAsync(sender, request, cancellationToken);
                decision = ClarificationPolicy.Decide(facts with { HasPersonalReplyTarget = target != null });
            }

            if (decision.Kind == ClarificationDecisionKind.Ask && sender != null && target != null)
            {
                return await AskAsync(request, analysis, sender, target, nowUtc, cancellationToken);
            }

            if (decision.Kind == ClarificationDecisionKind.Suggest)
            {
                return await SuggestAsync(request, analysis, nowUtc, cancellationToken);
            }

            return decision.SkipReason == ClarificationSkipReason.NoPersonalReplyTarget
                ? ClarificationPostAnalysis.ContinueWith(ClarificationNotificationTexts.NoPersonalTarget())
                : ClarificationPostAnalysis.Continue;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Clarification for client {ClientId} failed; the message stays on the regular path", request.ClientId);
            return ClarificationPostAnalysis.Continue;
        }
    }

    internal static IReadOnlyList<string> ThreadMessageIds(ClarificationEmailThread? thread)
    {
        if (thread == null)
        {
            return [];
        }

        var references = (thread.ThreadReferences ?? string.Empty)
            .Split(default(char[]), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var latestReferences = references.Skip(Math.Max(0, references.Length - InboundClarificationConstants.MaxReferencesCount));

        return new[] { thread.InReplyTo }
            .Concat(latestReferences)
            .Select(NormalizeMessageId)
            .OfType<string>()
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private async Task<ClarificationPreAnalysis> ResolveAnswerAsync(
        ClarificationRequest request, InboundClarification open, CancellationToken cancellationToken)
    {
        var history = new ClarificationHistory(
            OriginalText: open.OriginalText,
            OriginalReceivedAt: open.OriginalReceivedAt,
            Question: open.Question,
            AskedAt: open.AskedAt);
        var answerAnalysis = await _intentAnalysisService.AnalyzeAnswerAsync(
            request.ClientId, request.ClientType, request.Source, history, cancellationToken);
        if (answerAnalysis.Id == Guid.Empty)
        {
            answerAnalysis.Id = Guid.NewGuid();
        }

        var unresolved = answerAnalysis.NeedsClarification || answerAnalysis.FailureReason != null;
        if (unresolved)
        {
            answerAnalysis.Confidence = EmailConfidence.Low;
        }

        var status = unresolved ? InboundClarificationStatus.Unresolved : InboundClarificationStatus.Answered;
        var companyTimeZone = await _companyClock.GetTimeZoneAsync(cancellationToken);
        var askedLocal = ClarificationTimeConversion.ToLocal(open.AskedAt, companyTimeZone);

        var resolved = await _clarificationRepository.TryResolveAsync(
            open.Id,
            status,
            answerSourceId: request.Source.SourceId,
            resultAnalysisId: answerAnalysis.Id,
            resolvedAtUtc: await UtcNowAsync(cancellationToken),
            cancellationToken: cancellationToken);
        if (!resolved)
        {
            _logger.LogInformation(
                "Clarification {ClarificationId} expired before the answer {SourceId} was matched; analyzing the answer normally",
                open.Id, request.Source.SourceId);
            return ClarificationPreAnalysis.Context(ClarificationNotificationTexts.AnsweredAfterExpiry(open.Question, askedLocal));
        }

        return ClarificationPreAnalysis.Answer(
            answerAnalysis,
            ClarificationNotificationTexts.AnswerContext(open.Question, askedLocal, open.OriginalText, unresolved));
    }

    private async Task<ClarificationPreAnalysis> DescribeExpiredPredecessorAsync(
        ClarificationRequest request, CancellationToken cancellationToken)
    {
        var threadIds = ThreadMessageIds(request.EmailThread);
        var predecessor = threadIds.Count > 0
            ? await _clarificationRepository.GetLatestByEmailMessageIdsAsync(request.ClientId, threadIds, cancellationToken)
            : null;

        if (predecessor is not { Status: InboundClarificationStatus.Expired })
        {
            var nowUtc = await UtcNowAsync(cancellationToken);
            predecessor = await _clarificationRepository.GetLatestExpiredByClientSinceAsync(
                request.ClientId,
                nowUtc.AddHours(-InboundClarificationConstants.RecentExpiryNoteWindowHours),
                cancellationToken);
        }

        if (predecessor is not { Status: InboundClarificationStatus.Expired })
        {
            return ClarificationPreAnalysis.None;
        }

        var companyTimeZone = await _companyClock.GetTimeZoneAsync(cancellationToken);
        return ClarificationPreAnalysis.Context(
            ClarificationNotificationTexts.AnsweredAfterExpiry(predecessor.Question, ClarificationTimeConversion.ToLocal(predecessor.AskedAt, companyTimeZone)));
    }

    private async Task<ClarificationPolicyFacts> GatherFactsAsync(
        ClarificationRequest request, InboundAnalysis analysis, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var open = await _clarificationRepository.GetOpenByClientAsync(request.ClientId, cancellationToken);
        var askedRecently = await _clarificationRepository.CountAskedSinceAsync(
            request.ClientId, nowUtc.AddMinutes(-InboundClarificationConstants.RateLimitWindowMinutes), cancellationToken);
        var killSwitchActive = await _governanceResolver.IsKillSwitchActiveAsync(cancellationToken);
        var globalLevel = await _governanceResolver.GetGlobalAutonomyLevelAsync(cancellationToken);

        return new ClarificationPolicyFacts(
            NeedsClarification: analysis.NeedsClarification,
            ClientType: request.ClientType,
            Intent: analysis.Intent,
            FeatureEnabled: true,
            IsAutoGenerated: request.EmailThread?.IsAutoGenerated ?? false,
            HasOpenClarification: open != null,
            QuestionsAskedInRateWindow: askedRecently,
            KillSwitchActive: killSwitchActive,
            GlobalAutonomyLevel: globalLevel,
            HasPersonalReplyTarget: true);
    }

    private async Task<InboundReplyTarget?> ResolveTargetSafelyAsync(
        IInboundReplySender sender, ClarificationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return await sender.ResolveTargetAsync(request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Personal contact of client {ClientId} could not be resolved; treated as no personal contact", request.ClientId);
            return null;
        }
    }

    private async Task<ClarificationPostAnalysis> AskAsync(
        ClarificationRequest request,
        InboundAnalysis analysis,
        IInboundReplySender sender,
        InboundReplyTarget target,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var composed = await _questionComposer.ComposeAsync(request, analysis, cancellationToken);
        if (composed == null)
        {
            return ClarificationPostAnalysis.Continue;
        }

        var deadlineUtc = ClarificationDeadlineCalculator.Compute(nowUtc, composed.ShiftStartUtc);
        var clarification = BuildClarification(
            request, analysis, composed, target.Recipient, InboundClarificationStatus.Open, askedAtUtc: nowUtc, deadlineUtc: deadlineUtc);
        var clarificationId = clarification.Id;
        if (!await _clarificationRepository.TryAddOpenAsync(clarification, cancellationToken))
        {
            _logger.LogInformation("Client {ClientId} already has an open clarification; no second question", request.ClientId);
            return ClarificationPostAnalysis.Continue;
        }

        var delivery = await SendSafelyAsync(sender, request, target, composed.Question, cancellationToken);
        if (!delivery.Success)
        {
            _logger.LogWarning(
                "Clarification question to client {ClientId} (clarification {ClarificationId}) could not be sent: {Error}",
                request.ClientId, clarificationId, delivery.Error);
            await MarkUnresolvedSafelyAsync(clarificationId, request.ClientId);
            return ClarificationPostAnalysis.ContinueWith(ClarificationNotificationTexts.SendFailed(composed.Question));
        }

        await NotifyStartedSafelyAsync(request, analysis, composed, deadlineUtc, cancellationToken);
        return ClarificationPostAnalysis.Sent;
    }

    private async Task<ClarificationPostAnalysis> SuggestAsync(
        ClarificationRequest request, InboundAnalysis analysis, DateTime nowUtc, CancellationToken cancellationToken)
    {
        var composed = await _questionComposer.ComposeAsync(request, analysis, cancellationToken);
        if (composed == null)
        {
            return ClarificationPostAnalysis.Continue;
        }

        await _clarificationRepository.AddAsync(
            BuildClarification(
                request, analysis, composed, string.Empty, InboundClarificationStatus.Suggested, askedAtUtc: nowUtc, deadlineUtc: nowUtc),
            cancellationToken);
        return ClarificationPostAnalysis.ContinueWith(ClarificationNotificationTexts.Suggested(composed.Question));
    }

    private async Task<InboundReplyResult> SendSafelyAsync(
        IInboundReplySender sender, ClarificationRequest request, InboundReplyTarget target, string question, CancellationToken cancellationToken)
    {
        try
        {
            return await sender.SendAsync(request, target, question, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Clarification question to client {ClientId} could not be sent", request.ClientId);
            return InboundReplyResult.Failed(ex.Message);
        }
    }

    private async Task MarkUnresolvedSafelyAsync(Guid clarificationId, Guid clientId)
    {
        try
        {
            var resolved = await _clarificationRepository.TryResolveAsync(
                clarificationId,
                InboundClarificationStatus.Unresolved,
                answerSourceId: null,
                resultAnalysisId: null,
                resolvedAtUtc: await UtcNowAsync(CancellationToken.None),
                cancellationToken: CancellationToken.None);
            if (!resolved)
            {
                _logger.LogWarning(
                    "Clarification {ClarificationId} of client {ClientId} was no longer Open when marking it unresolved after a failed send",
                    clarificationId, clientId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Clarification {ClarificationId} of client {ClientId} could not be marked unresolved after a failed send",
                clarificationId, clientId);
        }
    }

    private async Task NotifyStartedSafelyAsync(
        ClarificationRequest request, InboundAnalysis analysis, ComposedClarificationQuestion composed, DateTime deadlineUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            var companyTimeZone = await _companyClock.GetTimeZoneAsync(cancellationToken);
            await _analysisNotifier.NotifyMessageAsync(
                ClarificationNotificationTexts.Started(
                    request.Source.SenderDisplay, analysis.Summary, composed.Question, composed.ShiftContext,
                    ClarificationTimeConversion.ToLocal(deadlineUtc, companyTimeZone)),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Planners could not be told about the clarification question to client {ClientId}", request.ClientId);
        }
    }

    private static InboundClarification BuildClarification(
        ClarificationRequest request,
        InboundAnalysis analysis,
        ComposedClarificationQuestion composed,
        string recipient,
        InboundClarificationStatus status,
        DateTime askedAtUtc,
        DateTime deadlineUtc) => new()
    {
        Id = Guid.NewGuid(),
        ClientId = request.ClientId,
        SourceKind = request.Source.SourceKind,
        Channel = request.ReplyChannel,
        Recipient = recipient,
        OriginalAnalysisId = analysis.Id,
        OriginalSourceId = request.Source.SourceId,
        SenderDisplay = Truncate(request.Source.SenderDisplay, InboundClarificationConstants.MaxSenderDisplayLength),
        OriginalText = Truncate(request.Source.Body, InboundClarificationConstants.MaxOriginalTextLength),
        OriginalReceivedAt = ClarificationTimeConversion.AsUtc(request.Source.ReceivedAt),
        Question = composed.Question,
        ShiftContext = composed.ShiftContext == null
            ? null
            : Truncate(composed.ShiftContext, InboundClarificationConstants.MaxShiftContextLength),
        AskedAt = askedAtUtc,
        DeadlineAt = deadlineUtc,
        Status = status,
        EmailMessageId = NormalizeMessageId(request.EmailThread?.MessageId)
    };

    private static string? NormalizeMessageId(string? messageId)
    {
        var normalized = messageId?.Trim().Trim(MessageIdBrackets).Trim();
        return string.IsNullOrEmpty(normalized) || normalized.Length > InboundClarificationConstants.MaxStoredEmailMessageIdLength
            ? null
            : normalized;
    }

    private async Task<bool> IsFeatureEnabledAsync()
    {
        var setting = await _settingsReader.GetSetting(AppSettings.INBOUND_CLARIFICATION_ENABLED);
        return setting?.Value != null && bool.TryParse(setting.Value, out var enabled) && enabled;
    }

    private async Task<DateTime> UtcNowAsync(CancellationToken cancellationToken) =>
        (await _companyClock.GetNowAsync(cancellationToken)).UtcDateTime;

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
