// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill to list all configured LLM providers with their status and configuration.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class ListLlmProvidersSkill : BaseSkill
{
    private readonly ILLMRepository _llmRepository;

    public override string Name => "list_llm_providers";

    public override string Description =>
        "Lists all configured LLM providers with their status, priority, and whether an API key is configured.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => [Permissions.CanViewSettings];

    public override IReadOnlyList<SkillParameter> Parameters =>
    [
        new SkillParameter(
            "searchTerm",
            "Filter providers by name",
            SkillParameterType.String,
            Required: false)
    ];

    public ListLlmProvidersSkill(ILLMRepository llmRepository)
    {
        _llmRepository = llmRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = GetParameter<string>(parameters, "searchTerm");

        var allProviders = await _llmRepository.GetProvidersAsync();

        var providers = allProviders
            .Where(p => !p.IsDeleted)
            .Where(p => string.IsNullOrEmpty(searchTerm) ||
                        p.ProviderName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.ProviderName)
            .Select(p => new
            {
                p.ProviderId,
                p.ProviderName,
                p.IsEnabled,
                p.HasApiKey,
                p.BaseUrl,
                p.Priority
            })
            .ToList();

        var resultData = new
        {
            Providers = providers,
            TotalCount = providers.Count
        };

        var message = $"Found {providers.Count} provider(s)" +
                      (!string.IsNullOrEmpty(searchTerm) ? $" matching '{searchTerm}'" : "") + ".";

        return SkillResult.SuccessResult(resultData, message);
    }
}
