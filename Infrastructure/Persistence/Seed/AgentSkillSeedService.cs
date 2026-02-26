// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Persistence.Seed;

public class AgentSkillSeedService
{
    private readonly ISkillRegistry _skillRegistry;
    private readonly IAgentRepository _agentRepository;
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly ILogger<AgentSkillSeedService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static readonly Dictionary<string, string> NavigateToRoutes = new()
    {
        { "dashboard", "/workplace/dashboard" },
        { "employees", "/workplace/client" },
        { "employee-details", "/workplace/client" },
        { "schedule", "/workplace/schedule" },
        { "absences", "/workplace/absence" },
        { "reports", "/workplace/dashboard" },
        { "settings", "/workplace/settings" },
        { "groups", "/workplace/group" },
        { "contracts", "/workplace/client" },
        { "holidays", "/workplace/settings" }
    };

    private const string NavigateToSkillName = "navigate_to";
    private const string UpdateGeneralSettingsName = "update_general_settings";
    private const string UpdateOwnerAddressName = "update_owner_address";
    private const string DefaultAgentName = "klacks-default";

    private static readonly HashSet<string> UiActionSkills = new(StringComparer.OrdinalIgnoreCase)
    {
        NavigateToSkillName,
        UpdateGeneralSettingsName,
        UpdateOwnerAddressName
    };

    public AgentSkillSeedService(
        ISkillRegistry skillRegistry,
        IAgentRepository agentRepository,
        IAgentSkillRepository agentSkillRepository,
        ILogger<AgentSkillSeedService> logger)
    {
        _skillRegistry = skillRegistry;
        _agentRepository = agentRepository;
        _agentSkillRepository = agentSkillRepository;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var agent = await EnsureDefaultAgentAsync(cancellationToken);
        var descriptors = _skillRegistry.GetAllSkills();

        if (descriptors.Count == 0)
        {
            _logger.LogWarning("No skills registered in SkillRegistry. Skipping seed.");
            return;
        }

        var existingSkills = await _agentSkillRepository.GetAllByAgentIdAsync(agent.Id, cancellationToken);
        var existingByName = existingSkills.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

        var inserted = 0;
        var updated = 0;

        foreach (var descriptor in descriptors)
        {
            if (existingByName.TryGetValue(descriptor.Name, out var existing))
            {
                UpdateExistingSkill(existing, descriptor);
                await _agentSkillRepository.UpdateAsync(existing, cancellationToken);
                updated++;
            }
            else
            {
                var newSkill = CreateNewSkill(agent.Id, descriptor);
                await _agentSkillRepository.AddAsync(newSkill, cancellationToken);
                inserted++;
            }
        }

        _logger.LogInformation(
            "Seeded {Total} skills for default agent (inserted: {Inserted}, updated: {Updated})",
            descriptors.Count, inserted, updated);
    }

    private async Task<Agent> EnsureDefaultAgentAsync(CancellationToken cancellationToken)
    {
        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent != null)
            return agent;

        agent = new Agent
        {
            Name = DefaultAgentName,
            DisplayName = "Klacks Assistant",
            Description = "Default AI assistant",
            IsActive = true,
            IsDefault = true
        };

