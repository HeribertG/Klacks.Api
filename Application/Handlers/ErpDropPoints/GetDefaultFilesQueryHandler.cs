// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for the file explorer listing of the default ERP drop point. Groups the stored
/// objects into pending, processed and error entries, derives display file names by stripping
/// the upload id prefix and joins the latest import exception reason onto each error entry.
/// A file left behind in the processing/ segment is listed as an error rather than as pending:
/// the import claimed it and did not finish, and no later run picks it up again, so calling it
/// pending would promise an import that is never going to happen.
/// </summary>
/// <param name="request">Marker query without parameters</param>
using Klacks.Api.Application.DTOs.ErpDropPoints;
using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Models.Imports;
using Klacks.Api.Domain.Services.Imports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ErpDropPoints;

public class GetDefaultFilesQueryHandler : IRequestHandler<GetDefaultFilesQuery, ErpDropPointFilesResource>
{
    private const string InterruptedImportReason = "The import claimed this file and did not finish. Re-upload it to import it again.";

    private readonly IErpDefaultDropPointProvider _defaultDropPointProvider;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IErpImportExceptionRepository _exceptionRepository;

    public GetDefaultFilesQueryHandler(
        IErpDefaultDropPointProvider defaultDropPointProvider,
        IObjectStorageService objectStorageService,
        IErpImportExceptionRepository exceptionRepository)
    {
        _defaultDropPointProvider = defaultDropPointProvider;
        _objectStorageService = objectStorageService;
        _exceptionRepository = exceptionRepository;
    }

    public async Task<ErpDropPointFilesResource> Handle(GetDefaultFilesQuery request, CancellationToken cancellationToken)
    {
        var dropPoint = await _defaultDropPointProvider.GetOrCreateDefaultAsync(cancellationToken);
        var prefix = ErpImportStorageKeys.NormalizePrefix(dropPoint.BucketPrefix);
        var processedPrefix = ErpImportStorageKeys.SegmentPrefix(prefix, ErpImportStorageKeys.ProcessedSegment);
        var errorPrefix = ErpImportStorageKeys.SegmentPrefix(prefix, ErpImportStorageKeys.ErrorSegment);
        var processingPrefix = ErpImportStorageKeys.SegmentPrefix(prefix, ErpImportStorageKeys.ProcessingSegment);

        var objects = await _objectStorageService.ListWithMetadataAsync(prefix, cancellationToken);

        var pending = new List<ErpDropPointFileResource>();
        var processed = new List<ErpDropPointFileResource>();
        var interrupted = new List<ErpDropPointFileResource>();
        var errorObjects = new List<(StorageObjectMetadata Metadata, string StoredFileName)>();

        foreach (var storageObject in objects)
        {
            if (storageObject.Key.StartsWith(errorPrefix, StringComparison.Ordinal))
            {
                errorObjects.Add((storageObject, storageObject.Key[errorPrefix.Length..]));
            }
            else if (storageObject.Key.StartsWith(processedPrefix, StringComparison.Ordinal))
            {
                processed.Add(ToResource(storageObject, storageObject.Key[processedPrefix.Length..]));
            }
            else if (storageObject.Key.StartsWith(processingPrefix, StringComparison.Ordinal))
            {
                var interruptedResource = ToResource(storageObject, storageObject.Key[processingPrefix.Length..]);
                interruptedResource.ErrorReason = InterruptedImportReason;
                interrupted.Add(interruptedResource);
            }
            else
            {
                pending.Add(ToResource(storageObject, storageObject.Key[prefix.Length..]));
            }
        }

        var error = await BuildErrorResourcesAsync(prefix, errorObjects, cancellationToken);
        error.AddRange(interrupted);

        return new ErpDropPointFilesResource
        {
            Pending = SortNewestFirst(pending),
            Processed = SortNewestFirst(processed),
            Error = SortNewestFirst(error)
        };
    }

    private async Task<List<ErpDropPointFileResource>> BuildErrorResourcesAsync(
        string prefix,
        List<(StorageObjectMetadata Metadata, string StoredFileName)> errorObjects,
        CancellationToken cancellationToken)
    {
        if (errorObjects.Count == 0)
        {
            return [];
        }

        var originalKeys = errorObjects
            .Select(errorObject => prefix + errorObject.StoredFileName)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var exceptions = await _exceptionRepository.GetByFileKeysAsync(originalKeys, cancellationToken);
        var latestReasonByFileKey = exceptions
            .GroupBy(exception => exception.FileKey, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.OrderByDescending(exception => exception.CreateTime).First().Reason,
                StringComparer.Ordinal);

        var resources = new List<ErpDropPointFileResource>();
        foreach (var (metadata, storedFileName) in errorObjects)
        {
            var resource = ToResource(metadata, storedFileName);
            resource.ErrorReason = latestReasonByFileKey.GetValueOrDefault(prefix + storedFileName);
            resources.Add(resource);
        }

        return resources;
    }

    private static ErpDropPointFileResource ToResource(StorageObjectMetadata metadata, string storedFileName)
    {
        return new ErpDropPointFileResource
        {
            Key = metadata.Key,
            FileName = ErpImportStorageKeys.ToDisplayFileName(storedFileName),
            SizeBytes = metadata.SizeBytes,
            LastModifiedUtc = metadata.LastModifiedUtc
        };
    }

    private static List<ErpDropPointFileResource> SortNewestFirst(List<ErpDropPointFileResource> resources)
    {
        return resources.OrderByDescending(resource => resource.LastModifiedUtc).ToList();
    }
}
