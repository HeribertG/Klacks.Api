// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler that permanently removes a quarantined file from the default ERP drop point. Deletion
/// covers the error segment and the processing segment: a file the import claimed but never
/// finished is listed among the errors, and without a way to remove it those leftovers would pile
/// up forever, because a re-upload is stored under a fresh key rather than replacing them.
/// </summary>
/// <param name="request">Contains the full storage key of the file inside the error or processing segment</param>
using Klacks.Api.Application.Commands.Imports;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Services.Imports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Imports;

public class DeleteErpOrderFileCommandHandler : IRequestHandler<DeleteErpOrderFileCommand>
{
    public const string KeyOutsideDeletableSegmentError = "Key must reference a file inside the error or processing segment of the default drop point.";

    private readonly IErpDefaultDropPointProvider _defaultDropPointProvider;
    private readonly IObjectStorageService _objectStorageService;

    public DeleteErpOrderFileCommandHandler(
        IErpDefaultDropPointProvider defaultDropPointProvider,
        IObjectStorageService objectStorageService)
    {
        _defaultDropPointProvider = defaultDropPointProvider;
        _objectStorageService = objectStorageService;
    }

    public async Task<Unit> Handle(DeleteErpOrderFileCommand request, CancellationToken cancellationToken)
    {
        var dropPoint = await _defaultDropPointProvider.GetOrCreateDefaultAsync(cancellationToken);
        var prefix = ErpImportStorageKeys.NormalizePrefix(dropPoint.BucketPrefix);
        var errorPrefix = ErpImportStorageKeys.SegmentPrefix(prefix, ErpImportStorageKeys.ErrorSegment);
        var processingPrefix = ErpImportStorageKeys.SegmentPrefix(prefix, ErpImportStorageKeys.ProcessingSegment);

        var segmentPrefix = MatchingSegmentPrefix(request.Key, errorPrefix, processingPrefix);
        if (segmentPrefix == null)
        {
            throw new InvalidRequestException(KeyOutsideDeletableSegmentError);
        }

        var fileName = request.Key[segmentPrefix.Length..];
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidRequestException(KeyOutsideDeletableSegmentError);
        }

        await _objectStorageService.DeleteAsync(request.Key, cancellationToken);

        return Unit.Value;
    }

    private static string? MatchingSegmentPrefix(string? key, string errorPrefix, string processingPrefix)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        if (key.StartsWith(errorPrefix, StringComparison.Ordinal))
        {
            return errorPrefix;
        }

        return key.StartsWith(processingPrefix, StringComparison.Ordinal) ? processingPrefix : null;
    }
}
