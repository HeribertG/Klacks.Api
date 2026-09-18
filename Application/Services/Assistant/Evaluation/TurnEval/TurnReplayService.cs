// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Headless single-turn replay against a configurable model. Mirrors the production
/// assembly path (agent, toolset, planning scope, soul/memory prompt, system prompt, the
/// graceful correction and the first-iteration tool-choice forcing) but performs at most one
/// provider call - two when the caller asks for the lookup follow-up and the first choice was a
/// plain lookup in front of an expected mutation - and returns the model's tool choices - a
/// correction that ends in the deterministic clarification returns that question and calls no
/// provider at all, exactly as production does. It never executes tools, never creates a conversation, never reads or writes
/// the previous-action record and never triggers background telemetry, so replays cannot pollute
/// production data; the only intended persistence is the EvalRun written by the runner.
/// LatencyMs keeps its established meaning on an ordinary replay - the provider call alone, so model
/// comparisons stay comparable - while a clarification, which makes no such call, reports the time its
/// own deterministic work took instead of a misleading zero.
/// </summary>

using System.Diagnostics;
using System.Text.Json;
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
    private readonly ISkillCacheService _skillCacheService;
    private readonly ISkillToolsetAssembler _toolsetAssembler;
    private readonly IPlanningScopeEnricher _planningScopeEnricher;
    private readonly IEntityCandidateGrounder _entityCandidateGrounder;
    private readonly LLMProviderOrchestrator _providerOrchestrator;
    private readonly ContextAssemblyPipeline _contextAssemblyPipeline;
    private readonly LLMSystemPromptBuilder _promptBuilder;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IContextBudgetPolicy _contextBudgetPolicy;
    private readonly ITurnPreparationService _turnPreparation;
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
        ITurnPreparationService turnPreparation,
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
        _turnPreparation = turnPreparation;
        _logger = logger;
    }

    public Task<TurnReplayResult> ReplayAsync(
        TurnGoldsetItem item,
        string modelId,
        string userId,
        List<string> userRights,
        CancellationToken cancellationToken = default) =>
        ReplayCoreAsync(item, modelId, userId, userRights, followUpLookups: false, cancellationToken);

    public Task<TurnReplayResult> ReplayWithLookupFollowUpAsync(
        TurnGoldsetItem item,
        string modelId,
        string userId,
        List<string> userRights,
        CancellationToken cancellationToken = default) =>
        ReplayCoreAsync(item, modelId, userId, userRights, followUpLookups: true, cancellationToken);

    private async Task<TurnReplayResult> ReplayCoreAsync(
        TurnGoldsetItem item,
        string modelId,
        string userId,
        List<string> userRights,
        bool followUpLookups,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var (model, provider, error) = await _providerOrchestrator.GetModelAndProviderAsync(modelId);
        if (error != null || model == null || provider == null)
        {
            return new TurnReplayResult { Success = false, Error = error ?? "Model or provider not available." };
        }

        var prep = await PrepareReplayPromptAsync(
            item, modelId, userId, userRights, model, provider, cancellationToken);

        if (prep.Correction?.ClarificationReply is { Length: > 0 } replayClarification)
        {
            _logger.LogInformation(
                "TurnReplay item {ItemId}: correction ended in the deterministic clarification, no provider call",
                item.Id);

            return new TurnReplayResult
            {
                Success = true,
                ChosenTool = null,
                Content = replayClarification,
                AvailableToolNames = prep.Context.AvailableFunctions.Select(f => f.Name).ToList(),
                RecipeWouldForce = prep.RecipeWouldForce,
                EngineRecipeWouldTrigger = prep.EngineRecipeWouldTrigger,
                ForcedRecipeName = prep.ForcingPlan?.Name,
                TriggeredRecipeName = prep.TriggeredRecipeName,
                ToolChoiceRequired = prep.ToolChoiceRequired,
                ProviderId = model.ProviderId,
                ApiModelId = model.ApiModelId,
                LatencyMs = stopwatch.ElapsedMilliseconds,
                CorrectionApplied = true,
                CorrectionClarificationOffered = true,
                UndoOfferedSkill = prep.Correction?.Undo?.SkillName
            };
        }

        stopwatch.Restart();
        var response = await ProcessWithTransientRetryAsync(provider, prep.Request, cancellationToken);
        stopwatch.Stop();

        var firstCall = response.FunctionCalls.FirstOrDefault();

        var result = new TurnReplayResult
        {
            Success = response.Success,
            Error = response.Error,
            ChosenTool = firstCall?.FunctionName,
            ToolParameters = firstCall?.Parameters ?? new Dictionary<string, object>(),
            Content = response.Content,
            LatencyMs = stopwatch.ElapsedMilliseconds,
            Cost = response.Usage.Cost,
            InputTokens = response.Usage.InputTokens,
            OutputTokens = response.Usage.OutputTokens,
            RecipeWouldForce = prep.RecipeWouldForce,
            EngineRecipeWouldTrigger = prep.EngineRecipeWouldTrigger,
            ForcedRecipeName = prep.ForcingPlan?.Name,
            TriggeredRecipeName = prep.TriggeredRecipeName,
            AvailableToolNames = prep.Context.AvailableFunctions.Select(f => f.Name).ToList(),
            ToolChoiceRequired = prep.ToolChoiceRequired,
            ProviderId = model.ProviderId,
            ApiModelId = model.ApiModelId,
            CorrectionApplied = prep.Correction != null,
            CorrectionClarificationOffered = false,
            UndoOfferedSkill = prep.Correction?.Undo?.SkillName
        };

        result.Steps.Add(new TurnReplayStep
        {
            Tool = result.ChosenTool,
            Parameters = result.ToolParameters,
            Content = result.Content,
            LatencyMs = result.LatencyMs,
            Success = result.Success,
            Error = result.Error
        });

        if (followUpLookups && firstCall != null
            && await QualifiesForFollowUpAsync(item, result, cancellationToken))
        {
            firstCall.Result = SyntheticLookupResultFactory.Build(firstCall.Parameters);

            var followUpHistory = new List<Domain.Services.Assistant.Providers.LLMMessage>(prep.ReplayHistory)
            {
                new() { Role = LLMMessageRoles.User, Content = item.Message },
                new()
                {
                    Role = LLMMessageRoles.Assistant,
                    Content = string.IsNullOrEmpty(response.Content)
                        ? LLMLoopConstants.ExecutingFunctionCallsPlaceholder
                        : response.Content
                }
            };

            var followUpRequest = BuildProviderRequest(
                model, prep.SystemPrompt, prep.VolatilePrompt, prep.Context,
                LLMService.FormatFunctionResults([firstCall], prep.BudgetProfile.MaxToolResultChars),
                followUpHistory,
                ToolChoicePolicy.ResolveToolChoice(
                    forceRecipe: false,
                    isMutationIntent: MutationIntentDetector.IsMutationIntent(item.Message),
                    isNavigationIntent: NavigationIntentDetector.IsNavigationIntent(item.Message),
                    forceConfirmation: false,
                    toolCallCount: 1));

            await RunFollowUpStepAsync(provider, followUpRequest, result, cancellationToken);
        }

        if (result.Steps.Count > TurnEvalDefaults.MaxReplaySteps)
        {
            throw new InvalidOperationException(TurnEvalDefaults.ReplayStepLimitExceededMessage);
        }

        _logger.LogInformation(
            "TurnReplay item {ItemId} model {Model}: tool={Tool}, latency={LatencyMs}ms, success={Success}",
            item.Id, modelId, result.ChosenTool ?? "(none)", result.LatencyMs, result.Success);

        return result;
    }

    /// <summary>
    /// Everything a replay needs before it can talk to the provider: agent, planning scope, graceful
    /// correction, the assembled toolset, the system/volatile prompt and the first-step request. Split out
    /// of <see cref="ReplayCoreAsync"/> purely to keep that method's own body under the project's method
    /// size ceiling - no behavior changes; the returned <see cref="ReplayPromptPreparation"/> carries
    /// everything ReplayCoreAsync used to hold in locals.
    /// </summary>
    private async Task<ReplayPromptPreparation> PrepareReplayPromptAsync(
        TurnGoldsetItem item,
        string modelId,
        string userId,
        List<string> userRights,
        LLMModel model,
        Domain.Services.Assistant.Providers.ILLMProvider provider,
        CancellationToken cancellationToken)
    {
        var agent = await _skillCacheService.GetDefaultAgentAsync(cancellationToken);
        var budgetProfile = _contextBudgetPolicy.Resolve(provider, model);

        var lastAction = BuildReplayLastAction(item, userId);
        var correctionPlan = await _turnPreparation.PlanCorrectionAsync(
            new GracefulCorrectionInput(
                agent, userRights, item.Message, ConversationId: null, userId, item.Locale, lastAction,
                RecipeIsActive: false),
            cancellationToken);

        var toolset = await _toolsetAssembler.AssembleAsync(
            agent, userRights, correctionPlan?.CompositeMessage ?? item.Message, conversationId: null,
            item.CurrentRoute, userId, item.Locale, budgetProfile.MaxToolsForProvider,
            applyLearnedPhraseGuarantee: true,
            excludedSkillNames: correctionPlan?.ExcludedSkillNames,
            pinnedSkillNames: null,
            cancellationToken: cancellationToken);

        var correction = correctionPlan == null
            ? null
            : _turnPreparation.CompleteCorrection(correctionPlan, toolset.Functions, item.Locale);

        var replayHistory = BuildReplayHistory(item);

        var context = new LLMContext
        {
            Message = item.Message,
            UserId = userId,
            UserRights = userRights,
            ModelId = modelId,
            Language = item.Locale,
            PageContext = item.CurrentRoute == null ? null : new AssistantPageContext { CurrentRoute = item.CurrentRoute },
            AvailableFunctions = toolset.Functions,
            HasDomainSkillContext = toolset.HasDomainSkillContext,
            GracefulCorrectionApplied = correction != null,

            // A replay resolves the undo as data and never holds a token, so it must never claim to
            // have offered one - that flag suppresses the settlement of an outstanding row.
            CorrectionUndoOffered = false
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
        var temporalContext = await _promptBuilder.BuildTemporalContextAsync(context, cancellationToken);

        var forcingPlan = RecipeForcingResolver.Resolve(item.Message);
        var triggeredRecipeName = await FindMatchingEngineRecipeNameAsync(
            item.Message, item.Locale, cancellationToken);
        var toolChoiceRequired = MutationIntentDetector.IsMutationIntent(item.Message)
            || NavigationIntentDetector.IsNavigationIntent(item.Message);

        var volatilePrompt = LLMService.CombineVolatile(
            temporalContext,
            LLMService.CombineVolatile(
                LLMService.CombineVolatile(
                    LLMSystemPromptBuilder.BuildVolatileAdditions(context), soulAndMemoryPrompt?.VolatilePrompt),
                correction?.ContextNote));

        var request = BuildProviderRequest(
            model, systemPrompt, volatilePrompt, context,
            item.Message, replayHistory, toolChoiceRequired ? MutationGuardConstants.ToolChoiceRequired : null);

        return new ReplayPromptPreparation(
            context, correction, replayHistory, forcingPlan, forcingPlan != null, triggeredRecipeName,
            triggeredRecipeName != null, toolChoiceRequired, budgetProfile, systemPrompt, volatilePrompt, request);
    }

    private static LLMProviderRequest BuildProviderRequest(
        LLMModel model,
        string systemPrompt,
        string? volatilePrompt,
        LLMContext context,
        string message,
        List<Domain.Services.Assistant.Providers.LLMMessage> history,
        string? toolChoice) => new()
    {
        Message = message,
        SystemPrompt = systemPrompt,
        VolatileSystemPrompt = volatilePrompt,
        ModelId = model.ApiModelId,
        ConversationHistory = history,
        AvailableFunctions = context.AvailableFunctions,
        Temperature = TurnEvalDefaults.ReplayTemperature,
        MaxTokens = model.MaxTokens,
        SupportedParameters = model.SupportedParameters,
        CostPerInputToken = model.CostPerInputToken,
        CostPerOutputToken = model.CostPerOutputToken,
        ToolChoice = toolChoice
    };

    /// <summary>
    /// Everything <see cref="ReplayCoreAsync"/> needs after <see cref="PrepareReplayPromptAsync"/> has run:
    /// the assembled context, the graceful-correction outcome, the seeded history and the pieces a
    /// follow-up request is rebuilt from.
    /// </summary>
    private sealed record ReplayPromptPreparation(
        LLMContext Context,
        GracefulCorrectionOutcome? Correction,
        List<Domain.Services.Assistant.Providers.LLMMessage> ReplayHistory,
        RecipeForcingPlan? ForcingPlan,
        bool RecipeWouldForce,
        string? TriggeredRecipeName,
        bool EngineRecipeWouldTrigger,
        bool ToolChoiceRequired,
        ContextBudgetProfile BudgetProfile,
        string SystemPrompt,
        string? VolatilePrompt,
        LLMProviderRequest Request);

    /// <summary>
    /// Seeds the replay with the turn a correction item refers to, in the same role/content shape
    /// production history uses (LLMConversationManager) - production also sets Timestamp and sanitizes
    /// tool-call markup, neither of which the replay needs. The assistant entry is omitted when the
    /// excerpt is blank: Anthropic drops whitespace-only history messages while OpenAI-compatible
    /// providers do not, and the replay must behave identically regardless of provider. An item without
    /// a previousTurn replays with an empty history, as before.
    /// </summary>
    internal static List<Domain.Services.Assistant.Providers.LLMMessage> BuildReplayHistory(TurnGoldsetItem item)
    {
        if (item.PreviousTurn == null)
        {
            return new List<Domain.Services.Assistant.Providers.LLMMessage>();
        }

        var history = new List<Domain.Services.Assistant.Providers.LLMMessage>
        {
            new() { Role = LLMMessageRoles.User, Content = item.PreviousTurn.Message }
        };

        if (!string.IsNullOrWhiteSpace(item.PreviousTurn.AssistantAnswerExcerpt))
        {
            history.Add(new() { Role = LLMMessageRoles.Assistant, Content = item.PreviousTurn.AssistantAnswerExcerpt });
        }

        return history;
    }

    /// <summary>
    /// Rebuilds the previous-action record from the goldset item. In-memory only, and deliberately not
    /// through IAssistantLastActionStore: a replay must never read or write the live rows of the user it
    /// runs as. IsReadOnly follows exactly the rule the live write point uses, so a goldset item cannot
    /// declare a classification the production path would not have produced.
    /// </summary>
    /// <param name="item">The goldset item, whose PreviousTurn carries the corrected call.</param>
    /// <param name="userId">The user the replay runs as, only to fill the record's own key field.</param>
    internal static AssistantLastAction? BuildReplayLastAction(TurnGoldsetItem item, string userId)
    {
        if (item.PreviousTurn == null)
        {
            return null;
        }

        Guid.TryParse(userId, out var parsedUserId);

        return new AssistantLastAction
        {
            UserId = parsedUserId,
            ConversationId = item.Id,
            UserMessage = item.PreviousTurn.Message,
            AssistantAnswerExcerpt = item.PreviousTurn.AssistantAnswerExcerpt ?? string.Empty,
            CreateTimeUtc = DateTime.UtcNow,
            Calls =
            [
                new AssistantLastActionCall
                {
                    SkillName = item.PreviousTurn.CalledSkill,
                    SkillDisplayLabel = item.PreviousTurn.SkillDisplayLabel,
                    SkillLabels = ReplayLabels(item),
                    ArgumentsJson = JsonSerializer.Serialize(item.PreviousTurn.Arguments),
                    ResultDataJson = JsonSerializer.Serialize(item.PreviousTurn.ResultData),
                    IsReadOnly = ReadOnlySkillPrefixes.HasReadOnlyPrefix(item.PreviousTurn.CalledSkill),
                    Success = true
                }
            ]
        };
    }

    /// <summary>
    /// The goldset authors ONE label per item, in the item's own locale, so the replay presents it as the
    /// authored label for exactly that locale. Without this the clarification would never be reached in a
    /// replay: the question resolves the previous action from the authored labels, and a goldset item
    /// carries no label dictionary of its own. A replay runs the correction in the item's locale, so this
    /// reproduces the production case where the action and the correction share a language.
    /// </summary>
    /// <param name="item">The goldset item, whose PreviousTurn carries the corrected call and its label.</param>
    private static IReadOnlyDictionary<string, string>? ReplayLabels(TurnGoldsetItem item) =>
        string.IsNullOrWhiteSpace(item.PreviousTurn?.SkillDisplayLabel) || string.IsNullOrWhiteSpace(item.Locale)
            ? null
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [item.Locale!] = item.PreviousTurn!.SkillDisplayLabel!
            };

    /// <summary>
    /// Whether the first step qualifies for the lookup follow-up: it must be a genuine miss against an
    /// available, non-hijacked expectation, and the chosen skill must be a plain read-only lookup ahead
    /// of an expected mutation - <see cref="ReplayFollowUpPolicy"/> holds the exact skill-shape rule.
    /// </summary>
    private async Task<bool> QualifiesForFollowUpAsync(
        TurnGoldsetItem item, TurnReplayResult firstStep, CancellationToken cancellationToken)
    {
        if (!firstStep.Success
            || item.ExpectedTool == null
            || firstStep.RecipeWouldForce
            || firstStep.EngineRecipeWouldTrigger
            || TurnEvalScorer.IsAcceptableTool(item, firstStep.ChosenTool)
            || !firstStep.AvailableToolNames.Any(name => TurnEvalScorer.IsAcceptableTool(item, name)))
        {
            return false;
        }

        var skills = await _skillCacheService.GetAllEnabledSkillsAsync(cancellationToken);
        AgentSkill? Find(string? name) => skills.FirstOrDefault(
            skill => string.Equals(skill.Name, name, StringComparison.OrdinalIgnoreCase));

        return ReplayFollowUpPolicy.ShouldFollowUp(Find(firstStep.ChosenTool), Find(item.ExpectedTool));
    }

    /// <summary>
    /// Runs the second provider call and records it as a step, never letting a provider failure surface
    /// as an exception - only as FollowUpFailed on the already-valid first-step result.
    /// </summary>
    private async Task RunFollowUpStepAsync(
        Domain.Services.Assistant.Providers.ILLMProvider provider,
        LLMProviderRequest request,
        TurnReplayResult result,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await ProcessWithTransientRetryAsync(provider, request, cancellationToken);
        stopwatch.Stop();

        var call = response.FunctionCalls.FirstOrDefault();
        result.FollowUpAttempted = true;
        result.FollowUpFailed = !response.Success;
        result.Cost += response.Usage.Cost;
        result.InputTokens += response.Usage.InputTokens;
        result.OutputTokens += response.Usage.OutputTokens;
        result.Steps.Add(new TurnReplayStep
        {
            Tool = call?.FunctionName,
            Parameters = call?.Parameters ?? new Dictionary<string, object>(),
            Content = response.Content,
            LatencyMs = stopwatch.ElapsedMilliseconds,
            Success = response.Success,
            Error = response.Error
        });

        if (!response.Success)
        {
            _logger.LogWarning("TurnReplay follow-up step failed: {Error}", response.Error);
        }
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

            IReadOnlyCollection<string>? synonyms = recipe.SynonymsFor(language);

            if (trigger != null && RecipeTriggerMatcher.Matches(
                    trigger, synonyms, message, null, language, recipe.VetoesFor(language)))
            {
                return recipe.Name;
            }
        }

        return null;
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
