// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Enqueues an update by writing a single Pending hand-off row, resolving and snapshotting the target
/// artifact from the current channel manifest at request time (avoids TOCTOU). Shared by the admin
/// trigger endpoint and the automatic update policy. Rejects when no update is available, an
/// intermediate version is required, or an operation is already active (single-active guard).
/// </summary>
using SettingsConstants = Klacks.Api.Application.Constants.Settings;
using Klacks.Api.Application.Commands.Update;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.Update;

public class TriggerUpdateCommandHandler : IRequestHandler<TriggerUpdateCommand, UpdateTriggerResult>
{
    private readonly IUpdateHistoryRepository _repository;
    private readonly IUpdateManifestReader _manifestReader;
    private readonly IUpdateAvailabilityEvaluator _evaluator;
    private readonly ISettingsReader _settingsReader;

    public TriggerUpdateCommandHandler(
        IUpdateHistoryRepository repository,
        IUpdateManifestReader manifestReader,
        IUpdateAvailabilityEvaluator evaluator,
        ISettingsReader settingsReader)
    {
        _repository = repository;
        _manifestReader = manifestReader;
        _evaluator = evaluator;
        _settingsReader = settingsReader;
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
            ArtifactRef = manifest.Artifacts.Docker?.ApiImage ?? manifest.Artifacts.OnPremWindows.FirstOrDefault()?.BundleUrl,
            ArtifactSha256 = manifest.Artifacts.Docker is not null
                ? manifest.Artifacts.Docker.Sha256
                : manifest.Artifacts.OnPremWindows.FirstOrDefault()?.Sha256,
            ArtifactSignature = manifest.Signature,
            ContainsMigrations = manifest.ContainsMigrations,
            RequestedBy = request.RequestedBy,
            RequestedAt = DateTime.UtcNow,
        };

        try
        {
            await _repository.AddAsync(entry, cancellationToken);
        }
        catch (DbUpdateException)
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

    private static UpdateTriggerResult Rejected(string reason) => new() { Enqueued = false, Reason = reason };
}
