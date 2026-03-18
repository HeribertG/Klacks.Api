// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace Klacks.Api.Domain.Services.Assistant;

/// <summary>
/// Service für asynchrone Hintergrundaufgaben nach LLM-Interaktionen (Compaction, Memory-Extraktion, Skill-Gap-Erkennung).
/// </summary>
/// <param name="_scopeFactory">Factory zum Erstellen neuer DI-Scopes für Background-Tasks</param>
/// <param name="_logger">Logger für Fehler-Tracking der Fire-and-Forget-Tasks</param>
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
                await compactionService.CompactIfNeededAsync(conversation.ConversationId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Fire-and-forget conversation compaction failed for {ConversationId}",
                    conversation.ConversationId);
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
                    var skillGapDetector = scope.ServiceProvider.GetRequiredService<ISkillGapDetector>();
                    await skillGapDetector.DetectAndSuggestAsync(
                        agent.Id, context.Message, responseContent, allFunctionCalls.Count > 0);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Fire-and-forget skill gap detection failed for agent {AgentId}", agent.Id);
                }
            });
        }
    }
}
