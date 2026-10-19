// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler that re-queues a Failed or Interrupted file of the erp_import_file ledger for import. First flips
/// stale Importing rows to Interrupted (so a file whose import died can be retried at once instead of after
/// the next run's reaper), then checks the row: unknown or deleted -> not found; any status other than
/// Failed/Interrupted -> invalid request. The stored object is re-hashed and compared with the SHA-256
/// recorded on receipt -- strict, per owner decision: a missing or changed file marks the row Failed with
/// StoredFileMissing / FileChangedAfterReceipt and is NOT re-queued (a corrected file must be uploaded anew).
/// Otherwise the row is reset to Received and its open exceptions are resolved in one transaction (the
/// conditional reset is the guard: a concurrent change of the row yields a conflict and resolves nothing),
/// and an import run is triggered. Returns the row's resulting status and failure reason.
/// </summary>
/// <param name="request">Contains the ledger row id of the file to retry</param>
using Klacks.Api.Application.Commands.Imports;
using Klacks.Api.Application.DTOs.ErpDropPoints;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Services.Imports;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Imports;

public class RetryErpOrderFileCommandHandler : IRequestHandler<RetryErpOrderFileCommand, ErpOrderFileRetryResultResource>
{
    public const string FileNotFoundError = "The ERP import file does not exist.";
    public const string NotRetryableError = "Only failed or interrupted ERP import files can be retried.";
    public const string ConcurrentChangeError = "The ERP import file was changed by another request. Reload the list and try again.";

    private readonly IErpImportFileRepository _fileRepository;
    private readonly IErpImportExceptionRepository _exceptionRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMediator _mediator;
    private readonly TimeProvider _timeProvider;

    public RetryErpOrderFileCommandHandler(
        IErpImportFileRepository fileRepository,
        IErpImportExceptionRepository exceptionRepository,
        IObjectStorageService objectStorageService,
        IUnitOfWork unitOfWork,
        IMediator mediator,
        TimeProvider timeProvider)
    {
        _fileRepository = fileRepository;
        _exceptionRepository = exceptionRepository;
        _objectStorageService = objectStorageService;
        _unitOfWork = unitOfWork;
        _mediator = mediator;
        _timeProvider = timeProvider;
    }

    public async Task<ErpOrderFileRetryResultResource> Handle(RetryErpOrderFileCommand request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        await _fileRepository.ReapStaleAsync(now - ErpImportFileTiming.StaleAfter, cancellationToken);

        var file = await _fileRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(FileNotFoundError);

        if (file.Status is not (ErpImportFileStatus.Failed or ErpImportFileStatus.Interrupted))
        {
            throw new InvalidRequestException(NotRetryableError);
        }

        var verificationFailure = await ErpImportStoredFileVerifier.VerifyAsync(_objectStorageService, file, cancellationToken);
        if (verificationFailure != null)
        {
            if (!await _fileRepository.TryMarkFailedAsync(file.Id, verificationFailure, now, cancellationToken))
            {
                throw new ConflictException(ConcurrentChangeError);
            }

            return new ErpOrderFileRetryResultResource { Id = file.Id, Status = ErpImportFileStatus.Failed, FailureReason = verificationFailure };
        }

        var reset = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (!await _fileRepository.TryResetForRetryAsync(file.Id, cancellationToken))
            {
                return false;
            }

            await _exceptionRepository.ResolveOpenForImportFileAsync(file.Id, now, cancellationToken);
            return true;
        });

        if (!reset)
        {
            throw new ConflictException(ConcurrentChangeError);
        }

        await _mediator.Send(new TriggerErpImportRunCommand(), cancellationToken);

        return new ErpOrderFileRetryResultResource { Id = file.Id, Status = ErpImportFileStatus.Received };
    }
}
