// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill for listing all configured messaging providers with their status.
/// Used by the chatbot to show which messaging services are available.
/// </summary>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Messaging;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.Messaging;

[SkillImplementation("list_messaging_providers")]
public class ListMessagingProvidersSkill : BaseSkillImplementation
{
    private readonly IMessagingProviderRepository _providerRepository;

    public ListMessagingProvidersSkill(IMessagingProviderRepository providerRepository)
    {
        _providerRepository = providerRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var providers = await _providerRepository.GetAllAsync();

        var result = providers.Select(p => new
        {
            p.Name,
            p.DisplayName,
            p.ProviderType,
            p.IsEnabled,
            Status = p.IsEnabled ? "active" : "disabled"
        }).ToList();

        var enabledCount = result.Count(p => p.IsEnabled);
        return SkillResult.SuccessResult(
            result,
            $"{result.Count} messaging provider(s) configured, {enabledCount} active.");
    }
}
