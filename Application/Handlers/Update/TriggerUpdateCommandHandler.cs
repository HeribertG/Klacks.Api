// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Enqueues an update by writing a single Pending hand-off row, resolving and snapshotting the target
/// artifact from the current channel manifest at request time (avoids TOCTOU). Shared by the admin
/// trigger endpoint and the automatic update policy. Rejects when no update is available, an
/// intermediate version is required, the release has no signed artifact for this installation (Docker vs. native
/// runtime identifier, see UpdateArtifactSelector), or an operation is already active (single-active guard). The
/// automatic policy additionally skips a version whose last update attempt failed or was rolled back, so a broken
/// release is not retried (backup, migration, restore) on every detection cycle; a manual trigger still retries.
/// </summary>
using SettingsConstants = Klacks.Api.Application.Constants.Settings;
using Klacks.Api.Application.Commands.Update;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Domain.Services.Update;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Update;

public class TriggerUpdateCommandHandler : IRequestHandler<TriggerUpdateCommand, UpdateTriggerResult>
{
    private readonly IUpdateHistoryRepository _repository;
    private readonly IUpdateManifestReader _manifestReader;
    private readonly IUpdateAvailabilityEvaluator _evaluator;
    private readonly ISettingsReader _settingsReader;
    private readonly IInstallationIdentityProvider _installationIdentityProvider;

    public TriggerUpdateCommandHandler(
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

    public async Task<UpdateTriggerResult> Handle(TriggerUpdateCommand request, CancellationToken cancellationToken)
    {
        var current = new SemanticVersion(MyVersion.Major, MyVersion.Minor, MyVersion.Patch);

        var channelSetting = await _settingsReader.GetSetting(SettingsConstants.UPDATE_CHANNEL);
        var channel = Enum.TryParse<UpdateChannel>(channelSetting?.Value, ignoreCase: true, out var c) ? c : UpdateChannel.Stable;

        var manifest = await _manifestReader.GetManifestAsync(channel, cancellationToken);
        if (manifest is null)
        {
            return Rejected(UpdateReasons.NoManifest);
        }

        var availability = _evaluator.Evaluate(current, manifest);
        if (availability.Status == UpdateAvailabilityStatus.UpdateRequiresIntermediate)
        {
            return Rejected(UpdateReasons.RequiresIntermediate);
        }

        if (!availability.IsUpdateAvailable)
        {
            return Rejected(UpdateReasons.UpToDate);
        }

        var artifact = UpdateArtifactSelector.Select(manifest, _installationIdentityProvider.Get());
        if (artifact is null)
        {
            return Rejected(UpdateReasons.NoArtifactForInstallation);
        }

        if (request.RequestedBy == UpdateRequesters.Automatic
            && await LastAttemptAtVersionFailedAsync(manifest.LatestVersion, cancellationToken))
        {
            return Rejected(UpdateReasons.PreviousAttemptFailed);
        }

        if (await _repository.GetActiveOperationAsync(cancellationToken) is not null)
        {
            return Rejected(UpdateReasons.OperationInProgress);
        }

        var entry = new UpdateHistory
        {
            Id = Guid.NewGuid(),
            OperationType = UpdateOperationType.Update,
            Status = UpdateOperationStatus.Pending,
            Channel = channel,
            FromVersion = current.ToString(),
            TargetVersion = manifest.LatestVersion,
            ArtifactRef = artifact.ArtifactRef,
            ArtifactSha256 = artifact.Sha256,
            ArtifactSignature = artifact.Signature,
            ContainsMigrations = manifest.ContainsMigrations,
            RequestedBy = request.RequestedBy,
            RequestedAt = DateTime.UtcNow,
        };

        try
        {
            await _repository.AddAsync(entry, cancellationToken);
        }
        catch (Exception exception) when (exception is DatabaseUpdateException or ConcurrencyException)
        {
            return Rejected(UpdateReasons.OperationInProgress);
        }

        return new UpdateTriggerResult
        {
            Enqueued = true,
            OperationId = entry.Id,
            Reason = UpdateReasons.UpdateEnqueued,
        };
    }

    private async Task<bool> LastAttemptAtVersionFailedAsync(string version, CancellationToken cancellationToken)
    {
        var last = await _repository.GetLatestByTypesAsync([UpdateOperationType.Update], cancellationToken);
        return last is not null
            && string.Equals(last.TargetVersion, version, StringComparison.Ordinal)
            && last.Status is UpdateOperationStatus.Failed or UpdateOperationStatus.RolledBack or UpdateOperationStatus.RollbackFailed;
    }

    private static UpdateTriggerResult Rejected(string reason) => new() { Enqueued = false, Reason = reason };
}
