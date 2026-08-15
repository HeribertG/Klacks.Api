// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sets which language model the schedule harmonizer runs on. The id is checked against the models
/// the installation actually has, so a wrong id cannot leave the harmonizer pointing at nothing.
/// </summary>
/// <param name="llmModelId">Id of the model the harmonizer should use.</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills.Base;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_wizard_settings")]
public class UpdateWizardSettingsSkill : SettingsWriterSkillBase
{
    private const int MaxListedModels = 15;

    private readonly ILLMRepository _llmRepository;

    public UpdateWizardSettingsSkill(
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        ILLMRepository llmRepository,
        ISettingsEncryptionService encryptionService)
        : base(settingsRepository, unitOfWork, encryptionService)
    {
        _llmRepository = llmRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var llmModelId = GetParameter<string>(parameters, "llmModelId")?.Trim();
        if (string.IsNullOrWhiteSpace(llmModelId))
        {
            return SkillResult.Error("Parameter 'llmModelId' is required.");
        }

        var models = await _llmRepository.GetModelsAsync();
        var match = models.FirstOrDefault(m =>
            string.Equals(m.ModelId, llmModelId, StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            return SkillResult.Error(
                $"Unknown model '{llmModelId}'. Available models: " +
                string.Join(", ", models.Take(MaxListedModels).Select(m => m.ModelId)));
        }

        var pending = new List<PendingSetting>
        {
            new("llmModelId", Settings.HOLISTIC_HARMONIZER_LLM_MODEL, match.ModelId)
        };

        return await PersistAsync(pending, "Harmonizer model setting");
    }
}
