// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Assembles the per-turn LLM context and splits it into a stable segment (identity, world-model
/// ontology, rule pack) and a volatile segment (pending-notes hint, recently touched entities,
/// sentiment mood hint, relevant memories via hybrid search) so the caller can cache the stable
/// segment across turns while the volatile segment is sent fresh every time.
/// </summary>
/// <param name="identityContextProvider">Provides cached identity prompt for the agent</param>
/// <param name="ontologyService">Provides the Klacks domain ontology (entities, relations, constraints)</param>
/// <param name="memoryRetrievalService">Retrieves relevant memories for the user message</param>
/// <param name="sentimentAnalyzer">Analyzes user message sentiment for mood hints</param>
/// <param name="ruleContextProvider">Builds the situational scheduling rule-pack when a scheduling skill is in scope</param>
/// <param name="pendingUserNoteRepository">Provides the count of undelivered notes stashed for the user, surfaced as a proactive hint</param>
/// <param name="logger">Logger instance</param>

using System.Text;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

public class ContextAssemblyPipeline
{
    private readonly IIdentityContextProvider _identityContextProvider;
    private readonly IKlacksOntologyService _ontologyService;
    private readonly IMemoryRetrievalService _memoryRetrievalService;
    private readonly ISentimentAnalyzer _sentimentAnalyzer;
    private readonly IRuleContextProvider _ruleContextProvider;
    private readonly IPendingUserNoteRepository _pendingUserNoteRepository;
    private readonly IRecentEntityRepository _recentEntityRepository;
    private readonly ILogger<ContextAssemblyPipeline> _logger;

    private const int CharsPerToken = 4;
    private const float SentimentThreshold = 0.5f;
    private const int OntologyBlockMaxTokens = IKlacksOntologyService.DefaultMaxTokens;

    // Below this length, an utterance is too short for sentiment/memory retrieval
    // to add value — typical examples are "ja", "ok", "weiter", "?". Skipping
    // saves one sentiment call + one embedding round-trip per such turn.
    private const int MinLengthForSemanticEnrichment = 8;
    private const int DefaultMaxLessonsPerTurn = 5;

    public ContextAssemblyPipeline(
        IIdentityContextProvider identityContextProvider,
        IKlacksOntologyService ontologyService,
        IMemoryRetrievalService memoryRetrievalService,
        ISentimentAnalyzer sentimentAnalyzer,
        IRuleContextProvider ruleContextProvider,
        IPendingUserNoteRepository pendingUserNoteRepository,
        IRecentEntityRepository recentEntityRepository,
        ILogger<ContextAssemblyPipeline> logger)
    {
        _identityContextProvider = identityContextProvider;
        _ontologyService = ontologyService;
        _memoryRetrievalService = memoryRetrievalService;
        _sentimentAnalyzer = sentimentAnalyzer;
        _ruleContextProvider = ruleContextProvider;
        _pendingUserNoteRepository = pendingUserNoteRepository;
        _recentEntityRepository = recentEntityRepository;
        _logger = logger;
    }

