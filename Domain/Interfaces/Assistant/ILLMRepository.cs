// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ILLMRepository : IBaseRepository<LLMModel>
{
    Task<List<LLMProvider>> GetProvidersAsync();
    Task<LLMProvider?> GetProviderByIdAsync(string providerId);
    Task<LLMProvider?> GetProviderAsync(Guid id);
    Task<LLMProvider> CreateProviderAsync(LLMProvider provider);
    Task<LLMProvider> UpdateProviderAsync(LLMProvider provider);
    Task<bool> DeleteProviderAsync(Guid id);
    
    Task<List<LLMModel>> GetModelsAsync(bool onlyEnabled = false);
    Task<LLMModel?> GetModelByIdAsync(string modelId);
    Task<LLMModel?> GetDefaultModelAsync();
    Task<LLMModel> CreateModelAsync(LLMModel model);
    Task<LLMModel> UpdateModelAsync(LLMModel model);
    Task SetDefaultModelAsync(string modelId);
    
    Task<LLMUsage> TrackUsageAsync(LLMUsage usage);
    Task<List<LLMUsage>> GetUserUsageAsync(string userId, DateTime fromDate, DateTime toDate);
    Task<Dictionary<string, decimal>> GetUsageSummaryByModelAsync(string userId, int days);
    Task<decimal> GetTotalCostAsync(string userId, int days);
    
    Task<LLMConversation> GetOrCreateConversationAsync(string conversationId, string userId);
    Task<List<LLMConversation>> GetUserConversationsAsync(string userId, int limit, int offset);
    Task<LLMConversation> UpdateConversationAsync(LLMConversation conversation);
    Task<bool> ArchiveConversationAsync(string conversationId, string userId);
    
    Task<LLMMessage> SaveMessageAsync(LLMMessage message);
    Task<List<LLMMessage>> GetConversationMessagesAsync(string conversationId, int limit = 20);
    Task<int> GetConversationTokenCountAsync(string conversationId);
}