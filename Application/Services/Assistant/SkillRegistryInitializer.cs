// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Scoped service that loads AgentSkill entries from the database, matches them to
/// C# implementations via [SkillImplementation] attribute, and reloads the ISkillRegistry.
/// </summary>
/// <param name="skillRepository">Repository to query all enabled AgentSkill records</param>
/// <param name="skillRegistry">Singleton registry that will be reloaded with the assembled descriptors</param>
/// <param name="logger">Logger for initialization diagnostics</param>

using System.Reflection;
using System.Text.Json;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Assistant;

public class SkillRegistryInitializer
{
    private readonly IAgentSkillRepository _skillRepository;
    private readonly ISkillRegistry _skillRegistry;
    private readonly ILogger<SkillRegistryInitializer> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public SkillRegistryInitializer(
        IAgentSkillRepository skillRepository,
        ISkillRegistry skillRegistry,
        ILogger<SkillRegistryInitializer> logger)
    {
        _skillRepository = skillRepository;
        _skillRegistry = skillRegistry;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var implementations = ScanAssemblyForImplementations();
        var agentSkills = await _skillRepository.GetAllEnabledAsync(cancellationToken);

        var descriptors = new List<SkillDescriptor>(agentSkills.Count);
        var matched = 0;

        foreach (var agentSkill in agentSkills)
        {
            var implementationType = implementations.TryGetValue(agentSkill.Name, out var type) ? type : null;

            if (implementationType != null)
            {
                matched++;
            }

            var descriptor = new SkillDescriptor(
                Name: agentSkill.Name,
                Description: agentSkill.Description,
                Category: ParseCategory(agentSkill.Category),
                Parameters: ParseParameters(agentSkill.ParametersJson, agentSkill.Name),
                RequiredPermissions: ParseRequiredPermissions(agentSkill.RequiredPermission),
                RequiredCapabilities: Array.Empty<LLMCapability>(),
                ImplementationType: implementationType)
            {
                ExecutionType = agentSkill.ExecutionType,
                HandlerType = agentSkill.HandlerType,
                HandlerConfig = agentSkill.HandlerConfig
            };

            descriptors.Add(descriptor);
        }

        _skillRegistry.Reload(descriptors);

        _logger.LogInformation(
            "Initialized {Count} skills ({Matched} with implementations, {Unmatched} DB-only)",
            descriptors.Count,
            matched,
            descriptors.Count - matched);
    }

    private static Dictionary<string, Type> ScanAssemblyForImplementations()
    {
        var assembly = typeof(SkillRegistryInitializer).Assembly;

        return assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<SkillImplementationAttribute>() != null && !t.IsAbstract)
            .ToDictionary(
                t => t.GetCustomAttribute<SkillImplementationAttribute>()!.SkillName,
                t => t,
                StringComparer.OrdinalIgnoreCase);
    }

    private static SkillCategory ParseCategory(string category)
    {
        return Enum.TryParse<SkillCategory>(category, ignoreCase: true, out var result)
            ? result
            : SkillCategory.Action;
    }

    private IReadOnlyList<SkillParameter> ParseParameters(string parametersJson, string skillName)
    {
        if (string.IsNullOrWhiteSpace(parametersJson) || parametersJson == "[]")
        {
            return Array.Empty<SkillParameter>();
        }

        try
        {
            var rawParameters = JsonSerializer.Deserialize<List<RawSkillParameter>>(parametersJson, JsonOptions);

            if (rawParameters == null)
            {
                return Array.Empty<SkillParameter>();
            }

            return rawParameters
                .Select(p => new SkillParameter(
                    Name: p.Name ?? string.Empty,
                    Description: p.Description ?? string.Empty,
                    Type: Enum.TryParse<SkillParameterType>(p.Type, ignoreCase: true, out var paramType)
                        ? paramType
                        : SkillParameterType.String,
                    Required: p.Required,
                    DefaultValue: p.DefaultValue,
                    EnumValues: p.EnumValues,
                    JsonSchema: p.JsonSchema))
                .ToList();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse ParametersJson for skill {SkillName}", skillName);
            return Array.Empty<SkillParameter>();
        }
    }

    private static IReadOnlyList<string> ParseRequiredPermissions(string? requiredPermission)
    {
        if (string.IsNullOrWhiteSpace(requiredPermission))
        {
            return Array.Empty<string>();
        }

        return requiredPermission
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private sealed class RawSkillParameter
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Type { get; set; }
        public bool Required { get; set; }
        public object? DefaultValue { get; set; }
        public List<string>? EnumValues { get; set; }
        public string? JsonSchema { get; set; }
    }
}
