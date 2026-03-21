// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Singleton registry that holds all registered SkillDescriptors and exports them
/// in provider-specific formats. Supports atomic reload from DB-driven descriptors.
/// </summary>

using System.Collections.Concurrent;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Adapters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Domain.Services.Assistant.Skills;

public class SkillRegistry : ISkillRegistry
{
    private volatile ConcurrentDictionary<string, SkillDescriptor> _skills = new(StringComparer.OrdinalIgnoreCase);
    private volatile int _cacheVersion = 0;
    private readonly ISkillAdapterFactory _adapterFactory;
    private readonly ILogger<SkillRegistry> _logger;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public SkillRegistry(
        ISkillAdapterFactory adapterFactory,
        ILogger<SkillRegistry> logger,
        IMemoryCache cache)
    {
        _adapterFactory = adapterFactory;
        _logger = logger;
        _cache = cache;
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5))
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
    }

    public void Register(SkillDescriptor descriptor)
    {
        if (_skills.TryAdd(descriptor.Name, descriptor))
        {
            _logger.LogInformation("Registered skill: {SkillName} ({Category})", descriptor.Name, descriptor.Category);
        }
        else
        {
            _logger.LogWarning("Skill already registered: {SkillName}", descriptor.Name);
        }
    }

    public void Reload(IReadOnlyList<SkillDescriptor> descriptors)
    {
        var newDictionary = new ConcurrentDictionary<string, SkillDescriptor>(StringComparer.OrdinalIgnoreCase);

        foreach (var descriptor in descriptors)
        {
            newDictionary[descriptor.Name] = descriptor;
        }

        _skills = newDictionary;
        Interlocked.Increment(ref _cacheVersion);

        _logger.LogInformation("SkillRegistry reloaded with {Count} skills", descriptors.Count);
    }

    public void Clear()
    {
        _skills = new ConcurrentDictionary<string, SkillDescriptor>(StringComparer.OrdinalIgnoreCase);
        Interlocked.Increment(ref _cacheVersion);

        _logger.LogInformation("SkillRegistry cleared");
    }

    public IReadOnlyList<SkillDescriptor> GetAllSkills()
    {
        return _skills.Values.ToList();
    }

    public IReadOnlyList<SkillDescriptor> GetSkillsForUser(IReadOnlyList<string> userPermissions)
    {
        if (userPermissions.Contains(Roles.Admin))
        {
            return _skills.Values.ToList();
        }

        return _skills.Values
            .Where(s => s.RequiredPermissions.All(rp => userPermissions.Contains(rp)))
            .ToList();
    }

    public SkillDescriptor? GetSkillByName(string name)
    {
        _skills.TryGetValue(name, out var descriptor);
        return descriptor;
    }

    public IReadOnlyList<object> ExportAsProviderFormat(
        LLMProviderType provider,
        IReadOnlyList<string> userPermissions)
    {
        var permissionsHash = string.Join(",", userPermissions.OrderBy(p => p));
        var cacheKey = $"skills_export_v{_cacheVersion}_{provider}_{permissionsHash}";

        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_cacheOptions);

            _logger.LogDebug("Cache miss for skill export: Provider={Provider}, Permissions={Permissions}",
                provider, permissionsHash);

            var adapter = _adapterFactory.GetAdapter(provider);
            var descriptors = GetSkillsForUser(userPermissions);

            return descriptors
                .Select(s => adapter.ConvertSkillToProviderFormat(s))
                .ToList();
        })!;
    }
}
