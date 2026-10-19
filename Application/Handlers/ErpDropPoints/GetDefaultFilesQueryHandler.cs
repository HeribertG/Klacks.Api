// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for the file listing of the default ERP drop point. Reads ONLY the erp_import_file ledger (never
/// the object storage): Inbox = Received rows plus Importing rows with a live heartbeat (with their progress),
/// Processed = the newest ErpImportFileTiming.ProcessedListingCap Completed/CompletedWithWarnings rows,
/// Failed = Failed and Interrupted rows. An Importing row whose last heartbeat is older than
/// ErpImportFileTiming.StaleAfter is reported as Interrupted (and listed under Failed) even before the
/// runner's reaper flipped it; the query itself never writes. Every row carries its open rejected orders
/// (open exceptions WITH an order reference; the order-less exception of a Failed file is its failure
/// reason, not a rejected order) and, when its content equals an earlier delivery, that delivery's receipt
/// time. Each list is ordered newest received first.
/// </summary>
/// <param name="request">Marker query without parameters</param>
using Klacks.Api.Application.DTOs.ErpDropPoints;
using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ErpDropPoints;

public class GetDefaultFilesQueryHandler : IRequestHandler<GetDefaultFilesQuery, ErpDropPointFilesResource>
{
    private readonly IErpDefaultDropPointProvider _defaultDropPointProvider;
    private readonly IErpImportFileRepository _fileRepository;
    private readonly IErpImportExceptionRepository _exceptionRepository;
    private readonly TimeProvider _timeProvider;

    public GetDefaultFilesQueryHandler(
        IErpDefaultDropPointProvider defaultDropPointProvider,
        IErpImportFileRepository fileRepository,
        IErpImportExceptionRepository exceptionRepository,
        TimeProvider timeProvider)
    {
        _defaultDropPointProvider = defaultDropPointProvider;
        _fileRepository = fileRepository;
        _exceptionRepository = exceptionRepository;
        _timeProvider = timeProvider;
    }

    public async Task<ErpDropPointFilesResource> Handle(GetDefaultFilesQuery request, CancellationToken cancellationToken)
    {
        var dropPoint = await _defaultDropPointProvider.GetOrCreateDefaultAsync(cancellationToken);
        var files = await _fileRepository.ListForDropPointAsync(dropPoint.Id, ErpImportFileTiming.ProcessedListingCap, cancellationToken);
        var staleCutoff = _timeProvider.GetUtcNow().UtcDateTime - ErpImportFileTiming.StaleAfter;

        var rejectedOrdersByFile = await LoadRejectedOrdersAsync(files, cancellationToken);
        var receivedAtById = await LoadDuplicateReceivedAtAsync(files, cancellationToken);

        var result = new ErpDropPointFilesResource();
        foreach (var file in files)
        {
            var resource = ToResource(file, staleCutoff, rejectedOrdersByFile, receivedAtById);
            TargetList(result, resource.Status).Add(resource);
        }

        return result;
    }

    private async Task<Dictionary<Guid, List<ErpImportRejectedOrderResource>>> LoadRejectedOrdersAsync(
        List<ErpImportFile> files,
        CancellationToken cancellationToken)
    {
        var exceptions = await _exceptionRepository.GetOpenRejectedOrdersAsync(files.Select(f => f.Id).ToList(), cancellationToken);

        return exceptions
            .GroupBy(e => e.ImportFileId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(e => new ErpImportRejectedOrderResource
                    {
                        ExternalOrderReference = e.ExternalOrderReference!,
                        Reason = e.Reason
                    })
                    .ToList());
    }

    private async Task<Dictionary<Guid, DateTime>> LoadDuplicateReceivedAtAsync(List<ErpImportFile> files, CancellationToken cancellationToken)
    {
        var receivedAtById = files.ToDictionary(f => f.Id, f => f.ReceivedAt);
        var missingIds = files
            .Where(f => f.DuplicateOfFileId is { } duplicateId && !receivedAtById.ContainsKey(duplicateId))
            .Select(f => f.DuplicateOfFileId!.Value)
            .Distinct()
            .ToList();

        foreach (var original in await _fileRepository.GetByIdsAsync(missingIds, cancellationToken))
        {
            receivedAtById[original.Id] = original.ReceivedAt;
        }

        return receivedAtById;
    }

    private static ErpImportFileResource ToResource(
        ErpImportFile file,
        DateTime staleCutoff,
        Dictionary<Guid, List<ErpImportRejectedOrderResource>> rejectedOrdersByFile,
        Dictionary<Guid, DateTime> receivedAtById)
    {
        var isStale = IsStaleImport(file, staleCutoff);

        return new ErpImportFileResource
        {
            Id = file.Id,
            FileName = file.FileName,
            SizeBytes = file.SizeBytes,
            Status = isStale ? ErpImportFileStatus.Interrupted : file.Status,
            Channel = file.Channel,
            UploadedBy = file.UploadedBy,
            ReceivedAt = file.ReceivedAt,
            StartedAt = file.StartedAt,
            FinishedAt = file.FinishedAt,
            ProcessedOrders = file.ProcessedOrders,
            TotalOrders = file.TotalOrders,
            NewCount = file.NewCount,
            UpdatedCount = file.UpdatedCount,
            UnchangedCount = file.UnchangedCount,
            SupersededCount = file.SupersededCount,
            RejectedCount = file.RejectedCount,
            FailureReason = isStale ? file.FailureReason ?? ErpImportFileFailureReasons.StaleHeartbeat : file.FailureReason,
            DuplicateOfReceivedAt = file.DuplicateOfFileId is { } duplicateId && receivedAtById.TryGetValue(duplicateId, out var receivedAt)
                ? receivedAt
                : null,
            RejectedOrders = rejectedOrdersByFile.GetValueOrDefault(file.Id) ?? []
        };
    }

    private static bool IsStaleImport(ErpImportFile file, DateTime staleCutoff)
    {
        return file.Status == ErpImportFileStatus.Importing && (file.HeartbeatAt ?? file.StartedAt) < staleCutoff;
    }

    private static List<ErpImportFileResource> TargetList(ErpDropPointFilesResource result, ErpImportFileStatus status)
    {
        return status switch
        {
            ErpImportFileStatus.Received or ErpImportFileStatus.Importing => result.Inbox,
            ErpImportFileStatus.Completed or ErpImportFileStatus.CompletedWithWarnings => result.Processed,
            _ => result.Failed
        };
    }
}
