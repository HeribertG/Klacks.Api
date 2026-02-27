// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Skills;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Assistant;

public class SkillRegistrationService
{
    private readonly ISkillRegistry _registry;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SkillRegistrationService> _logger;

    public SkillRegistrationService(
        ISkillRegistry registry,
        IServiceProvider serviceProvider,
        ILogger<SkillRegistrationService> logger)
    {
        _registry = registry;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public void RegisterAllSkills()
    {
        _logger.LogInformation("Starting skill registration...");

        RegisterSingletonSkills();
        RegisterScopedSkills();

        _logger.LogInformation("Skill registration completed. Total skills: {Count}", _registry.GetAllSkills().Count);
    }

    private void RegisterSingletonSkills()
    {
        var singletonSkills = new (Type type, Func<ISkill> factory)[]
        {
            (typeof(GetSystemInfoSkill), () => new GetSystemInfoSkill()),
            (typeof(GetCurrentTimeSkill), () => new GetCurrentTimeSkill()),
            (typeof(GetUserContextSkill), () => new GetUserContextSkill()),
            (typeof(ValidateCalendarRuleSkill), () => new ValidateCalendarRuleSkill())
        };

        foreach (var (type, factory) in singletonSkills)
        {
            try
            {
                var skill = factory();
                _registry.Register(CreateDescriptor(skill, type));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register singleton skill: {TypeName}", type.Name);
            }
        }
    }

    private void RegisterScopedSkills()
    {
        var scopedSkillTypes = new[]
        {
            typeof(NavigateToSkill),
            typeof(CreateEmployeeSkill),
            typeof(SearchEmployeesSkill),
            typeof(CreateContractSkill),
            typeof(SearchAndNavigateSkill),
            typeof(GetClientDetailsSkill),
            typeof(AddClientToGroupSkill),
            typeof(AssignContractToClientSkill),
            typeof(ListContractsSkill),
            typeof(ListGroupsSkill),
            typeof(ValidateAddressSkill),
            typeof(GetUserPermissionsSkill),
            typeof(GetGeneralSettingsSkill),
            typeof(UpdateGeneralSettingsSkill),
            typeof(GetOwnerAddressSkill),
            typeof(UpdateOwnerAddressSkill),
            typeof(GetAiSoulSkill),
            typeof(UpdateAiSoulSkill),
            typeof(AddAiMemorySkill),
            typeof(GetAiMemoriesSkill),
            typeof(UpdateAiMemorySkill),
            typeof(DeleteAiMemorySkill),
            typeof(SetUserGroupScopeSkill),
            typeof(ConfigureHeartbeatSkill),
            typeof(GetAiGuidelinesSkill),
            typeof(UpdateAiGuidelinesSkill),
            typeof(GetPageControlsSkill),
            typeof(GetEmailSettingsSkill),
            typeof(UpdateEmailSettingsSkill),
            typeof(GetImapSettingsSkill),
            typeof(UpdateImapSettingsSkill),
            typeof(WebSearchSkill),
            typeof(TestSmtpConnectionSkill),
            typeof(TestImapConnectionSkill)
        };

        using var scope = _serviceProvider.CreateScope();

        foreach (var type in scopedSkillTypes)
        {
            try
            {
                var skill = (ISkill)scope.ServiceProvider.GetRequiredService(type);
                _registry.Register(CreateDescriptor(skill, type));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register scoped skill: {TypeName}", type.Name);
            }
        }
    }

    private static SkillDescriptor CreateDescriptor(ISkill skill, Type implementationType)
    {
        return new SkillDescriptor(
            skill.Name,
            skill.Description,
            skill.Category,
            skill.Parameters,
            skill.RequiredPermissions,
            skill.RequiredCapabilities,
            implementationType);
    }
}
