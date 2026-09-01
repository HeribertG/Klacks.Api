// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Headless single-turn replay against a configurable model. Mirrors the production
/// assembly path (agent, toolset, planning scope, soul/memory prompt, system prompt and
/// the first-iteration tool-choice forcing) but performs exactly one provider call and
/// returns the model's first tool choice. It never executes tools, never creates a
/// conversation and never triggers background telemetry, so replays cannot pollute
/// production data; the only intended persistence is the EvalRun written by the runner.
/// </summary>

using System.Diagnostics;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.Recipes;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public class TurnReplayService : ITurnReplayService
{
    private const double ReplayTemperature = 0.7;

    /// <summary>
    /// W4: how many provider calls one replay may perform. Two covers the dominant check-then-act
    /// pattern (list/get first, then create/update/delete); more iterations would multiply provider
    /// cost without additional signal.
    /// </summary>
    private const int ReplayMaxIterations = 2;

    private readonly ISkillCacheService _skillCacheService;
    private readonly ISkillToolsetAssembler _toolsetAssembler;
    private readonly IPlanningScopeEnricher _planningScopeEnricher;
    private readonly IEntityCandidateGrounder _entityCandidateGrounder;
    private readonly LLMProviderOrchestrator _providerOrchestrator;
    private readonly ContextAssemblyPipeline _contextAssemblyPipeline;
    private readonly LLMSystemPromptBuilder _promptBuilder;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IContextBudgetPolicy _contextBudgetPolicy;
    private readonly ILogger<TurnReplayService> _logger;

    private List<AgentRecipe>? _cachedEnabledRecipes;

    private static readonly System.Text.Json.JsonSerializerOptions TriggerJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TurnReplayService(
        ISkillCacheService skillCacheService,
        ISkillToolsetAssembler toolsetAssembler,
        IPlanningScopeEnricher planningScopeEnricher,
        IEntityCandidateGrounder entityCandidateGrounder,
        LLMProviderOrchestrator providerOrchestrator,
        ContextAssemblyPipeline contextAssemblyPipeline,
        LLMSystemPromptBuilder promptBuilder,
        IServiceScopeFactory scopeFactory,
        IContextBudgetPolicy contextBudgetPolicy,
        ILogger<TurnReplayService> logger)
    {
        _skillCacheService = skillCacheService;
        _toolsetAssembler = toolsetAssembler;
        _planningScopeEnricher = planningScopeEnricher;
        _entityCandidateGrounder = entityCandidateGrounder;
        _providerOrchestrator = providerOrchestrator;
        _contextAssemblyPipeline = contextAssemblyPipeline;
        _promptBuilder = promptBuilder;
        _scopeFactory = scopeFactory;
        _contextBudgetPolicy = contextBudgetPolicy;
        _logger = logger;
    }

    public async Task<TurnReplayResult> ReplayAsync(
        TurnGoldsetItem item,
        string modelId,
        string userId,
        List<string> userRights,
        CancellationToken cancellationToken = default)
    {
        var (model, provider, error) = await _providerOrchestrator.GetModelAndProviderAsync(modelId);
        if (error != null || model == null || provider == null)
        {
            return new TurnReplayResult { Success = false, Error = error ?? "Model or provider not available." };
        }

        var agent = await _skillCacheService.GetDefaultAgentAsync(cancellationToken);
        var budgetProfile = _contextBudgetPolicy.Resolve(provider, model);

        var toolset = await _toolsetAssembler.AssembleAsync(
            agent, userRights, item.Message, conversationId: null,
            item.CurrentRoute, userId, item.Locale, budgetProfile.MaxToolsForProvider,
            cancellationToken: cancellationToken);

        var context = new LLMContext
        {
            Message = item.Message,
            UserId = userId,
            UserRights = userRights,
            ModelId = modelId,
            Language = item.Locale,
            PageContext = item.CurrentRoute == null ? null : new AssistantPageContext { CurrentRoute = item.CurrentRoute },
            AvailableFunctions = toolset.Functions,
            HasDomainSkillContext = toolset.HasDomainSkillContext
        };

        await _planningScopeEnricher.EnrichAsync(context, cancellationToken);
        await _entityCandidateGrounder.GroundAsync(context, cancellationToken);

        SoulAndMemoryPrompt? soulAndMemoryPrompt = null;
        if (agent != null)
        {
            var availableSkillNames = context.AvailableFunctions.Select(f => f.Name).ToList();
            Guid? parsedUserId = Guid.TryParse(userId, out var parsed) ? parsed : null;
            soulAndMemoryPrompt = await _contextAssemblyPipeline.AssembleSoulAndMemoryPromptAsync(
                agent.Id, context.Message, context.Language, availableSkillNames, context.ScopedClientPolicy,
                hasDomainSkillContext: context.HasDomainSkillContext ?? true,
                userId: parsedUserId,
                pageContext: context.PageContext,
                isVoiceMode: false,
                budgetProfile: budgetProfile,
                cancellationToken: cancellationToken);
        }

        context.InjectedMemoryIds = soulAndMemoryPrompt?.InjectedMemoryIds;

        var systemPrompt = await _promptBuilder.BuildSystemPromptAsync(context, soulAndMemoryPrompt?.StablePrompt);

        var forcingPlan = RecipeForcingResolver.Resolve(item.Message);
        var recipeWouldForce = forcingPlan != null;
        var triggeredRecipeName = await FindMatchingEngineRecipeNameAsync(
            item.Message, item.Locale, cancellationToken);
        var engineRecipeWouldTrigger = triggeredRecipeName != null;
        var toolChoiceRequired = MutationIntentDetector.IsMutationIntent(item.Message)
            || NavigationIntentDetector.IsNavigationIntent(item.Message);

        // W4 multi-step replay: run up to ReplayMaxIterations provider calls and feed each tool call
        // back as a synthetic empty result, mirroring the production loop's check-then-act pattern
        // (list first, then create/update/delete). Tools are never executed; read-only checks get
        // "[]", everything else "OK". The scorer credits any call in the sequence.
        var runningHistory = new List<Domain.Services.Assistant.Providers.LLMMessage>();
        var toolCalls = new List<TurnReplayToolCall>();
        var totalUsage = new Domain.Services.Assistant.Providers.LLMUsage();
        var totalCost = 0m;
        LLMProviderResponse? lastResponse = null;

        var stopwatch = Stopwatch.StartNew();
        for (var iteration = 0; iteration < ReplayMaxIterations; iteration++)
        {
            var request = new LLMProviderRequest
            {
                Message = item.Message,
                SystemPrompt = systemPrompt,
                VolatileSystemPrompt = LLMService.CombineVolatile(
                    LLMSystemPromptBuilder.BuildVolatileAdditions(context), soulAndMemoryPrompt?.VolatilePrompt),
                ModelId = model.ApiModelId,
                ConversationHistory = runningHistory,
                AvailableFunctions = context.AvailableFunctions,
                Temperature = ReplayTemperature,
                MaxTokens = model.MaxTokens,
                SupportedParameters = model.SupportedParameters,
                CostPerInputToken = model.CostPerInputToken,
                CostPerOutputToken = model.CostPerOutputToken,
                ToolChoice = toolChoiceRequired ? MutationGuardConstants.ToolChoiceRequired : null
            };

            var response = await ProcessWithTransientRetryAsync(provider, request, cancellationToken);
            lastResponse = response;

            totalUsage.InputTokens += response.Usage.InputTokens;
            totalUsage.OutputTokens += response.Usage.OutputTokens;
            totalUsage.CacheCreationInputTokens += response.Usage.CacheCreationInputTokens;
            totalUsage.CacheReadInputTokens += response.Usage.CacheReadInputTokens;
            totalCost += response.Usage.Cost;

            if (!response.Success)
            {
                break;
            }

            var firstCall = response.FunctionCalls.FirstOrDefault();
            if (firstCall == null)
            {
                break;
            }

            toolCalls.Add(new TurnReplayToolCall
            {
                Name = firstCall.FunctionName,
                Parameters = firstCall.Parameters ?? new Dictionary<string, object>()
            });

            // Synthetic tool result so the model can move from the read-only pre-check to the action.
            // Format mirrors LLMService.FormatFunctionResults so the model sees its usual result block.
            var syntheticCall = new LLMFunctionCall
            {
                FunctionName = firstCall.FunctionName,
                Parameters = firstCall.Parameters,
                Result = SyntheticToolResult(firstCall.FunctionName),
                Success = true
            };
            runningHistory.Add(new Domain.Services.Assistant.Providers.LLMMessage
            {
                Role = "assistant",
                Content = response.Content ?? string.Empty
            });
            runningHistory.Add(new Domain.Services.Assistant.Providers.LLMMessage
            {
                Role = "user",
                Content = LLMService.FormatFunctionResults([syntheticCall])
            });
        }

        stopwatch.Stop();

        var firstToolCall = toolCalls.FirstOrDefault();
        var result = new TurnReplayResult
        {
            Success = lastResponse?.Success ?? false,
            Error = lastResponse?.Error,
            ChosenTool = firstToolCall?.Name,
            ToolParameters = firstToolCall?.Parameters ?? new Dictionary<string, object>(),
            ToolCalls = toolCalls,
            Content = lastResponse?.Content ?? string.Empty,
            LatencyMs = stopwatch.ElapsedMilliseconds,
            Cost = totalCost,
            InputTokens = totalUsage.InputTokens,
            OutputTokens = totalUsage.OutputTokens,
            RecipeWouldForce = recipeWouldForce,
            EngineRecipeWouldTrigger = engineRecipeWouldTrigger,
            ForcedRecipeName = forcingPlan?.Name,
            TriggeredRecipeName = triggeredRecipeName,
            AvailableToolNames = context.AvailableFunctions.Select(f => f.Name).ToList(),
            ToolChoiceRequired = toolChoiceRequired,
            ProviderId = model.ProviderId,
            ApiModelId = model.ApiModelId
        };

        _logger.LogInformation(
            "TurnReplay item {ItemId} model {Model}: tool={Tool}, latency={LatencyMs}ms, success={Success}",
            item.Id, modelId, result.ChosenTool ?? "(none)", result.LatencyMs, result.Success);

        return result;
    }

    private async Task<string?> FindMatchingEngineRecipeNameAsync(
        string message, string? language, CancellationToken cancellationToken)
    {
        if (_cachedEnabledRecipes == null)
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IAgentRecipeRepository>();
            _cachedEnabledRecipes = await repository.GetAllEnabledAsync(cancellationToken);
        }

        foreach (var recipe in _cachedEnabledRecipes)
        {
            RecipeTrigger? trigger;
            try
            {
                trigger = System.Text.Json.JsonSerializer.Deserialize<RecipeTrigger>(recipe.TriggerJson, TriggerJsonOptions);
            }
            catch (System.Text.Json.JsonException)
            {
                continue;
            }

            IReadOnlyCollection<string>? synonyms = null;
            if (language != null && recipe.Synonyms != null && recipe.Synonyms.TryGetValue(language, out var languageSynonyms))
            {
                synonyms = languageSynonyms;
            }

            if (trigger != null && RecipeTriggerMatcher.Matches(trigger, synonyms, message))
            {
                return recipe.Name;
            }
        }

        return null;
    }

    private static string SyntheticToolResult(string functionName)
    {
        // Read-only pre-checks plausibly return an empty collection; everything else an ack. The
        // replay never executes tools, so these placeholders are deliberately neutral.
        var lower = functionName.ToLowerInvariant();
        if (lower.StartsWith("list_", StringComparison.Ordinal)
            || lower.StartsWith("get_", StringComparison.Ordinal)
            || lower.StartsWith("search_", StringComparison.Ordinal)
            || lower.StartsWith("find_", StringComparison.Ordinal))
        {
            return "[]";
        }

        return "OK";
    }

    private async Task<LLMProviderResponse> ProcessWithTransientRetryAsync(
        Domain.Services.Assistant.Providers.ILLMProvider provider,
        LLMProviderRequest request,
        CancellationToken cancellationToken)
    {
        var response = await provider.ProcessAsync(request);

        for (var attempt = 1;
             !response.Success
                 && attempt <= LLMRetryConstants.MaxTransientRetries
                 && TransientProviderErrorDetector.IsTransient(response.Error);
             attempt++)
        {
            _logger.LogWarning(
                "TurnReplay transient provider error (attempt {Attempt}/{Max}): {Error} - retrying",
                attempt, LLMRetryConstants.MaxTransientRetries, response.Error);
            await Task.Delay(LLMRetryConstants.GetRetryDelay(attempt), cancellationToken);
            response = await provider.ProcessAsync(request);
        }

        return response;
    }
}
