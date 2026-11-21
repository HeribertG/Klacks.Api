// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Queues a PostgreSQL engine upgrade by writing one Pending hand-off row of type PostgresUpgrade, the same row the support
/// script deploy/postgres/enqueue-engine-upgrade.sh writes: from_version/target_version as bare majors ("17", "19", never
/// the full engine version), artifact reference, hash and signature verbatim from the manifest, no migrations. The channel
/// is always Stable like in the script, even when the manifest was read from the Beta channel: the engine upgrade is not
/// channel specific and the updater does not read the column for it. The blocking rules live in IEngineUpgradePreflight;
/// an insert that loses the race against another active operation (duplicate on the single-active partial unique index)
/// is answered as OperationInProgress, every other persistence failure propagates. The out-of-process updater verifies the
/// signature and executes the row.
/// </summary>
/// <param name="repository">Self-committing store of the update_history rows</param>
/// <param name="preflight">Decides whether the upgrade may be queued and names the signed package</param>
/// <param name="timeProvider">Clock for requested_at</param>
using System.Globalization;
using Klacks.Api.Application.Commands.Update;
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Update;
using Klacks.Api.Domain.Models.Update;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Update;

public class TriggerEngineUpgradeCommandHandler : IRequestHandler<TriggerEngineUpgradeCommand, EngineUpgradeTriggerResult>
{
    private readonly IUpdateHistoryRepository _repository;
    private readonly IEngineUpgradePreflight _preflight;
    private readonly TimeProvider _timeProvider;

    public TriggerEngineUpgradeCommandHandler(
        IUpdateHistoryRepository repository, IEngineUpgradePreflight preflight, TimeProvider timeProvider)
    {
        _repository = repository;
        _preflight = preflight;
        _timeProvider = timeProvider;
    }

    public async Task<EngineUpgradeTriggerResult> Handle(
        TriggerEngineUpgradeCommand request, CancellationToken cancellationToken)
    {
        var preflight = await _preflight.CheckAsync(cancellationToken);
        if (preflight is not { BlockedReason: null, AvailableMajor: { } targetMajor, Artifact: { } artifact })
        {
            return Blocked(preflight.BlockedReason ?? EngineUpgradeBlockedReason.NoEngineInManifest);
        }

        var entry = new UpdateHistory
        {
            Id = Guid.NewGuid(),
            OperationType = UpdateOperationType.PostgresUpgrade,
            Status = UpdateOperationStatus.Pending,
            Channel = UpdateChannel.Stable,
            FromVersion = preflight.CurrentMajor.ToString(CultureInfo.InvariantCulture),
            TargetVersion = targetMajor.ToString(CultureInfo.InvariantCulture),
            ArtifactRef = artifact.ArtifactRef,
            ArtifactSha256 = artifact.Sha256,
            ArtifactSignature = artifact.Signature,
            ContainsMigrations = false,
            RequestedBy = request.RequestedBy,
            RequestedAt = _timeProvider.GetUtcNow().UtcDateTime,
        };

        try
        {
            await _repository.AddAsync(entry, cancellationToken);
        }
        catch (DatabaseUpdateException exception) when (exception.IsDuplicate)
        {
            return Blocked(EngineUpgradeBlockedReason.OperationInProgress);
        }

        return new EngineUpgradeTriggerResult { Enqueued = true, OperationId = entry.Id };
    }

    private static EngineUpgradeTriggerResult Blocked(EngineUpgradeBlockedReason reason) =>
        new() { Enqueued = false, BlockedReason = reason.ToString() };
}
