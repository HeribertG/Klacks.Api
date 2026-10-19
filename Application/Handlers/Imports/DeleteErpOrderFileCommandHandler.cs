// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler that discards a Failed or Interrupted file of the erp_import_file ledger. First flips stale
/// Importing rows to Interrupted (so a dead import can be discarded at once), then checks the row: unknown or
/// deleted -> not found; any status other than Failed/Interrupted -> invalid request. The row is soft-deleted
/// FIRST -- conditional on the status, together with the resolution of the file's open exceptions in one
/// transaction -- so a concurrent retry that re-queued the row in between makes the delete a conflict and the
/// re-queued file keeps its stored object. Only after the soft delete succeeded is the stored object deleted.
/// If that fails, it is logged and the row stays deleted: the inbox registration sees the soft-deleted row for
/// that key, never registers the object again and removes the leftover itself. The actor of the soft delete is
/// stamped by the repository from the request's audit actor. Nothing of the file is listed afterwards.
/// </summary>
/// <param name="request">Contains the ledger row id of the file</param>
using Klacks.Api.Application.Commands.Imports;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Imports;

public class DeleteErpOrderFileCommandHandler : IRequestHandler<DeleteErpOrderFileCommand>
{
    public const string FileNotFoundError = "The ERP import file does not exist.";
    public const string NotDeletableError = "Only failed or interrupted ERP import files can be deleted.";
    public const string ConcurrentChangeError = "The ERP import file was changed by another request. Reload the list and try again.";

    private readonly IErpImportFileRepository _fileRepository;
    private readonly IErpImportExceptionRepository _exceptionRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DeleteErpOrderFileCommandHandler> _logger;

    public DeleteErpOrderFileCommandHandler(
        IErpImportFileRepository fileRepository,
        IErpImportExceptionRepository exceptionRepository,
        IObjectStorageService objectStorageService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<DeleteErpOrderFileCommandHandler> logger)
    {
        _fileRepository = fileRepository;
        _exceptionRepository = exceptionRepository;
        _objectStorageService = objectStorageService;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<Unit> Handle(DeleteErpOrderFileCommand request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        await _fileRepository.ReapStaleAsync(now - ErpImportFileTiming.StaleAfter, cancellationToken);

        var file = await _fileRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(FileNotFoundError);

        if (file.Status is not (ErpImportFileStatus.Failed or ErpImportFileStatus.Interrupted))
        {
            throw new InvalidRequestException(NotDeletableError);
        }

        var deleted = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (!await _fileRepository.TrySoftDeleteAsync(file.Id, now, cancellationToken))
            {
                return false;
            }

            await _exceptionRepository.ResolveOpenForImportFileAsync(file.Id, now, cancellationToken);
            return true;
        });

        if (!deleted)
        {
            throw new ConflictException(ConcurrentChangeError);
        }

        await DeleteStoredObjectAsync(file.StorageKey);

        return Unit.Value;
    }

    private async Task DeleteStoredObjectAsync(string storageKey)
    {
        try
        {
            await _objectStorageService.DeleteAsync(storageKey, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "ERP import: the stored object {Key} of a deleted import file could not be deleted; the inbox registration removes the leftover",
                storageKey.ForLog());
        }
    }
}
