// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Enqueues an update by writing a single Pending hand-off row, resolving and snapshotting the target
/// artifact from the current channel manifest at request time (avoids TOCTOU). Shared by the admin
/// trigger endpoint, the automatic update policy and the confirmed chat skill. The rejections every path shares
/// live in IUpdateTriggerPreflight: no update available, an intermediate version required, no signed artifact for
/// this installation (Docker vs. native runtime identifier, see UpdateArtifactSelector) or an operation already
/// active (single-active guard). A caller that names ExpectedTargetVersion is rejected when the manifest moved on to
/// another version. The automatic policy additionally skips a version whose last update attempt failed or was rolled
/// back, so a broken release is not retried (backup, migration, restore) on every detection cycle; a manual trigger
/// still retries.
/// </summary>
using Klacks.Api.Application.Commands.Update;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Update;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Update;

public class TriggerUpdateCommandHandler : IRequestHandler<TriggerUpdateCommand, UpdateTriggerResult>
{
    private readonly IUpdateHistoryRepository _repository;
    private readonly IUpdateTriggerPreflight _preflight;

    public TriggerUpdateCommandHandler(IUpdateHistoryRepository repository, IUpdateTriggerPreflight preflight)
    {
        _repository = repository;
        _preflight = preflight;
    }

    public async Task<UpdateTriggerResult> Handle(TriggerUpdateCommand request, CancellationToken cancellationToken)
    {
        var preflight = await _preflight.CheckAsync(cancellationToken);
        if (preflight is not { RejectionReason: null, Manifest: { } manifest, Artifact: { } artifact })
        {
            return Rejected(preflight.RejectionReason ?? UpdateReasons.NoManifest);
        }

        if (request.ExpectedTargetVersion is not null
            && !string.Equals(request.ExpectedTargetVersion, manifest.LatestVersion, StringComparison.Ordinal))
        {
            return Rejected(UpdateReasons.TargetVersionChanged);
        }

        if (request.RequestedBy == UpdateRequesters.Automatic
            && await LastAttemptAtVersionFailedAsync(manifest.LatestVersion, cancellationToken))
        {
            return Rejected(UpdateReasons.PreviousAttemptFailed);
        }

        var entry = new UpdateHistory
        {
            Id = Guid.NewGuid(),
            OperationType = UpdateOperationType.Update,
            Status = UpdateOperationStatus.Pending,
            Channel = preflight.Channel,
            FromVersion = preflight.CurrentVersion.ToString(),
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
            && UpdateTriggerPreflight.IsFailedAttempt(last.Status);
    }

    private static UpdateTriggerResult Rejected(string reason) => new() { Enqueued = false, Reason = reason };
}
