// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Imports one Received row of the erp_import_file ledger. Claims it (compare-and-swap on status and
/// attempt; a lost claim skips the file), reads the stored object exactly once
/// (ErpImportStoredFileVerifier.ReadVerifiedAsync: bounded buffer, SHA-256 over the buffered bytes compared with
/// the hash recorded on receipt), parses those same bytes, and imports every order through
/// ImportedOrderProcessor. Outcomes are counted per order (Created, Updated, Unchanged, Superseded,
/// Rejected); parser rejections are counted per rejected order element (its order index), not per order
/// reference, so orders without or with a repeated reference are not merged. Every rejected order -- refused
/// by the parser or failed while importing -- becomes one
/// erp_import_exception linked through ImportFileId (FileKey keeps the storage key) and raises
/// OrderImportFailedTriggerEvent. Progress is written once right after parsing (so the total is visible
/// at once) and then by the heartbeat cadence of ErpImportFileAttempt.
/// End states: Completed (no rejects), CompletedWithWarnings (at least one reject, also when every order
/// was rejected but the file itself was parseable), Failed (stored file missing, changed after receipt, larger
/// than ErpImportFileLimits.MaxFileSizeBytes, or nothing parseable -- then the reason is the joined parser messages, also recorded as one exception
/// without order reference so admins are alerted). A heartbeat or finish returning false means the row
/// was reaped or reset elsewhere: the file is abandoned without a finish. Cancellation stops at once and
/// leaves the row to age into Interrupted. Any other unexpected error finishes the attempt as Interrupted
/// with the error message, so the next file still runs and the row can be retried.
/// The raw file is never moved: it stays at its storage key for audit and retry.
/// All ledger transitions are self-committing ExecuteUpdate calls issued outside the per-order
/// transactions; exceptions are staged and committed right away, before the next ledger call.
/// A failed order (or an aborted file) leaves its rolled-back entities tracked in the shared scoped
/// context; they are discarded (IUnitOfWork.DiscardTrackedChanges) before anything else is committed, so
/// the exception commit and every later order and file of the run do not re-save them and fail as well.
/// </summary>
/// <param name="fileRepository">Ledger repository (claim/heartbeat/finish)</param>
/// <param name="objectStorageService">Downloads the stored file (once per attempt)</param>
/// <param name="parser">Parses the order XML</param>
/// <param name="orderProcessor">Imports one parsed order and reports its outcome</param>
/// <param name="exceptionRepository">Stages one erp_import_exception per rejected order</param>
/// <param name="triggerService">Raises the admin alert per recorded exception</param>
/// <param name="unitOfWork">Commits each recorded exception and discards what a failed order left tracked</param>
/// <param name="instanceIdentity">Instance id the claim, heartbeats and finish are written with</param>
/// <param name="timeProvider">Clock for claim, heartbeat and finish timestamps</param>
using Klacks.Api.Application.DTOs.Imports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Imports;
using Klacks.Api.Application.Services.Assistant.Triggers;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Imports;

namespace Klacks.Api.Application.Services.Imports;

public class ErpImportFileImporter
{
    private const string ReasonSeparator = "; ";

    private readonly IErpImportFileRepository _fileRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IOrderImportParser _parser;
    private readonly ImportedOrderProcessor _orderProcessor;
    private readonly IErpImportExceptionRepository _exceptionRepository;
    private readonly IAgentTriggerService _triggerService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ErpImportInstanceIdentity _instanceIdentity;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ErpImportFileImporter> _logger;

