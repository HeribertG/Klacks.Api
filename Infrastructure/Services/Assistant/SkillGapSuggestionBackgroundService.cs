// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Background service that periodically reviews accumulated SkillGapRecords, asks the LLM to
/// generate a concrete skill proposal, and notifies connected admins via SignalR.
/// </summary>

using System.Text;
using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Infrastructure.Services.Assistant;

public class SkillGapSuggestionBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SkillGapSuggestionBackgroundService> _logger;

    private const int IntervalHours = 6;
    private const int InitialDelayMinutes = 10;
    private const int MinOccurrences = 3;

    public SkillGapSuggestionBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SkillGapSuggestionBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Skill gap suggestion background service started");

        try
        {
            await Task.Delay(TimeSpan.FromMinutes(InitialDelayMinutes), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPendingGapsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in skill gap suggestion background service");
                }

                await Task.Delay(TimeSpan.FromHours(IntervalHours), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("Skill gap suggestion background service stopped");
    }

    private async Task ProcessPendingGapsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var skillGapRepository = scope.ServiceProvider.GetRequiredService<ISkillGapRepository>();
        var agentRepository = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
        var llmService = scope.ServiceProvider.GetRequiredService<ILLMService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<IAssistantNotificationService>();

        var agent = await agentRepository.GetDefaultAgentAsync(stoppingToken);
        if (agent == null)
        {
            return;
        }

        var gaps = await skillGapRepository.GetPendingAsync(agent.Id, MinOccurrences, stoppingToken);
        if (gaps.Count == 0)
        {
            return;
        }

        _logger.LogInformation("Processing {Count} pending skill gaps for agent {AgentId}", gaps.Count, agent.Id);

        foreach (var gap in gaps)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }

            await ProcessSingleGapAsync(gap, llmService, skillGapRepository, notificationService, stoppingToken);
        }
    }

    private async Task ProcessSingleGapAsync(
        SkillGapRecord gap,
        ILLMService llmService,
        ISkillGapRepository skillGapRepository,
        IAssistantNotificationService notificationService,
        CancellationToken stoppingToken)
    {
        try
        {
            var prompt = BuildLlmPrompt(gap);

            var context = new LLMContext
            {
                Message = prompt,
                UserId = "system",
                ConversationId = null,
                Language = "en",
                AvailableFunctions = []
            };

            var response = await llmService.ProcessAsync(context);

            if (string.IsNullOrWhiteSpace(response.Message))
            {
                _logger.LogWarning("LLM did not return a suggestion for skill gap {GapId}", gap.Id);
                return;
            }

            var suggestion = ParseLlmSuggestion(response.Message);

            gap.SuggestedSkillName = suggestion.Name;
            gap.SuggestedDescription = suggestion.Description;
            gap.Status = SkillGapStatuses.Suggested;
            gap.UpdateTime = DateTime.UtcNow;

            await skillGapRepository.UpdateAsync(gap);

            var adminMessage = BuildAdminNotification(gap);
            foreach (var adminId in notificationService.GetConnectedUserIds())
            {
                await notificationService.SendProactiveMessageAsync(adminId, adminMessage);
            }

            _logger.LogInformation("Skill gap {GapId} suggested as '{SkillName}'", gap.Id, gap.SuggestedSkillName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to process skill gap {GapId}", gap.Id);
        }
    }

    private static string BuildLlmPrompt(SkillGapRecord gap)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an AI assistant helping to design chatbot skills.");
        sb.AppendLine("A user has repeatedly asked for something the chatbot could not fulfill.");
        sb.AppendLine();
        sb.AppendLine($"Recurring request (occurred {gap.OccurrenceCount} times):");
        sb.AppendLine($"User message: \"{gap.UserMessage}\"");
        sb.AppendLine($"Detected intent: \"{gap.DetectedIntent}\"");
        sb.AppendLine();
        sb.AppendLine("Propose a new chatbot skill in valid JSON:");
        sb.AppendLine("{");
        sb.AppendLine("  \"name\": \"<snake_case_skill_name>\",");
        sb.AppendLine("  \"description\": \"<one sentence LLM-facing description>\"");
        sb.AppendLine("}");
        sb.AppendLine("Return ONLY the JSON object, no other text.");
        return sb.ToString();
    }

    private static (string Name, string Description) ParseLlmSuggestion(string content)
    {
        try
        {
            var json = content.Trim();
            var start = json.IndexOf('{');
            var end = json.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                json = json[start..(end + 1)];
            }

            using var doc = JsonDocument.Parse(json);
            var name = doc.RootElement.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "" : "";
            var description = doc.RootElement.TryGetProperty("description", out var descProp) ? descProp.GetString() ?? "" : "";
            return (name, description);
        }
        catch
        {
            return ("suggested_skill", content.Length > 200 ? content[..200] : content);
        }
    }

    private static string BuildAdminNotification(SkillGapRecord gap)
    {
        var sb = new StringBuilder();
        sb.AppendLine("**New Skill Suggestion**");
        sb.AppendLine($"Users have repeatedly asked for something I cannot do ({gap.OccurrenceCount} times).");
        sb.AppendLine($"Request: \"{gap.DetectedIntent}\"");
        if (!string.IsNullOrWhiteSpace(gap.SuggestedSkillName))
        {
            sb.AppendLine($"Suggested skill name: `{gap.SuggestedSkillName}`");
        }
        sb.AppendLine("Use `review_skill_suggestions` to view and act on pending suggestions.");
        return sb.ToString();
    }
}
