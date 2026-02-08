using Klacks.Api.Application.Skills;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Services.Skills.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Skills;

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
        var singletonSkillTypes = new (Type type, Func<IServiceProvider, ISkill> factory)[]
        {
            (typeof(GetSystemInfoSkill), _ => new GetSystemInfoSkill()),
            (typeof(GetCurrentTimeSkill), _ => new GetCurrentTimeSkill()),
            (typeof(GetUserContextSkill), _ => new GetUserContextSkill()),
            (typeof(NavigateToSkill), _ => new NavigateToSkill())
        };

        foreach (var (type, factory) in singletonSkillTypes)
        {
            try
            {
                var skill = factory(_serviceProvider);
                _registry.Register(skill);
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
            typeof(DeleteAiMemorySkill)
        };

        using var scope = _serviceProvider.CreateScope();

        foreach (var type in scopedSkillTypes)
        {
            try
            {
                var skill = (ISkill)scope.ServiceProvider.GetRequiredService(type);
                _registry.Register(skill);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register scoped skill: {TypeName}", type.Name);
            }
        }
    }
}
