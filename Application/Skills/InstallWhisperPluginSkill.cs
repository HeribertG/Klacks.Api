// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Installs the self-hosted transcription plugin via InstallWhisperPluginCommand. The work runs in
/// the background, so the skill reports whether it was queued rather than claiming it is finished —
/// get_whisper_plugin_status shows how far it got. Without a modelAlias the preferred engine of this
/// installation is installed (Parakeet).
/// </summary>
/// <param name="modelAlias">Engine to install: small, large-v3-turbo (Whisper) or parakeet-v3 (Parakeet).</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("install_whisper_plugin")]
public class InstallWhisperPluginSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;
    private readonly IInstallationIdentityProvider _installationIdentityProvider;

    public InstallWhisperPluginSkill(IMediator mediator, IInstallationIdentityProvider installationIdentityProvider)
    {
        _mediator = mediator;
        _installationIdentityProvider = installationIdentityProvider;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var modelAlias = GetParameter<string>(parameters, "modelAlias")?.Trim()
                         ?? WhisperPluginConstants.GetDefaultAlias(_installationIdentityProvider.Get().Kind);

        if (WhisperPluginConstants.ResolveModelId(modelAlias) == null)
        {
            return SkillResult.Error(
                $"Unknown modelAlias '{modelAlias}'. Use one of: " +
                $"{WhisperPluginConstants.ModelAliasSmall}, {WhisperPluginConstants.ModelAliasLarge}, " +
                $"{WhisperPluginConstants.ModelAliasParakeet}.");
        }

        var result = await _mediator.Send(
            new InstallWhisperPluginCommand(modelAlias, context.UserId.ToString()), cancellationToken);

        if (!result.Enqueued)
        {
            return SkillResult.Error($"Installation was not started: {result.Reason}");
        }

        return SkillResult.SuccessResult(
            new { result.Enqueued, result.OperationId, ModelAlias = modelAlias },
            $"Installation of the '{modelAlias}' transcription model was queued and runs in the background. " +
            "Once get_whisper_plugin_status reports it as installed, call update_speech_settings with " +
            $"sttEngine \"{WhisperPluginConstants.LocalEngineValue}\" to use it.");
    }
}
