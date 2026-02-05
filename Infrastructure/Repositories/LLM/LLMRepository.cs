using Microsoft.EntityFrameworkCore;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.LLM;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Infrastructure.Repositories.LLM;

public class LLMRepository : BaseRepository<LLMModel>, ILLMRepository
{
    protected readonly DataBaseContext _context;

    public LLMRepository(DataBaseContext context, ILogger<LLMModel> logger) : base(context, logger)
    {
        _context = context;
    }

    // Provider methods
    public async Task<List<LLMProvider>> GetProvidersAsync()
    {
        return await _context.Set<LLMProvider>()
            .Where(p => !p.IsDeleted)
            .Include(p => p.Models)
            .OrderBy(p => p.Priority)
            .ToListAsync();
    }

    public async Task<LLMProvider?> GetProviderByIdAsync(string providerId)
    {
        return await _context.Set<LLMProvider>()
            .Where(p => !p.IsDeleted && p.ProviderId == providerId)
            .Include(p => p.Models)
            .FirstOrDefaultAsync();
    }

     public async Task<LLMProvider> CreateProviderAsync(LLMProvider provider)
    {
        _context.Set<LLMProvider>().Add(provider);
        await _context.SaveChangesAsync();
        return provider;
    }

    public async Task<LLMProvider> UpdateProviderAsync(LLMProvider provider)
    {
        _context.Set<LLMProvider>().Update(provider);
        await _context.SaveChangesAsync();
        return provider;
    }

