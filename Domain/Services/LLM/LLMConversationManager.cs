using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.LLM;

namespace Klacks.Api.Domain.Services.LLM;

public class LLMConversationManager
{
    private readonly ILogger<LLMConversationManager> _logger;
    private readonly ILLMRepository _repository;

    public LLMConversationManager(
        ILogger<LLMConversationManager> logger,
        ILLMRepository repository)
    {
        this._logger = logger;
        _repository = repository;
    }

    public async Task<LLMConversation> GetOrCreateConversationAsync(string? conversationId, string userId)
    {
        return await _repository.GetOrCreateConversationAsync(
            conversationId ?? Guid.NewGuid().ToString(),
            userId);
    }

    public async Task<List<Providers.LLMMessage>> GetConversationHistoryAsync(string conversationId)
    {
        var history = await _repository.GetConversationMessagesAsync(conversationId);
        return history.Select(m => new Providers.LLMMessage
        {
            Role = m.Role,
            Content = m.Content,
            Timestamp = m.CreateTime ?? DateTime.UtcNow
        }).ToList();
    }

    public async Task SaveConversationMessagesAsync(
        LLMConversation conversation,
        string userMessage,
        string assistantMessage,
        string modelId)
    {
        await _repository.SaveMessageAsync(new LLMMessage
        {
            ConversationId = conversation.Id,
            Role = "user",
            Content = userMessage,
            CreateTime = DateTime.UtcNow
        });

        await _repository.SaveMessageAsync(new LLMMessage
        {
            ConversationId = conversation.Id,
            Role = "assistant",
            Content = assistantMessage,
            ModelId = modelId,
            CreateTime = DateTime.UtcNow
        });

        conversation.LastMessageAt = DateTime.UtcNow;
        conversation.MessageCount += 2;
        conversation.LastModelId = modelId;

        if (conversation.MessageCount == 2)
        {
            conversation.Title = GenerateConversationTitle(userMessage, assistantMessage);
        }

        await _repository.UpdateConversationAsync(conversation);
    }

    public async Task TrackUsageAsync(
        string userId,
        LLMModel model,
        LLMConversation conversation,
        Providers.LLMUsage usage,
        long responseTimeMs,
        bool hasError = false,
        string? errorMessage = null)
    {
        await _repository.TrackUsageAsync(new LLMUsage
        {
            UserId = userId,
            ModelId = model.Id,
            ConversationId = conversation.ConversationId,
            InputTokens = usage.InputTokens,
            OutputTokens = usage.OutputTokens,
            Cost = usage.Cost,
            ResponseTimeMs = (int)responseTimeMs,
            HasError = hasError,
            ErrorMessage = errorMessage,
            CreateTime = DateTime.UtcNow
        });

        if (!hasError)
        {
            conversation.TotalTokens += usage.TotalTokens;
            conversation.TotalCost += usage.Cost;
        }
    }

    private string GenerateConversationTitle(string userMessage, string assistantMessage)
    {
        var words = userMessage.Split(' ').Take(5);
        return string.Join(' ', words) + (words.Count() >= 5 ? "..." : "");
    }
}
