// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Caches default agent and enabled skills to avoid DB queries on every chat request.
/// Uses IServiceScopeFactory to resolve scoped repositories when cache misses occur.
/// </summary>
/// <param name="cache">In-memory cache with 5 minute TTL</param>
/// <param name="scopeFactory">Creates new DI scopes for DB access on cache miss</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Assistant;

public interface ISkillCacheService
{
    Task<Agent?> GetDefaultAgentAsync(CancellationToken ct = default);
    Task<IReadOnlyList<AgentSkill>> GetEnabledSkillsAsync(Guid agentId, CancellationToken ct = default);

    /// <summary>
    /// Every enabled, non-deleted skill of every agent — the exact result of
    /// IAgentSkillRepository.GetAllEnabledAsync, cached. Deliberately not expressed through
    /// GetEnabledSkillsAsync: that one is scoped to a single agent and does not filter soft-deleted
    /// rows, so the two are not interchangeable.
    /// </summary>
    Task<IReadOnlyList<AgentSkill>> GetAllEnabledSkillsAsync(CancellationToken ct = default);

    void InvalidateCache();
    Task WarmupAsync(CancellationToken ct = default);
}

public class SkillCacheService : ISkillCacheService
{
    private readonly IMemoryCache _cache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SkillCacheService> _logger;

    private const string AgentCacheKey = "skill_cache_default_agent";
    private const string SkillsCacheKeyPrefix = "skill_cache_skills_";
    private const string AllSkillsCacheKeyPrefix = "skill_cache_all_enabled_skills_v";
    private const int CacheMinutes = 5;

    private int _cacheVersion;

    public SkillCacheService(
        IMemoryCache cache,
        IServiceScopeFactory scopeFactory,
        ILogger<SkillCacheService> logger)
    {
        _cache = cache;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<Agent?> GetDefaultAgentAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(AgentCacheKey, out Agent? cached))
            return cached;

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAgentRepository>();
        var agent = await repo.GetDefaultAgentAsync(ct);

        if (agent != null)
        {
            _cache.Set(AgentCacheKey, agent, new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheMinutes))
                .SetSize(1));
        }

        return agent;
    }

    public async Task<IReadOnlyList<AgentSkill>> GetEnabledSkillsAsync(Guid agentId, CancellationToken ct = default)
    {
        var cacheKey = GetSkillsCacheKey(agentId);

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<AgentSkill>? cached) && cached != null)
            return cached;

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAgentSkillRepository>();
        var skills = await repo.GetEnabledAsync(agentId, ct);

        var skillList = skills as IReadOnlyList<AgentSkill> ?? skills.ToList();

        _cache.Set(cacheKey, skillList, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheMinutes))
            .SetSize(1));

        _logger.LogDebug("Cached {Count} skills for agent {AgentId}", skillList.Count, agentId);
        return skillList;
    }

    public async Task<IReadOnlyList<AgentSkill>> GetAllEnabledSkillsAsync(CancellationToken ct = default)
    {
        var cacheKey = GetAllSkillsCacheKey();

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<AgentSkill>? cached) && cached != null)
            return cached;

        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IAgentSkillRepository>();
        var skills = await repo.GetAllEnabledAsync(ct);

        _cache.Set(cacheKey, (IReadOnlyList<AgentSkill>)skills, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheMinutes))
            .SetSize(1));

        _logger.LogDebug("Cached {Count} enabled skills across all agents", skills.Count);
        return skills;
    }

    public void InvalidateCache()
    {
        Interlocked.Increment(ref _cacheVersion);
        _cache.Remove(AgentCacheKey);
        _logger.LogInformation("Skill cache invalidated (version {Version})", _cacheVersion);
    }

    public async Task WarmupAsync(CancellationToken ct = default)
    {
        var agent = await GetDefaultAgentAsync(ct);
        if (agent != null)
        {
            await GetEnabledSkillsAsync(agent.Id, ct);
        }
        _logger.LogInformation("Skill cache warmed up");
    }

    private string GetSkillsCacheKey(Guid agentId) => $"{SkillsCacheKeyPrefix}{agentId}_v{_cacheVersion}";

    private string GetAllSkillsCacheKey() => $"{AllSkillsCacheKeyPrefix}{_cacheVersion}";
}