    public ErpImportFileImporter(
        IErpImportFileRepository fileRepository,
        IObjectStorageService objectStorageService,
        IOrderImportParser parser,
        ImportedOrderProcessor orderProcessor,
        IErpImportExceptionRepository exceptionRepository,
        IAgentTriggerService triggerService,
        IUnitOfWork unitOfWork,
        ErpImportInstanceIdentity instanceIdentity,
        TimeProvider timeProvider,
        ILogger<ErpImportFileImporter> logger)
    {
        _fileRepository = fileRepository;
        _objectStorageService = objectStorageService;
        _parser = parser;
        _orderProcessor = orderProcessor;
        _exceptionRepository = exceptionRepository;
        _triggerService = triggerService;
        _unitOfWork = unitOfWork;
        _instanceIdentity = instanceIdentity;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task ImportAsync(ErpImportFile file, CancellationToken cancellationToken = default)
    {
        var claimedAt = _timeProvider.GetUtcNow().UtcDateTime;
        if (!await _fileRepository.TryClaimAsync(file.Id, file.Attempt, _instanceIdentity.InstanceId, claimedAt, cancellationToken))
        {
            _logger.LogInformation("ERP import: file {Key} is already being imported elsewhere, skipping", file.StorageKey.ForLog());
            return;
        }

        var attempt = new ErpImportFileAttempt(
            _fileRepository, _timeProvider, file.Id, _instanceIdentity.InstanceId, file.Attempt + 1, claimedAt);

        try
        {
            await ImportClaimedAsync(file, attempt, cancellationToken);
        }
        catch (Exception ex) when (!IsCancellation(ex, cancellationToken))
        {
            _logger.LogError(ex, "ERP import: file {Key} aborted by an unexpected error", file.StorageKey.ForLog());
            _unitOfWork.DiscardTrackedChanges();
            await TryFinishInterruptedAsync(file, attempt, ex, cancellationToken);
        }
    }

    private async Task ImportClaimedAsync(ErpImportFile file, ErpImportFileAttempt attempt, CancellationToken cancellationToken)
    {
        var storedFile = await ErpImportStoredFileVerifier.ReadVerifiedAsync(_objectStorageService, file, cancellationToken);
        if (storedFile.Content == null)
        {
            await FailAsync(file, attempt, storedFile.FailureReason!, cancellationToken);
            return;
        }

        OrderImportParseResult result;
        using (var stream = new MemoryStream(storedFile.Content, writable: false))
        {
            result = _parser.Parse(stream);
        }

        if (IsUnparseable(result))
        {
            await FailAsync(file, attempt, string.Join(ReasonSeparator, result.Errors.Select(e => e.Message)), cancellationToken);
            return;
        }

        var rejectedByParser = result.Errors
            .GroupBy(e => (e.OrderIndex, Reference: e.OrderIndex.HasValue ? null : e.ExternalOrderReference))
            .ToList();
        var tally = new ErpImportFileTally(result.Orders.Count + rejectedByParser.Count);

        if (!await attempt.HeartbeatAsync(tally.ToProgress(), cancellationToken))
        {
            LogOwnershipLost(file);
            return;
        }

        foreach (var rejectedOrder in rejectedByParser)
        {
            var reference = rejectedOrder.First().ExternalOrderReference;
            var reason = string.Join(ReasonSeparator, rejectedOrder.Select(e => e.Message));
            _logger.LogWarning(
                "ERP import: order {Reference} in {Key} rejected: {Errors}",
                reference.ForLog(), file.StorageKey.ForLog(), reason.ForLog());
            await RecordExceptionAsync(file, reference, reason, cancellationToken);
            tally.Record(ImportedOrderOutcome.Rejected);

            if (!await attempt.HeartbeatIfDueAsync(tally.ToProgress(), cancellationToken))
            {
                LogOwnershipLost(file);
                return;
            }
        }

        foreach (var order in result.Orders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            tally.Record(await ProcessOrderAsync(file, order, cancellationToken));

            if (!await attempt.HeartbeatIfDueAsync(tally.ToProgress(), cancellationToken))
            {
                LogOwnershipLost(file);
                return;
            }
        }

        var status = tally.RejectedCount == 0 ? ErpImportFileStatus.Completed : ErpImportFileStatus.CompletedWithWarnings;
        if (!await attempt.FinishAsync(status, tally.ToProgress(), failureReason: null, cancellationToken))
        {
            LogOwnershipLost(file);
        }
    }

    /// <summary>
    /// Nothing parseable: no order survived and every error is a file-level one (no order index, no order
    /// reference) -- a root-level failure such as malformed XML or a wrong schema version. Orders rejected because
    /// their own reference is missing still carry their order index, so a file consisting only of such orders is
    /// parseable (CompletedWithWarnings, every order rejected). A valid file without any order is not unparseable
    /// (Completed, total 0).
    /// </summary>
    private static bool IsUnparseable(OrderImportParseResult result)
    {
        return result.Orders.Count == 0
            && result.Errors.Count > 0
            && result.Errors.All(e => e.OrderIndex == null && e.ExternalOrderReference == null);
    }

    private async Task<ImportedOrderOutcome> ProcessOrderAsync(ErpImportFile file, ImportedOrderPayload order, CancellationToken cancellationToken)
    {
        try
        {
            return await _orderProcessor.ProcessOrderAsync(order, cancellationToken);
        }
        catch (Exception ex) when (!IsCancellation(ex, cancellationToken))
        {
            _logger.LogError(
                ex, "ERP import: order {Reference} in {Key} failed",
                order.ExternalOrderReference.ForLog(), file.StorageKey.ForLog());
            _unitOfWork.DiscardTrackedChanges();
            await RecordExceptionAsync(file, order.ExternalOrderReference, RejectionReasonFor(ex), cancellationToken);
            return ImportedOrderOutcome.Rejected;
        }
    }

    private static string RejectionReasonFor(Exception exception)
    {
        return exception is DatabaseUpdateException { IsDuplicate: true, ConstraintName: ShiftIndexNames.OpenDraftReferenceUnique }
            ? ErpImportFileFailureReasons.ConcurrentOpenDraft
            : exception.Message;
    }

    private async Task FailAsync(ErpImportFile file, ErpImportFileAttempt attempt, string reason, CancellationToken cancellationToken)
    {
        _logger.LogWarning("ERP import: file {Key} failed: {Reason}", file.StorageKey.ForLog(), reason.ForLog());
        await RecordExceptionAsync(file, externalOrderReference: null, reason, cancellationToken);

        if (!await attempt.FinishAsync(ErpImportFileStatus.Failed, ErpImportFileProgress.Empty, reason, cancellationToken))
        {
            LogOwnershipLost(file);
        }
    }

    private async Task TryFinishInterruptedAsync(ErpImportFile file, ErpImportFileAttempt attempt, Exception error, CancellationToken cancellationToken)
    {
        try
        {
            var reason = ErpImportFileFailureReasons.ImportAbortedPrefix + error.Message;
            if (!await attempt.FinishAsync(ErpImportFileStatus.Interrupted, attempt.LastProgress, reason, cancellationToken))
            {
                LogOwnershipLost(file);
            }
        }
        catch (Exception ex) when (!IsCancellation(ex, cancellationToken))
        {
            _logger.LogError(
                ex, "ERP import: file {Key} could not be marked as interrupted; the stale-heartbeat reaper will do it",
                file.StorageKey.ForLog());
        }
    }

    private async Task RecordExceptionAsync(ErpImportFile file, string? externalOrderReference, string reason, CancellationToken cancellationToken)
    {
        var exception = new ErpImportException
        {
            Id = Guid.NewGuid(),
            SourceSystemId = file.SourceSystemId,
            FileKey = file.StorageKey,
            ImportFileId = file.Id,
            ExternalOrderReference = externalOrderReference,
            Reason = reason
        };

        await _exceptionRepository.Add(exception);
        await _unitOfWork.CompleteAsync();

        await _triggerService.OnEventAsync(new OrderImportFailedTriggerEvent(exception.Id, file.StorageKey, reason), cancellationToken);
    }

    private void LogOwnershipLost(ErpImportFile file)
    {
        _logger.LogWarning(
            "ERP import: file {Key} was reaped or reset elsewhere while importing; abandoning this attempt",
            file.StorageKey.ForLog());
    }

    private static bool IsCancellation(Exception exception, CancellationToken cancellationToken)
    {
        return exception is OperationCanceledException && cancellationToken.IsCancellationRequested;
    }
}
