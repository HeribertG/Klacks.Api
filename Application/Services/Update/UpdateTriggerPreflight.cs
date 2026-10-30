// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides, without writing, whether an update could be enqueued now. Rejection order: no manifest, intermediate version
/// required, nothing newer (an invalid manifest counts as that), no signed artifact for this installation, an operation
/// of any type already active. The verdict is taken before Availability is narrowed to this installation, because the
/// narrowed status would read as "nothing newer".
/// </summary>
/// <param name="repository">Reads the active operation of the hand-off queue</param>
/// <param name="manifestReader">Fetches the channel manifest</param>
/// <param name="evaluator">Compares the running version against the manifest</param>
/// <param name="settingsReader">Reads the configured channel</param>
/// <param name="installationIdentityProvider">Tells Docker from native and names the runtime identifier</param>

using SettingsConstants = Klacks.Api.Application.Constants.Settings;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Domain.Services.Update;

namespace Klacks.Api.Application.Services.Update;

public class UpdateTriggerPreflight : IUpdateTriggerPreflight
{
    private readonly IUpdateHistoryRepository _repository;
    private readonly IUpdateManifestReader _manifestReader;
    private readonly IUpdateAvailabilityEvaluator _evaluator;
    private readonly ISettingsReader _settingsReader;
    private readonly IInstallationIdentityProvider _installationIdentityProvider;

    public UpdateTriggerPreflight(
        IUpdateHistoryRepository repository,
        IUpdateManifestReader manifestReader,
        IUpdateAvailabilityEvaluator evaluator,
        ISettingsReader settingsReader,
        IInstallationIdentityProvider installationIdentityProvider)
    {
        _repository = repository;
        _manifestReader = manifestReader;
        _evaluator = evaluator;
        _settingsReader = settingsReader;
        _installationIdentityProvider = installationIdentityProvider;
    }

    public static bool IsFailedAttempt(UpdateOperationStatus status) =>
        status is UpdateOperationStatus.Failed or UpdateOperationStatus.RolledBack or UpdateOperationStatus.RollbackFailed;

    public async Task<UpdateTriggerPreflightResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var current = new SemanticVersion(MyVersion.Major, MyVersion.Minor, MyVersion.Patch);
        var installation = _installationIdentityProvider.Get();

        var channelSetting = await _settingsReader.GetSetting(SettingsConstants.UPDATE_CHANNEL);
        var channel = Enum.TryParse<UpdateChannel>(channelSetting?.Value, ignoreCase: true, out var parsed)
            ? parsed
            : UpdateChannel.Stable;

        var active = await _repository.GetActiveOperationAsync(cancellationToken);
        var manifest = await _manifestReader.GetManifestAsync(channel, cancellationToken);
        if (manifest is null)
        {
            return new UpdateTriggerPreflightResult(
                current, channel, installation, null, null, null, active, UpdateReasons.NoManifest);
        }

        var availability = _evaluator.Evaluate(current, manifest);
        var artifact = UpdateArtifactSelector.Select(manifest, installation);
        var rejection = RejectionOf(availability, artifact, active);

        return new UpdateTriggerPreflightResult(
            current,
            channel,
            installation,
            manifest,
            UpdateArtifactSelector.RestrictToInstallation(availability, manifest, installation),
            artifact,
            active,
            rejection);
    }

    private static string? RejectionOf(
        UpdateAvailability availability, SelectedUpdateArtifact? artifact, UpdateHistory? active)
    {
        if (availability.Status == UpdateAvailabilityStatus.UpdateRequiresIntermediate)
        {
            return UpdateReasons.RequiresIntermediate;
        }

        if (!availability.IsUpdateAvailable)
        {
            return UpdateReasons.UpToDate;
        }

        if (artifact is null)
        {
            return UpdateReasons.NoArtifactForInstallation;
        }

        return active is null ? null : UpdateReasons.OperationInProgress;
    }
}
