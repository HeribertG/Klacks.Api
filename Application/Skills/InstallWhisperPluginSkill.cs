// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Installs the self-hosted transcription plugin via InstallWhisperPluginCommand. The work runs in
/// the background, so the skill reports whether it was queued rather than claiming it is finished —
/// get_whisper_plugin_status shows how far it got. Without a modelAlias the engine is chosen from the installation
/// language: Parakeet when it covers that language, Whisper large-v3-turbo on a Docker installation whose language
/// Parakeet does not cover, Parakeet on a native installation (the only engine there) with a warning that dictation
/// in the installation language is not supported locally. The success message always lists the Parakeet coverage.
/// </summary>
/// <param name="modelAlias">Engine to install: small, large-v3-turbo (Whisper) or parakeet-v3 (Parakeet).</param>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Settings;
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
    private readonly IInstallationLanguageResolver _installationLanguageResolver;

    public InstallWhisperPluginSkill(
        IMediator mediator,
        IInstallationIdentityProvider installationIdentityProvider,
        IInstallationLanguageResolver installationLanguageResolver)
    {
        _mediator = mediator;
        _installationIdentityProvider = installationIdentityProvider;
        _installationLanguageResolver = installationLanguageResolver;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var installationLanguage = await _installationLanguageResolver.ResolveAsync(cancellationToken);
        var rawAlias = GetParameter<string>(parameters, "modelAlias");
        var requestedAlias = string.IsNullOrWhiteSpace(rawAlias) ? null : rawAlias.Trim().ToLowerInvariant();
        var modelAlias = requestedAlias
                         ?? WhisperPluginConstants.GetDefaultAlias(_installationIdentityProvider.Get().Kind, installationLanguage);

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
            BuildSuccessMessage(modelAlias, installationLanguage, requestedAlias is null));
    }

    private static string BuildSuccessMessage(string modelAlias, string installationLanguage, bool chosenByDefault)
    {
        var message =
            $"Installation of the '{modelAlias}' transcription model was queued and runs in the background. " +
            "Once get_whisper_plugin_status reports it as installed, call update_speech_settings with " +
            $"sttEngine \"{WhisperPluginConstants.LocalEngineValue}\" to use it. " +
            $"The Parakeet engine covers only these languages: {string.Join(", ", ParakeetLanguageCoverage.Languages)}.";

        var languageCovered = ParakeetLanguageCoverage.IsCovered(installationLanguage);
        if (modelAlias == WhisperPluginConstants.ModelAliasParakeet && !languageCovered)
        {
            message += $" Dictation in the installation language '{installationLanguage}' is not supported by the local " +
                       "Parakeet engine; recommend a cloud speech provider for that language.";
        }
        else if (chosenByDefault && modelAlias != WhisperPluginConstants.ModelAliasParakeet)
        {
            message += $" Whisper was chosen because the installation language '{installationLanguage}' is not covered by Parakeet.";
        }

        return message;
    }
}
