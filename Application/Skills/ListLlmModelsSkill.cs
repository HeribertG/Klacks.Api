// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill to list all configured LLM models with their provider, status, costs, and capabilities.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class ListLlmModelsSkill : BaseSkill
{
    private readonly ILLMRepository _llmRepository;

    public override string Name => "list_llm_models";

    public override string Description =>
        "Lists all configured LLM models with their provider, status, costs, and capabilities.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => [Permissions.CanViewSettings];

    public override IReadOnlyList<SkillParameter> Parameters =>
    [
        new SkillParameter(
            "searchTerm",
            "Optional search term to filter models by name",
            SkillParameterType.String,
            Required: false),
        new SkillParameter(
            "providerId",
            "Filter by provider ID",
            SkillParameterType.String,
            Required: false)
    ];

    public ListLlmModelsSkill(ILLMRepository llmRepository)
    {
        _llmRepository = llmRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = GetParameter<string>(parameters, "searchTerm");
        var providerIdFilter = GetParameter<string>(parameters, "providerId");

        var allModels = await _llmRepository.GetModelsAsync();

        var models = allModels
            .Where(m => !m.IsDeleted)
            .Where(m => string.IsNullOrEmpty(searchTerm) ||
                        m.ModelName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        m.ModelId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            .Where(m => string.IsNullOrEmpty(providerIdFilter) ||
                        m.ProviderId.Equals(providerIdFilter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(m => m.ProviderId)
            .ThenBy(m => m.ModelName)
            .Select(m => new
            {
                m.ModelId,
                m.ModelName,
                m.ProviderId,
                m.ApiModelId,
                m.IsEnabled,
                m.IsDefault,
                m.ContextWindow,
                m.MaxTokens,
                m.CostPerInputToken,
                m.CostPerOutputToken
            })
            .ToList();

        var resultData = new
        {
            Models = models,
            TotalCount = models.Count
        };

        var message = $"Found {models.Count} model(s)" +
                      (!string.IsNullOrEmpty(searchTerm) ? $" matching '{searchTerm}'" : "") +
                      (!string.IsNullOrEmpty(providerIdFilter) ? $" for provider '{providerIdFilter}'" : "") + ".";

        return SkillResult.SuccessResult(resultData, message);
    }
}
