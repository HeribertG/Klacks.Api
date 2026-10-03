// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Sets how the schedule harmonizer (wizard stage 3) works: the deterministic local search (default, no AI)
/// or a language model, and which model that is. A model id is checked against the models the installation
/// actually has, so a wrong id cannot leave the harmonizer pointing at nothing.
/// </summary>
/// <param name="llmModelId">Optional id of the model the harmonizer should use in LLM mode.</param>
/// <param name="mode">Optional stage-3 mode: "deterministic" or "llm".</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;
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
    private const string LlmModelIdParameter = "llmModelId";
    private const string ModeParameter = "mode";
    private const string Subject = "Harmonizer settings";

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
        var llmModelId = GetParameter<string>(parameters, LlmModelIdParameter)?.Trim();
        var mode = GetParameter<string>(parameters, ModeParameter)?.Trim();
        if (string.IsNullOrWhiteSpace(llmModelId) && string.IsNullOrWhiteSpace(mode))
        {
            return SkillResult.Error($"Pass '{ModeParameter}' ('{HolisticHarmonizerModes.Deterministic}' or '{HolisticHarmonizerModes.Llm}'), '{LlmModelIdParameter}', or both.");
        }

        var pending = new List<PendingSetting>();
        if (!string.IsNullOrWhiteSpace(mode))
        {
            if (!HolisticHarmonizerModes.IsKnown(mode))
            {
                return SkillResult.Error(
                    $"Unknown mode '{mode}'. Use '{HolisticHarmonizerModes.Deterministic}' or '{HolisticHarmonizerModes.Llm}'.");
            }

            var normalized = HolisticHarmonizerModes.Parse(mode) == HolisticHarmonizerMode.Llm
                ? HolisticHarmonizerModes.Llm
                : HolisticHarmonizerModes.Deterministic;
            pending.Add(new PendingSetting(ModeParameter, Settings.HOLISTIC_HARMONIZER_MODE, normalized));
        }

        if (!string.IsNullOrWhiteSpace(llmModelId))
        {
            var models = await _llmRepository.GetModelsAsync();
            var match = models.FirstOrDefault(m =>
                string.Equals(m.ModelId, llmModelId, StringComparison.OrdinalIgnoreCase));

            if (match == null)
            {
                return SkillResult.Error(
                    $"Unknown model '{llmModelId}'. Available models: " +
                    string.Join(", ", models.Take(MaxListedModels).Select(m => m.ModelId)));
            }

            pending.Add(new PendingSetting(LlmModelIdParameter, Settings.HOLISTIC_HARMONIZER_LLM_MODEL, match.ModelId));
        }

        return await PersistAsync(pending, Subject);
    }
}
