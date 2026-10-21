// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Prepares the context both chat paths start a turn from: resolves model and provider, loads or creates
/// the conversation, assembles the soul/memory segment, builds the stable and volatile system prompt and
/// truncates the stored history into the turn's history budget. Slow stages are logged.
/// </summary>
/// <param name="logger">The chat service's logger, so log categories stay unchanged</param>
/// <param name="providerOrchestrator">Resolves the model and the provider serving it</param>
/// <param name="conversationManager">Loads or creates the conversation and reads its history</param>
/// <param name="agentRepository">Supplies the default agent whose soul and memories are assembled</param>
/// <param name="contextAssemblyPipeline">Assembles the soul/memory prompt segments</param>
/// <param name="promptBuilder">Builds the stable system prompt and the temporal context</param>
/// <param name="contextBudgetPolicy">Derives the per-turn budget profile from provider and model</param>

using System.Diagnostics;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.ChatTurn;

internal sealed class ChatTurnContextPreparer
{
    private const int StageLogThresholdMs = 50;

    private readonly ILogger _logger;
    private readonly LLMProviderOrchestrator _providerOrchestrator;
    private readonly LLMConversationManager _conversationManager;
    private readonly IAgentRepository _agentRepository;
    private readonly ContextAssemblyPipeline _contextAssemblyPipeline;
    private readonly LLMSystemPromptBuilder _promptBuilder;
    private readonly IContextBudgetPolicy _contextBudgetPolicy;

    internal ChatTurnContextPreparer(
        ILogger logger,
        LLMProviderOrchestrator providerOrchestrator,
        LLMConversationManager conversationManager,
        IAgentRepository agentRepository,
        ContextAssemblyPipeline contextAssemblyPipeline,
        LLMSystemPromptBuilder promptBuilder,
        IContextBudgetPolicy contextBudgetPolicy)
    {
        _logger = logger;
        _providerOrchestrator = providerOrchestrator;
        _conversationManager = conversationManager;
        _agentRepository = agentRepository;
        _contextAssemblyPipeline = contextAssemblyPipeline;
        _promptBuilder = promptBuilder;
        _contextBudgetPolicy = contextBudgetPolicy;
    }

    /// <param name="context">The turn context; receives the ids of the memories injected into the prompt</param>
    /// <param name="cancellationToken">Cancels the agent lookup and the temporal-context build</param>
    internal async Task<PreparedChatTurn> PrepareAsync(LLMContext context, CancellationToken cancellationToken = default)
    {
        var stageWatch = Stopwatch.StartNew();

        var (model, provider, error) = await _providerOrchestrator.GetModelAndProviderAsync(context.ModelId);
        if (error != null) return PreparedChatTurn.Failed(error);

        var budgetProfile = _contextBudgetPolicy.Resolve(provider!, model!);

        var conversation = await _conversationManager.GetOrCreateConversationAsync(context.ConversationId, context.UserId);
        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);

        stageWatch.Restart();
        var llmHistory = await _conversationManager.GetConversationHistoryAsync(conversation.ConversationId, conversation.UserId);
        if (stageWatch.ElapsedMilliseconds > StageLogThresholdMs)
            _logger.LogInformation("LLM-Stage {Stage}: {Ms}ms", "GetConversationHistory", stageWatch.ElapsedMilliseconds);

        SoulAndMemoryPrompt? soulAndMemoryPrompt = null;
        if (agent != null)
        {
            stageWatch.Restart();
            var availableSkillNames = context.AvailableFunctions?.Select(f => f.Name).ToList();
            Guid? userId = Guid.TryParse(context.UserId, out var parsedUserId) ? parsedUserId : null;
            soulAndMemoryPrompt = await _contextAssemblyPipeline.AssembleSoulAndMemoryPromptAsync(
                agent.Id, context.Message, context.Language, availableSkillNames, context.ScopedClientPolicy,
                hasDomainSkillContext: context.HasDomainSkillContext ?? true,
                userId: userId,
                conversationId: context.ConversationId,
                pageContext: context.PageContext,
                isVoiceMode: context.IsVoiceMode,
                budgetProfile: budgetProfile);
            if (stageWatch.ElapsedMilliseconds > StageLogThresholdMs)
                _logger.LogInformation("LLM-Stage {Stage}: {Ms}ms", "AssembleSoulAndMemory", stageWatch.ElapsedMilliseconds);
        }

        context.InjectedMemoryIds = soulAndMemoryPrompt?.InjectedMemoryIds;

        stageWatch.Restart();
        var systemPrompt = await _promptBuilder.BuildSystemPromptAsync(context, soulAndMemoryPrompt?.StablePrompt);
        var temporalContext = await _promptBuilder.BuildTemporalContextAsync(context, cancellationToken);
        var volatilePrompt = VolatilePromptComposer.BuildVolatilePrompt(temporalContext, context, soulAndMemoryPrompt?.VolatilePrompt);
        if (stageWatch.ElapsedMilliseconds > StageLogThresholdMs)
            _logger.LogInformation("LLM-Stage {Stage}: {Ms}ms", "BuildSystemPrompt", stageWatch.ElapsedMilliseconds);

        if (context.IsVoiceMode)
        {
            _logger.LogInformation(
                "Voice turn: spoken-answer directive appended={Appended}, prompt {Length} chars",
                systemPrompt.Contains(VoiceModeInstructionConstants.SpokenAnswerDirective),
                systemPrompt.Length);
        }

        var historyBudget = ConversationHistoryBudget.HistoryBudgetFor(provider!, model!, systemPrompt, volatilePrompt, context.AvailableFunctions);
        var truncatedHistory = ConversationHistoryBudget.TruncateHistory(
            llmHistory, historyBudget, conversation.Summary, budgetProfile.MaxHistoryMessages);

        return new PreparedChatTurn(model, provider, null, conversation, systemPrompt, volatilePrompt, truncatedHistory, budgetProfile);
    }
}