    /// <remarks>
    /// Every repository used here resolves from the request scope and therefore shares one DbContext,
    /// which permits exactly one in-flight operation. Starting a retrieval task early and awaiting it
    /// later ran it concurrently with the awaits in between and threw "A second operation was started
    /// on this context instance", aborting the whole turn before skill selection was ever reached
    /// (observed live 2026-08-10: the toolset-lessons query collided with CountPendingAsync). The
    /// database-backed steps below are therefore strictly sequential. Only work that owns a separate
    /// scope — SentimentAnalyzer, via IServiceScopeFactory — may run alongside them.
    /// </remarks>
    public async Task<SoulAndMemoryPrompt> AssembleSoulAndMemoryPromptAsync(
        Guid agentId,
        string userMessage,
        string? language = null,
        IReadOnlyList<string>? availableSkillNames = null,
        Klacks.Api.Domain.Models.Scheduling.SchedulingPolicy? scopedClientPolicy = null,
        bool hasDomainSkillContext = true,
        Guid? userId = null,
        string? conversationId = null,
        bool isVoiceMode = false,
        ContextBudgetProfile? budgetProfile = null,
        CancellationToken cancellationToken = default)
    {
        var stableSb = new StringBuilder();
        var volatileSb = new StringBuilder();

        var maxLessons = budgetProfile?.MaxLessonsPerTurn ?? DefaultMaxLessonsPerTurn;

        var identityPrompt = await _identityContextProvider.GetIdentityPromptAsync(
            agentId, language, suppressTextOnlyAffordances: isVoiceMode, cancellationToken);
        stableSb.Append(identityPrompt);

        if (userId.HasValue)
        {
            var pendingNoteCount = await _pendingUserNoteRepository.CountPendingAsync(agentId, userId.Value, cancellationToken);
            if (pendingNoteCount > 0)
            {
                volatileSb.AppendLine();
                volatileSb.AppendLine($"[PENDING_NOTES: {pendingNoteCount}] You have {pendingNoteCount} undelivered note(s) stashed for this user. Call manage_pending_notes with action 'read' to read them, relay them to the user naturally, then call manage_pending_notes with action 'mark_delivered' and their ids so they are not delivered again.");
                volatileSb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(conversationId))
            {
                var recentEntities = await _recentEntityRepository.GetRecentAsync(userId.Value, conversationId!, cancellationToken);
                var recentBlock = RecentEntityContextRenderer.Render(recentEntities);
                if (!string.IsNullOrEmpty(recentBlock))
                {
                    volatileSb.AppendLine();
                    volatileSb.AppendLine(recentBlock);
                    volatileSb.AppendLine();
                }
            }
        }

        // The world-model ontology grounds domain reasoning. On purely conversational turns (no domain skill
        // retrieved and not a scheduling task) it is dead weight — omit it to save up to ~1500 tokens/turn.
        var includeWorldModel = hasDomainSkillContext || _ruleContextProvider.IsSchedulingContext(availableSkillNames);
        if (includeWorldModel)
        {
            var ontologyBlock = _ontologyService.RenderWorldModelBlock(OntologyBlockMaxTokens);
            if (!string.IsNullOrWhiteSpace(ontologyBlock))
            {
                stableSb.AppendLine();
                stableSb.AppendLine(ontologyBlock);
                stableSb.AppendLine();
            }
        }

        var rulePack = _ruleContextProvider.BuildSchedulingRulePack(availableSkillNames, scopedClientPolicy);
        if (!string.IsNullOrWhiteSpace(rulePack))
        {
            stableSb.AppendLine(rulePack);
            stableSb.AppendLine();
        }

        if ((userMessage?.Trim().Length ?? 0) < MinLengthForSemanticEnrichment)
        {
            _logger.LogDebug("Skipping sentiment + memory retrieval for short utterance (len < {Min})", MinLengthForSemanticEnrichment);
            var shortTurnLessons = await RetrieveToolsetLessonsAsync(agentId, availableSkillNames, maxLessons, cancellationToken);
            AppendLessons(volatileSb, shortTurnLessons);
            return new SoulAndMemoryPrompt(stableSb.ToString(), volatileSb.ToString(), CombineIds(null, shortTurnLessons));
        }

        // SentimentAnalyzer resolves its own service scope, so it owns a separate DbContext and may run
        // alongside the queries below. Everything else here shares the request-scoped DbContext and must
        // stay strictly sequential — see the method remarks.
        var sentimentTask = _sentimentAnalyzer.AnalyzeSentimentAsync(userMessage!);

        var memoryResult = await _memoryRetrievalService.RetrieveRelevantMemoriesAsync(
            agentId, userMessage!, userId, budgetProfile, cancellationToken);
        var toolsetLessons = await RetrieveToolsetLessonsAsync(agentId, availableSkillNames, maxLessons, cancellationToken);

        var sentimentResult = await sentimentTask;
        if (sentimentResult.Mood != SentimentMood.Neutral && sentimentResult.Confidence > SentimentThreshold)
        {
            volatileSb.AppendLine($"[USER_MOOD: {sentimentResult.Mood.ToString().ToUpperInvariant()}] Adjust your tone accordingly.");
            volatileSb.AppendLine();
        }

        volatileSb.Append(memoryResult.PromptText);

        var lessons = toolsetLessons
            .Where(l => memoryResult.InjectedMemoryIds?.Contains(l.Id) != true)
            .ToList();
        AppendLessons(volatileSb, lessons);

        return new SoulAndMemoryPrompt(stableSb.ToString(), volatileSb.ToString(), CombineIds(memoryResult.InjectedMemoryIds, lessons));
    }

    private Task<List<AgentMemory>> RetrieveToolsetLessonsAsync(
        Guid agentId,
        IReadOnlyList<string>? availableSkillNames,
        int maxLessons,
        CancellationToken cancellationToken)
    {
        return availableSkillNames is { Count: > 0 }
            ? _memoryRetrievalService.RetrieveToolsetLessonsAsync(agentId, availableSkillNames, maxLessons, cancellationToken)
            : Task.FromResult(new List<AgentMemory>());
    }

    private static void AppendLessons(StringBuilder volatileSb, IReadOnlyList<AgentMemory> lessons)
    {
        if (lessons.Count == 0)
        {
            return;
        }

        volatileSb.AppendLine();
        volatileSb.AppendLine("[LESSONS] Earlier attempts with tools in your current tool set went wrong; apply these lessons:");
        foreach (var lesson in lessons)
        {
            volatileSb.AppendLine($"- [{lesson.Key}] {lesson.Content}");
        }

        volatileSb.AppendLine();
    }

    private static IReadOnlyList<Guid>? CombineIds(IReadOnlyList<Guid>? memoryIds, IReadOnlyList<AgentMemory> lessons)
    {
        if (lessons.Count == 0)
        {
            return memoryIds;
        }

        var combined = new List<Guid>(memoryIds ?? (IReadOnlyList<Guid>)Array.Empty<Guid>());
        combined.AddRange(lessons.Select(l => l.Id));
        return combined;
    }

    public int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        return text.Length / CharsPerToken;
    }
}
