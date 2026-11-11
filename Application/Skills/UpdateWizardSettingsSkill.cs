// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Sets the schedule harmonizer (wizard stage 3) to the deterministic local search. The language-model mode is
/// retired (owner decision 2026-11-11): any other mode, a missing mode or a harmonizer model is refused without writing.
/// </summary>
/// <param name="mode">Stage-3 mode; only "deterministic" is accepted.</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;
using Klacks.Api.Application.Skills.Base;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_wizard_settings")]
public class UpdateWizardSettingsSkill : SettingsWriterSkillBase
{
    private const string LlmModelIdParameter = "llmModelId";
    private const string ModeParameter = "mode";
    private const string Subject = "Harmonizer settings";
    private const string LlmModeRetiredMessage =
        "The harmonizer works only with the deterministic local search; the language-model mode has been retired, " +
        "so no model can be chosen for it. The only accepted call is mode 'deterministic'.";

    public UpdateWizardSettingsSkill(
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        ISettingsEncryptionService encryptionService)
        : base(settingsRepository, unitOfWork, encryptionService)
    {
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var llmModelId = GetParameter<string>(parameters, LlmModelIdParameter)?.Trim();
        var mode = GetParameter<string>(parameters, ModeParameter)?.Trim();

        if (!string.IsNullOrWhiteSpace(llmModelId)
            || !string.Equals(mode, HolisticHarmonizerModes.Deterministic, StringComparison.OrdinalIgnoreCase))
        {
            return SkillResult.Error(LlmModeRetiredMessage);
        }

        var pending = new List<PendingSetting>
        {
            new(ModeParameter, Settings.HOLISTIC_HARMONIZER_MODE, HolisticHarmonizerModes.Deterministic),
        };

        return await PersistAsync(pending, Subject);
    }
}