        await _agentRepository.AddAsync(agent, cancellationToken);
        _logger.LogInformation("Created default agent: {AgentName} ({AgentId})", agent.Name, agent.Id);
        return agent;
    }

    private AgentSkill CreateNewSkill(Guid agentId, SkillDescriptor descriptor)
    {
        var skill = new AgentSkill
        {
            AgentId = agentId,
            Name = descriptor.Name,
            Description = descriptor.Description,
            ParametersJson = SerializeParameters(descriptor.Parameters),
            RequiredPermission = descriptor.RequiredPermissions.FirstOrDefault(),
            ExecutionType = MapExecutionType(descriptor),
            Category = descriptor.Category.ToString().ToLowerInvariant()
        };

        var handlerConfig = GetDefaultHandlerConfig(descriptor.Name);
        if (handlerConfig != null)
        {
            skill.HandlerConfig = handlerConfig;
        }

        return skill;
    }

    private void UpdateExistingSkill(AgentSkill existing, SkillDescriptor descriptor)
    {
        existing.Description = descriptor.Description;
        existing.ParametersJson = SerializeParameters(descriptor.Parameters);
        existing.RequiredPermission = descriptor.RequiredPermissions.FirstOrDefault();
        existing.Category = descriptor.Category.ToString().ToLowerInvariant();
        existing.ExecutionType = MapExecutionType(descriptor);

        if (existing.HandlerConfig == "{}" || string.IsNullOrEmpty(existing.HandlerConfig))
        {
            var handlerConfig = GetDefaultHandlerConfig(existing.Name);
            if (handlerConfig != null)
            {
                existing.HandlerConfig = handlerConfig;
            }
        }
    }

    private static string MapExecutionType(SkillDescriptor descriptor)
    {
        if (UiActionSkills.Contains(descriptor.Name))
            return LlmExecutionTypes.UiAction;

        if (descriptor.Category == SkillCategory.UI)
            return LlmExecutionTypes.FrontendOnly;

        return LlmExecutionTypes.Skill;
    }

    private static string? GetDefaultHandlerConfig(string skillName)
    {
        if (skillName.Equals(NavigateToSkillName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                steps = new object[]
                {
                    new { action = "navigate", routeMap = NavigateToRoutes, routeKeyFrom = "params.page", appendParamFrom = "params.entityId" }
                }
            });

        if (skillName.Equals(UpdateGeneralSettingsName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                steps = new object[]
                {
                    new { action = "navigate", route = "/workplace/settings" },
                    new { action = "click", selector = "open-settings" },
                    new { action = "waitForElement", selector = "setting-general-name", timeout = 5000 },
                    new { action = "setValue", selector = "setting-general-name", valueFrom = "params.appName" }
                }
            });

        if (skillName.Equals(UpdateOwnerAddressName, StringComparison.OrdinalIgnoreCase))
            return SerializeHandlerConfig(new
            {
                onError = "continue",
                steps = new object[]
                {
                    new { action = "navigate", route = "/workplace/settings" },
                    new { action = "click", selector = "open-settings" },
                    new { action = "waitForElement", selector = "setting-owner-address-name", timeout = 5000 },
                    new { action = "delay", delay = 1000 },
                    new { action = "conditional", condition = new { type = "paramExists", key = "addressName", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "setting-owner-address-name", valueFrom = "params.addressName" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "phone", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "setting-owner-address-tel", valueFrom = "params.phone" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "supplementAddress", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "setting-owner-address-supplement", valueFrom = "params.supplementAddress" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "email", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "setting-owner-address-email", valueFrom = "params.email" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "street", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "setting-owner-address-street", valueFrom = "params.street" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "zip", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "setting-owner-address-zip", valueFrom = "params.zip" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "city", @operator = "==" }, thenSteps = new object[] { new { action = "setValue", selector = "setting-owner-address-city", valueFrom = "params.city" } } },
                    new { action = "conditional", condition = new { type = "paramExists", key = "country", @operator = "==" }, thenSteps = new object[] { new { action = "setSelect", selector = "setting-owner-address-country", valueFrom = "params.country" } } },
                    new { action = "delay", delay = 500 },
                    new { action = "conditional", condition = new { type = "paramExists", key = "state", @operator = "==" }, thenSteps = new object[] { new { action = "setSelect", selector = "setting-owner-address-state", valueFrom = "params.state" } } }
                }
            });

        return null;
    }

    private static string SerializeParameters(IReadOnlyList<SkillParameter> parameters)
    {
        var paramDtos = parameters.Select(p => new
        {
            name = p.Name,
            type = SkillParameterTypeMapping.ToJsonSchemaType(p.Type),
            description = p.Description,
            required = p.Required,
            enumValues = p.EnumValues
        });

        return JsonSerializer.Serialize(paramDtos, JsonOptions);
    }

    private static string SerializeHandlerConfig(object config)
    {
        return JsonSerializer.Serialize(config, JsonOptions);
    }
}
