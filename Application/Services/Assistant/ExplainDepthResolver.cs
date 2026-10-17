// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves how deep Klacksy explains for a user: a persisted override memory wins, otherwise the usage heuristic
/// decides (enough own works over a long enough span, or enough successful Klacksy writes), otherwise beginner.
/// Results are cached per user: an advanced estimate for a day, a beginner estimate or an override for a few minutes;
/// a failed read falls back to beginner and is not cached.
/// </summary>
/// <param name="memoryRepository">Reads the user's persisted explanation-depth override</param>
/// <param name="signalReader">Reads the usage facts the heuristic is based on</param>
/// <param name="cache">Process-wide cache holding resolved profiles per user</param>
/// <param name="logger">Logs reads that failed and fell back to beginner</param>

using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Assistant;

public sealed class ExplainDepthResolver : IExplainDepthResolver
{
    private const int CacheEntrySize = 1;

    private static readonly ExplainDepthProfile FallbackProfile = new(ExplainExperienceLevel.Beginner, false);

    private readonly IAgentMemoryRepository _memoryRepository;
    private readonly IUserExperienceSignalReader _signalReader;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ExplainDepthResolver> _logger;

    public ExplainDepthResolver(
        IAgentMemoryRepository memoryRepository,
        IUserExperienceSignalReader signalReader,
        IMemoryCache cache,
        ILogger<ExplainDepthResolver> logger)
    {
        _memoryRepository = memoryRepository;
        _signalReader = signalReader;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ExplainDepthProfile?> ResolveAsync(string? userId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(userId, out var id) || id == Guid.Empty)
        {
            return null;
        }

        var cacheKey = CacheKey(id);
        if (_cache.TryGetValue(cacheKey, out ExplainDepthProfile? cached) && cached != null)
        {
            return cached;
        }

        ExplainDepthProfile profile;
        try
        {
            profile = await ResolveUncachedAsync(id, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Explanation depth for user {UserId} could not be resolved; using beginner", id);
            return FallbackProfile;
        }

        _cache.Set(cacheKey, profile, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheLifetime(profile))
            .SetSize(CacheEntrySize));

        return profile;
    }

    public void Invalidate(Guid userId)
    {
        _cache.Remove(CacheKey(userId));
    }

    private async Task<ExplainDepthProfile> ResolveUncachedAsync(Guid userId, CancellationToken cancellationToken)
    {
        var overrideMemory = await _memoryRepository.GetByUserAndKeyAsync(
            userId, ExplainDepthDefaults.OverrideMemoryKey, cancellationToken);
        var overrideLevel = ParseOverride(overrideMemory?.Content);
        if (overrideLevel.HasValue)
        {
            return new ExplainDepthProfile(overrideLevel.Value, IsExplicitOverride: true);
        }

        var signals = await _signalReader.ReadAsync(userId, cancellationToken);
        return new ExplainDepthProfile(EstimateLevel(signals), IsExplicitOverride: false);
    }

    private static ExplainExperienceLevel? ParseOverride(string? content)
    {
        var value = content?.Trim();
        if (string.Equals(value, ExplainDepthDefaults.OverrideValueAdvanced, StringComparison.OrdinalIgnoreCase))
        {
            return ExplainExperienceLevel.Advanced;
        }

        if (string.Equals(value, ExplainDepthDefaults.OverrideValueBeginner, StringComparison.OrdinalIgnoreCase))
        {
            return ExplainExperienceLevel.Beginner;
        }

        return null;
    }

    private static ExplainExperienceLevel EstimateLevel(UserExperienceSignals signals)
    {
        var experiencedPlanner = signals.OwnWorkCount >= ExplainDepthDefaults.MinOwnWorks
                                 && signals.ActiveSpanDays >= ExplainDepthDefaults.MinActiveSpanDays;
        var experiencedAssistantUser = signals.SuccessfulSkillWrites >= ExplainDepthDefaults.MinSuccessfulSkillWrites;

        return experiencedPlanner || experiencedAssistantUser
            ? ExplainExperienceLevel.Advanced
            : ExplainExperienceLevel.Beginner;
    }

    private static TimeSpan CacheLifetime(ExplainDepthProfile profile) =>
        profile is { Level: ExplainExperienceLevel.Advanced, IsExplicitOverride: false }
            ? TimeSpan.FromHours(ExplainDepthDefaults.AdvancedEstimateCacheHours)
            : TimeSpan.FromMinutes(ExplainDepthDefaults.CacheMinutes);

    private static string CacheKey(Guid userId) => ExplainDepthDefaults.CacheKeyPrefix + userId.ToString("N");
}
