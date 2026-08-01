// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Makes one configured language model the default via SetDefaultLLMModelCommand. The id is checked
/// against the models the installation has, so a wrong one is answered with the real choices instead
/// of leaving the assistant without a default.
/// </summary>
/// <param name="modelId">Id of the model that should become the default (required).</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("set_default_llm_model")]
public class SetDefaultLlmModelSkill : BaseSkillImplementation
{
    private const int MaxListedModels = 15;

    private readonly IMediator _mediator;
    private readonly ILLMRepository _llmRepository;

    public SetDefaultLlmModelSkill(IMediator mediator, ILLMRepository llmRepository)
    {
        _mediator = mediator;
        _llmRepository = llmRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var modelId = GetParameter<string>(parameters, "modelId")?.Trim();
        if (string.IsNullOrWhiteSpace(modelId))
        {
            return SkillResult.Error("Parameter 'modelId' is required.");
        }

        var models = await _llmRepository.GetModelsAsync();
        var match = models.FirstOrDefault(m =>
            string.Equals(m.ModelId, modelId, StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            return SkillResult.Error(
                $"Unknown model '{modelId}'. Available models: " +
                string.Join(", ", models.Take(MaxListedModels).Select(m => m.ModelId)));
        }

        var updated = await _mediator.Send(new SetDefaultLLMModelCommand(match.ModelId), cancellationToken);

        return SkillResult.SuccessResult(
            new { updated.ModelId, updated.ModelName, updated.IsDefault },
            $"'{updated.ModelName}' is now the default model.");
    }
}
