// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One claimed attempt of an ERP import file: reports progress and the final status through the ledger's
/// heartbeat and finish transitions, always under the claiming instance id and attempt number. Decides the
/// heartbeat cadence: HeartbeatIfDueAsync writes after every ErpImportFileTiming.HeartbeatEveryOrders
/// orders or once ErpImportFileTiming.HeartbeatInterval has passed since the last write, whichever comes
/// first. A false result from either transition means the row was reaped, reset or claimed again -- the
/// caller must stop working on the file. LastProgress is the progress of the last successful write and
/// is what an abort reports.
/// </summary>
/// <param name="repository">Ledger repository with the self-committing heartbeat/finish transitions</param>
/// <param name="timeProvider">Clock for the heartbeat cadence and the transition timestamps</param>
/// <param name="fileId">Ledger row id of the claimed file</param>
/// <param name="instanceId">Instance id the row was claimed with</param>
/// <param name="attempt">Attempt number the claim produced</param>
/// <param name="claimedAt">Time of the claim, which also wrote the first heartbeat</param>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Models.Imports;

namespace Klacks.Api.Application.Services.Imports;

public sealed class ErpImportFileAttempt
{
    private readonly IErpImportFileRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly Guid _fileId;
    private readonly string _instanceId;
    private DateTime _lastBeatAt;
    private int _ordersSinceLastBeat;

    public ErpImportFileAttempt(
        IErpImportFileRepository repository,
        TimeProvider timeProvider,
        Guid fileId,
        string instanceId,
        int attempt,
        DateTime claimedAt)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _fileId = fileId;
        _instanceId = instanceId;
        Attempt = attempt;
        _lastBeatAt = claimedAt;
    }

    public int Attempt { get; }

    public ErpImportFileProgress LastProgress { get; private set; } = ErpImportFileProgress.Empty;

    public async Task<bool> HeartbeatAsync(ErpImportFileProgress progress, CancellationToken cancellationToken)
    {
        var now = UtcNow();
        if (!await _repository.HeartbeatAsync(_fileId, _instanceId, Attempt, progress, now, cancellationToken))
        {
            return false;
        }

        _lastBeatAt = now;
        _ordersSinceLastBeat = 0;
        LastProgress = progress;
        return true;
    }

    public async Task<bool> HeartbeatIfDueAsync(ErpImportFileProgress progress, CancellationToken cancellationToken)
    {
        _ordersSinceLastBeat++;

        var due = _ordersSinceLastBeat >= ErpImportFileTiming.HeartbeatEveryOrders
            || UtcNow() - _lastBeatAt >= ErpImportFileTiming.HeartbeatInterval;

        return !due || await HeartbeatAsync(progress, cancellationToken);
    }

    public async Task<bool> FinishAsync(ErpImportFileStatus terminalStatus, ErpImportFileProgress progress, string? failureReason, CancellationToken cancellationToken)
    {
        if (!await _repository.FinishAsync(_fileId, _instanceId, Attempt, terminalStatus, progress, failureReason, UtcNow(), cancellationToken))
        {
            return false;
        }

        LastProgress = progress;
        return true;
    }

    private DateTime UtcNow()
    {
        return _timeProvider.GetUtcNow().UtcDateTime;
    }
}
