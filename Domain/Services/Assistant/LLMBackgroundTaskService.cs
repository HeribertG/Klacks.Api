// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Domain.Services.Assistant;

/// <summary>
/// Service for asynchronous background tasks after LLM interactions (compaction, memory extraction,
/// learning case collection, trajectory capture and — only on a failed skill call — reflection), plus
/// standalone triggers for callers outside the post-turn hook: task-boundary compaction (e.g. AgentPlan
/// completion) and reflection (e.g. a user correction arriving later).
/// </summary>
/// <param name="_scopeFactory">Factory for creating new DI scopes for background tasks</param>
/// <param name="_logger">Logger for error tracking of fire-and-forget tasks</param>
public class LLMBackgroundTaskService : ILLMBackgroundTaskService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LLMBackgroundTaskService> _logger;

    public LLMBackgroundTaskService(
        IServiceScopeFactory scopeFactory,
        ILogger<LLMBackgroundTaskService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void RunBackgroundTasks(Agent? agent, LLMConversation conversation, LLMContext context,
        string responseContent, List<LLMFunctionCall> allFunctionCalls)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var compactionService = scope.ServiceProvider.GetRequiredService<IConversationCompactionService>();
                await compactionService.CompactIfNeededAsync(conversation.ConversationId, conversation.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fire-and-forget conversation compaction failed for {ConversationId}",
                    conversation.ConversationId.ForLog());
            }
        });

        if (agent != null && !string.IsNullOrWhiteSpace(responseContent))
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var autoMemoryExtraction = scope.ServiceProvider.GetRequiredService<IAutoMemoryExtractionService>();
                    await autoMemoryExtraction.ExtractAndStoreMemoriesAsync(
                        agent.Id, context.Message, responseContent, context.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fire-and-forget auto memory extraction failed for agent {AgentId}", agent.Id);
                }
            });

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var caseCollector = scope.ServiceProvider.GetRequiredService<ISkillLearningCaseCollector>();
                    await caseCollector.CollectFromTurnAsync(new SkillLearningTurn(
                        agent.Id,
                        context.Message,
                        responseContent,
                        allFunctionCalls.Count > 0,
                        context.UserId,
                        context.ConversationId,
                        context.Language,
                        allFunctionCalls.FirstOrDefault()?.FunctionName,
                        context.AvailableFunctions.Select(function => function.Name).ToList()));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fire-and-forget learning case collection failed for agent {AgentId}", agent.Id);
                }
            });

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var trajectoryCapture = scope.ServiceProvider.GetRequiredService<ITrajectoryCaptureService>();
                    await trajectoryCapture.CaptureAsync(agent.Id, context, responseContent, allFunctionCalls);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fire-and-forget trajectory capture failed for agent {AgentId}", agent.Id);
                }
            });

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var groundingEvaluator = scope.ServiceProvider.GetRequiredService<IAnswerGroundingEvaluator>();
                    await groundingEvaluator.EvaluateAsync(agent.Id, context, responseContent, allFunctionCalls);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fire-and-forget answer grounding evaluation failed for agent {AgentId}", agent.Id);
                }
            });

            // Reflection runs on a hard negative signal only, never on a turn that went fine: a lesson
            // drawn from a successful turn is noise that later comes back as a rule. A confirmation
            // prompt sets Success=false without being a failure, so it must not count as one here.
            var failedCalls = SelectFailedCalls(allFunctionCalls);
            if (failedCalls.Count > 0)
            {
                TriggerReflection(BuildFailureReflection(agent.Id, context, failedCalls));
            }
        }
    }

    public void TriggerReflection(TurnReflectionRequest request)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var reflection = scope.ServiceProvider.GetRequiredService<ITurnReflectionService>();
                await reflection.ReflectAsync(request);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fire-and-forget turn reflection failed for agent {AgentId}", request.AgentId);
            }
        });
    }

    internal static List<LLMFunctionCall> SelectFailedCalls(IReadOnlyList<LLMFunctionCall> allFunctionCalls)
        => allFunctionCalls.Where(c => !c.Success && !c.RequiresConfirmation && !c.IsRejectedRepeat).ToList();

    private static TurnReflectionRequest BuildFailureReflection(
        Guid agentId, LLMContext context, IReadOnlyList<LLMFunctionCall> failedCalls)
    {
        var primary = failedCalls[0];
        var evidence = string.Join(
            Environment.NewLine,
            failedCalls.Select(c => $"{c.FunctionName} failed: {c.Result}"));

        return new TurnReflectionRequest(
            agentId,
            ReflectionTriggers.SkillFailure,
            context.Message,
            evidence,
            primary.FunctionName,
            Guid.TryParse(context.UserId, out var userId) ? userId : null);
    }

    public void TriggerConversationCompaction(string conversationId, string userId, int minMessages)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var compactionService = scope.ServiceProvider.GetRequiredService<IConversationCompactionService>();
                await compactionService.CompactIfNeededAsync(conversationId, userId, minMessages);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fire-and-forget task-boundary compaction failed for {ConversationId}",
                    conversationId.ForLog());
            }
        });
    }
}
