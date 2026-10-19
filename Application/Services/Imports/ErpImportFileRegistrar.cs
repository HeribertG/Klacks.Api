// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Registers a stored ERP order file in the erp_import_file ledger and commits it -- normally as a Received
/// row (received at the given time, else now); the inbox registration also registers stored files without
/// their record as Failed (finish time now, reason clamped), which are never imported automatically.
/// Shared by the upload handlers and the inbox registration so every entry path writes the row the same
/// way: the earliest earlier row of the drop point with the same SHA-256 becomes duplicate_of_file_id
/// (an info hint only, it never blocks the import), and display name and uploader are clamped to their
/// column lengths. A unique storage-key violation means another caller registered the same object
/// concurrently: the staged row is detached (otherwise the next commit would insert it again) and null
/// is returned.
/// </summary>
/// <param name="repository">Ledger repository; Add is stage-only, the registrar commits</param>
/// <param name="unitOfWork">Commits the staged row</param>
/// <param name="timeProvider">Source of the received-at timestamp (UTC)</param>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Imports;

namespace Klacks.Api.Application.Services.Imports;

public class ErpImportFileRegistrar
{
    private readonly IErpImportFileRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ErpImportFileRegistrar> _logger;

    public ErpImportFileRegistrar(
        IErpImportFileRepository repository,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        ILogger<ErpImportFileRegistrar> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ErpImportFile?> RegisterAsync(ErpImportFileRegistration registration, CancellationToken cancellationToken = default)
    {
        if (registration.Status is not (ErpImportFileStatus.Received or ErpImportFileStatus.Failed))
        {
            throw new ArgumentOutOfRangeException(nameof(registration), registration.Status, "A file can only be registered as Received or Failed.");
        }

        var dropPoint = registration.DropPoint;
        var storedObject = registration.StoredObject;
        var duplicate = await _repository.FindDuplicateAsync(dropPoint.Id, storedObject.Sha256, excludeId: null, cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var isFailed = registration.Status == ErpImportFileStatus.Failed;

        var file = new ErpImportFile
        {
            Id = Guid.NewGuid(),
            DropPointId = dropPoint.Id,
            SourceSystemId = dropPoint.SourceSystemId,
            StorageKey = registration.StorageKey,
            FileName = Clamp(registration.FileName, ErpImportFileLimits.FileNameMaxLength),
            SizeBytes = storedObject.SizeBytes,
            Sha256 = storedObject.Sha256,
            Channel = registration.Channel,
            UploadedBy = string.IsNullOrWhiteSpace(registration.UploadedBy)
                ? null
                : Clamp(registration.UploadedBy.Trim(), ErpImportFileLimits.UploadedByMaxLength),
            Status = registration.Status,
            ReceivedAt = registration.ReceivedAt is { } receivedAt ? DateTime.SpecifyKind(receivedAt, DateTimeKind.Utc) : now,
            FinishedAt = isFailed ? now : null,
            FailureReason = isFailed && registration.FailureReason != null
                ? Clamp(registration.FailureReason, ErpImportFileLimits.FailureReasonMaxLength)
                : null,
            DuplicateOfFileId = duplicate?.Id,
        };

        _repository.Add(file);

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (DatabaseUpdateException ex) when (ex.IsDuplicate)
        {
            _repository.Detach(file);
            _logger.LogInformation(
                "ERP import: file {Key} was registered concurrently by another caller, skipping",
                registration.StorageKey.ForLog());
            return null;
        }

        return file;
    }

    private static string Clamp(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