    public async Task<LLMProvider?> GetProviderAsync(Guid id)
    {
        return await _context.Set<LLMProvider>()
            .Where(p => !p.IsDeleted && p.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteProviderAsync(Guid id)
    {
        var provider = await GetProviderAsync(id);
        if (provider != null)
        {
            provider.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }
        return false;
    }

    // Model methods (erweiterte BaseRepository Funktionalität)
    public async Task<List<LLMModel>> GetModelsAsync(bool onlyEnabled = false)
    {
        var query = _context.Set<LLMModel>()
            .Where(m => !m.IsDeleted)
            .Join(_context.Set<LLMProvider>(),
                model => model.ProviderId,
                provider => provider.ProviderId,
                (model, provider) => new { Model = model, Provider = provider })
            .Where(joined => !joined.Provider.IsDeleted && joined.Provider.IsEnabled)
            .Select(joined => joined.Model)
            .AsQueryable();

        if (onlyEnabled)
        {
            query = query.Where(m => m.IsEnabled);
        }

        return await query
            .OrderBy(m => m.ProviderId)
            .ThenBy(m => m.ModelName)
            .ToListAsync();
    }

    public async Task<LLMModel?> GetModelByIdAsync(string modelId)
    {
        return await _context.Set<LLMModel>()
            .Where(m => !m.IsDeleted && m.ModelId == modelId)
            .FirstOrDefaultAsync();
    }

    public async Task<LLMModel?> GetDefaultModelAsync()
    {
        return await _context.Set<LLMModel>()
            .Where(m => !m.IsDeleted && m.IsDefault && m.IsEnabled)
            .FirstOrDefaultAsync();
    }

    public async Task<LLMModel> CreateModelAsync(LLMModel model)
    {
        _context.Set<LLMModel>().Add(model);
        await _context.SaveChangesAsync();
        return model;
    }

    public async Task<LLMModel> UpdateModelAsync(LLMModel model)
    {
        _context.Set<LLMModel>().Update(model);
        await _context.SaveChangesAsync();
        return model;
    }

    public async Task SetDefaultModelAsync(string modelId)
    {
        var allModels = await _context.Set<LLMModel>()
            .Where(m => !m.IsDeleted && m.IsDefault)
            .ToListAsync();

        foreach (var model in allModels)
        {
            model.IsDefault = false;
        }

        var targetModel = await _context.Set<LLMModel>()
            .FirstOrDefaultAsync(m => !m.IsDeleted && m.ModelId == modelId);

        if (targetModel != null)
        {
            targetModel.IsDefault = true;
        }

        await _context.SaveChangesAsync();
    }

    public override async Task<LLMModel?> Delete(Guid id)
    {
        var model = await _context.Set<LLMModel>()
            .FirstOrDefaultAsync(m => m.Id == id);
            
        if (model == null)
        {
            return null;
        }

        if (model.IsDefault)
        {
            throw new InvalidOperationException("Cannot delete default model");
        }

        model.IsDeleted = true;
        model.DeletedTime = DateTime.UtcNow;
        
        _context.Set<LLMModel>().Update(model);
        await _context.SaveChangesAsync();
        
        return model;
    }

    // Usage tracking
    public async Task<LLMUsage> TrackUsageAsync(LLMUsage usage)
    {
        _context.Set<LLMUsage>().Add(usage);
        await _context.SaveChangesAsync();
        return usage;
    }

    public async Task<List<LLMUsage>> GetUserUsageAsync(string userId, DateTime fromDate, DateTime toDate)
    {
        return await _context.Set<LLMUsage>()
            .Where(u => !u.IsDeleted && 
                       u.UserId == userId && 
                       u.CreateTime >= fromDate && 
                       u.CreateTime <= toDate)
            .Include(u => u.Model)
            .OrderByDescending(u => u.CreateTime)
            .ToListAsync();
    }

    public async Task<Dictionary<string, decimal>> GetUsageSummaryByModelAsync(string userId, int days)
    {
        var fromDate = DateTime.UtcNow.AddDays(-days);
        
        return await _context.Set<LLMUsage>()
            .Where(u => !u.IsDeleted && 
                       u.UserId == userId && 
                       u.CreateTime >= fromDate)
            .Include(u => u.Model)
            .GroupBy(u => u.Model.ModelId)
            .Select(g => new { ModelId = g.Key, TotalCost = g.Sum(u => u.Cost) })
            .ToDictionaryAsync(x => x.ModelId, x => x.TotalCost);
    }

    public async Task<decimal> GetTotalCostAsync(string userId, int days)
    {
        var fromDate = DateTime.UtcNow.AddDays(-days);
        
        return await _context.Set<LLMUsage>()
            .Where(u => !u.IsDeleted && 
                       u.UserId == userId && 
                       u.CreateTime >= fromDate)
            .SumAsync(u => u.Cost);
    }

    // Conversation management
    public async Task<LLMConversation> GetOrCreateConversationAsync(string conversationId, string userId)
    {
        var conversation = await _context.Set<LLMConversation>()
            .Where(c => !c.IsDeleted && c.ConversationId == conversationId)
            .FirstOrDefaultAsync();

        if (conversation == null)
        {
            conversation = new LLMConversation
            {
                ConversationId = conversationId,
                UserId = userId,
                LastMessageAt = DateTime.UtcNow,
                MessageCount = 0,
                TotalTokens = 0,
                TotalCost = 0
            };

            _context.Set<LLMConversation>().Add(conversation);
            await _context.SaveChangesAsync();
        }

        return conversation;
    }

    public async Task<List<LLMConversation>> GetUserConversationsAsync(string userId, int limit, int offset)
    {
        return await _context.Set<LLMConversation>()
            .Where(c => !c.IsDeleted && c.UserId == userId && !c.IsArchived)
            .OrderByDescending(c => c.LastMessageAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<LLMConversation> UpdateConversationAsync(LLMConversation conversation)
    {
        _context.Set<LLMConversation>().Update(conversation);
        await _context.SaveChangesAsync();
        return conversation;
    }

    public async Task<bool> ArchiveConversationAsync(string conversationId, string userId)
    {
        var conversation = await _context.Set<LLMConversation>()
            .FirstOrDefaultAsync(c => !c.IsDeleted && c.ConversationId == conversationId && c.UserId == userId);

        if (conversation == null)
            return false;

        conversation.IsArchived = true;
        await _context.SaveChangesAsync();
        return true;
    }

    // Message history
    public async Task<Domain.Models.LLM.LLMMessage> SaveMessageAsync(Domain.Models.LLM.LLMMessage message)
    {
        _context.Set<Domain.Models.LLM.LLMMessage>().Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<List<Domain.Models.LLM.LLMMessage>> GetConversationMessagesAsync(string conversationId, int limit = 20)
    {
        var conversation = await _context.Set<LLMConversation>()
            .Where(c => !c.IsDeleted && c.ConversationId == conversationId)
            .FirstOrDefaultAsync();

        if (conversation == null)
        {
            return new List<Domain.Models.LLM.LLMMessage>();
        }

        return await _context.Set<Domain.Models.LLM.LLMMessage>()
            .Where(m => !m.IsDeleted && m.ConversationId == conversation.Id)
            .OrderBy(m => m.CreateTime)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> GetConversationTokenCountAsync(string conversationId)
    {
        var conversation = await _context.Set<LLMConversation>()
            .Where(c => !c.IsDeleted && c.ConversationId == conversationId)
            .FirstOrDefaultAsync();

        return conversation?.TotalTokens ?? 0;
    }
}