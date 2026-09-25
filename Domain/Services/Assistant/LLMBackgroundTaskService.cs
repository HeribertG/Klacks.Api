// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Klacks.Api.Domain.Services.Assistant.Skills;
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
        string responseContent, List<LLMFunctionCall> allFunctionCalls, bool answeredWithNotice = false)
    {
        RunDetached<IConversationCompactionService>(
            compaction => compaction.CompactIfNeededAsync(conversation.ConversationId, conversation.UserId),
            "Fire-and-forget conversation compaction failed for {ConversationId}", conversation.ConversationId.ForLog());

        if (agent == null || string.IsNullOrWhiteSpace(responseContent))
        {
            return;
        }

        // An empty-answer notice is a canned sentence, not something the assistant concluded: remembering
        // it, reading it as a refusal or grading it against the tool results would each learn noise. The
        // hooks below that never read the answer text keep running, so the audit trail, the trajectory
        // (which also labels the PREVIOUS turn from this turn's message) and failure reflection stay intact.
        if (!answeredWithNotice)
        {
            RunAnswerTextTasks(agent, context, responseContent, allFunctionCalls);
        }

        RunDetached<IAgentSkillRepository>(
            skillRepository => LogSkillExecutionsAsync(skillRepository, agent, conversation, context, allFunctionCalls),
            "Fire-and-forget skill execution logging failed for agent {AgentId}", agent.Id);

        RunDetached<ITrajectoryCaptureService>(
            trajectoryCapture => trajectoryCapture.CaptureAsync(agent.Id, context, responseContent, allFunctionCalls),
            "Fire-and-forget trajectory capture failed for agent {AgentId}", agent.Id);

        // Reflection runs on a hard negative signal only, never on a turn that went fine: a lesson
        // drawn from a successful turn is noise that later comes back as a rule. A confirmation
        // prompt sets Success=false without being a failure, so it must not count as one here.
        var failedCalls = SelectFailedCalls(allFunctionCalls);
        if (failedCalls.Count > 0)
        {
            TriggerReflection(BuildFailureReflection(agent.Id, context, failedCalls));
        }
    }

    public void RunStoppedTurnTasks(Agent? agent, LLMConversation conversation, LLMContext context,
        string responseContent, List<LLMFunctionCall> executedCalls, string interruptedPhase)
    {
        RunDetached<IConversationCompactionService>(
            compaction => compaction.CompactIfNeededAsync(conversation.ConversationId, conversation.UserId),
            "Fire-and-forget conversation compaction failed for {ConversationId}", conversation.ConversationId.ForLog());

        if (agent == null)
        {
            return;
        }

        RunDetached<IAgentSkillRepository>(
            skillRepository => LogSkillExecutionsAsync(skillRepository, agent, conversation, context, executedCalls),
            "Fire-and-forget skill execution logging failed for agent {AgentId}", agent.Id);

        RunDetached<ITrajectoryCaptureService>(
            trajectoryCapture => trajectoryCapture.CaptureAsync(
                agent.Id, context, responseContent, executedCalls, interruptedPhase),
            "Fire-and-forget trajectory capture failed for agent {AgentId}", agent.Id);
    }

    /// <summary>
    /// The hooks that consume the answer text itself: auto-memory extraction, learning-case collection and
    /// answer grounding.
    /// </summary>
    private void RunAnswerTextTasks(
        Agent agent, LLMContext context, string responseContent, List<LLMFunctionCall> allFunctionCalls)
    {
        RunDetached<IAutoMemoryExtractionService>(
            autoMemoryExtraction => autoMemoryExtraction.ExtractAndStoreMemoriesAsync(
                agent.Id, context.Message, responseContent, context.UserId),
            "Fire-and-forget auto memory extraction failed for agent {AgentId}", agent.Id);

        RunDetached<ISkillLearningCaseCollector>(
            caseCollector => caseCollector.CollectFromTurnAsync(new SkillLearningTurn(
                agent.Id,
                context.Message,
                responseContent,
                allFunctionCalls.Count > 0,
                context.UserId,
                context.ConversationId,
                context.Language,
                allFunctionCalls.FirstOrDefault()?.FunctionName,
                context.AvailableFunctions.Select(function => function.Name).ToList())),
            "Fire-and-forget learning case collection failed for agent {AgentId}", agent.Id);

        RunDetached<IAnswerGroundingEvaluator>(
            groundingEvaluator => groundingEvaluator.EvaluateAsync(agent.Id, context, responseContent, allFunctionCalls),
            "Fire-and-forget answer grounding evaluation failed for agent {AgentId}", agent.Id);
    }

    // W1.7: agent_skill_executions is the audit trail consumed by the trigger stack
    // (lock-conflict detector, verify/rollback my last action). It was never written;
    // fill it once per executed tool call. Hallucinated names have no FK target and
    // are skipped — skill_usage_records already logs them as NotFound failures.
    private static async Task LogSkillExecutionsAsync(
        IAgentSkillRepository skillRepository, Agent agent, LLMConversation conversation, LLMContext context,
        List<LLMFunctionCall> allFunctionCalls)
    {
        var sessionId = Guid.TryParse(conversation.ConversationId, out var parsedSession)
            ? parsedSession
            : Guid.Empty;

        foreach (var call in allFunctionCalls)
        {
            if (string.IsNullOrWhiteSpace(call.FunctionName))
            {
                continue;
            }

            var skill = await skillRepository.GetByNameAsync(agent.Id, call.FunctionName);
            if (skill == null)
            {
                continue;
            }

            var errorMessage = call.Success
                ? null
                : InternalIdentifierRedactor.Redact(call.Result);

            await skillRepository.LogExecutionAsync(new AgentSkillExecution
            {
                AgentId = agent.Id,
                SkillId = skill.Id,
                SessionId = sessionId,
                UserId = context.UserId,
                ToolName = call.FunctionName,
                ParametersJson = JsonSerializer.Serialize(SkillParameterRedactor.Redact(call.Parameters)),
                Success = call.Success,
                ErrorMessage = errorMessage,
                DurationMs = 0,
                TriggeredBy = "agent"
            });
        }
    }

    /// <summary>
    /// Runs one post-turn hook fire-and-forget in its own DI scope and logs, never throws, when it fails.
    /// </summary>
    /// <param name="work">The hook, given its service resolved from the new scope.</param>
    /// <param name="failureMessage">Log template for a failure; carries exactly one placeholder.</param>
    /// <param name="subject">Value of that placeholder, the conversation or agent the hook ran for.</param>
    private void RunDetached<TService>(Func<TService, Task> work, string failureMessage, object subject)
        where TService : notnull
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await work(scope.ServiceProvider.GetRequiredService<TService>());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, failureMessage, subject);
            }
        });
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
        => allFunctionCalls.Where(c => !c.Success && !c.RequiresConfirmation && !c.IsRejectedRepeat && !c.SkippedByStop).ToList();

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
