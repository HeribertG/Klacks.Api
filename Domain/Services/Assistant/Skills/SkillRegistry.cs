using System.Collections.Concurrent;
using System.Reflection;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Adapters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Domain.Services.Assistant.Skills;

public class SkillRegistry : ISkillRegistry
{
    private readonly ConcurrentDictionary<string, ISkill> _skills = new(StringComparer.OrdinalIgnoreCase);
    private readonly IServiceProvider _serviceProvider;
    private readonly ISkillAdapterFactory _adapterFactory;
    private readonly ILogger<SkillRegistry> _logger;
    private readonly IMemoryCache _cache;
    private readonly MemoryCacheEntryOptions _cacheOptions;

    public SkillRegistry(
        IServiceProvider serviceProvider,
        ISkillAdapterFactory adapterFactory,
        ILogger<SkillRegistry> logger,
        IMemoryCache cache)
    {
        _serviceProvider = serviceProvider;
        _adapterFactory = adapterFactory;
        _logger = logger;
        _cache = cache;
        _cacheOptions = new MemoryCacheEntryOptions()
            .SetSlidingExpiration(TimeSpan.FromMinutes(5))
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));
    }

    public void Register(ISkill skill)
    {
        if (_skills.TryAdd(skill.Name, skill))
        {
            _logger.LogInformation("Registered skill: {SkillName} ({Category})", skill.Name, skill.Category);
        }
        else
        {
            _logger.LogWarning("Skill already registered: {SkillName}", skill.Name);
        }
    }

    public void RegisterFromAssembly(Assembly assembly)
    {
        var skillTypes = assembly.GetTypes()
            .Where(t => typeof(ISkill).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

        foreach (var type in skillTypes)
        {
            try
            {
                var skill = (ISkill)ActivatorUtilities.CreateInstance(_serviceProvider, type);
                Register(skill);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register skill from type: {TypeName}", type.FullName);
            }
        }

        _logger.LogInformation("Registered {Count} skills from assembly: {AssemblyName}",
            skillTypes.Count(), assembly.GetName().Name);
    }

    public IReadOnlyList<ISkill> GetAllSkills()
    {
        return _skills.Values.ToList();
    }

    public IReadOnlyList<ISkill> GetSkillsForUser(IReadOnlyList<string> userPermissions)
    {
        if (userPermissions.Contains("Admin"))
        {
            return _skills.Values.ToList();
        }

        return _skills.Values
            .Where(s => s.RequiredPermissions.All(rp => userPermissions.Contains(rp)))
            .ToList();
    }

    public IReadOnlyList<ISkill> GetSkillsByCategory(SkillCategory category)
    {
        return _skills.Values
            .Where(s => s.Category == category)
            .ToList();
    }

    public ISkill? GetSkillByName(string name)
    {
        _skills.TryGetValue(name, out var skill);
        return skill;
    }

    public IReadOnlyList<object> ExportAsProviderFormat(
        LLMProviderType provider,
        IReadOnlyList<string> userPermissions)
    {
        var permissionsHash = string.Join(",", userPermissions.OrderBy(p => p));
        var cacheKey = $"skills_export_{provider}_{permissionsHash.GetHashCode()}";

        return _cache.GetOrCreate(cacheKey, entry =>
        {
            entry.SetOptions(_cacheOptions);

            _logger.LogDebug("Cache miss for skill export: Provider={Provider}, PermissionsHash={Hash}",
                provider, permissionsHash.GetHashCode());

            var adapter = _adapterFactory.GetAdapter(provider);
            var skills = GetSkillsForUser(userPermissions);

            return skills
                .Select(s => adapter.ConvertSkillToProviderFormat(s))
                .ToList();
        })!;
    }

    public void InvalidateCache()
    {
        _logger.LogInformation("Invalidating skill registry cache");
    }
}
