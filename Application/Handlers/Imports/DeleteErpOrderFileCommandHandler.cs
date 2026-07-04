// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler that permanently removes a quarantined file from the error segment of the default
/// ERP drop point.
/// </summary>
/// <param name="request">Contains the full storage key of the file inside the error segment</param>
using Klacks.Api.Application.Commands.Imports;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Services.Imports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Imports;

public class DeleteErpOrderFileCommandHandler : IRequestHandler<DeleteErpOrderFileCommand>
{
    public const string KeyOutsideErrorSegmentError = "Key must reference a file inside the error segment of the default drop point.";

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

        if (string.IsNullOrWhiteSpace(request.Key) || !request.Key.StartsWith(errorPrefix, StringComparison.Ordinal))
        {
            throw new InvalidRequestException(KeyOutsideErrorSegmentError);
        }

        var fileName = request.Key[errorPrefix.Length..];
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidRequestException(KeyOutsideErrorSegmentError);
        }

        await _objectStorageService.DeleteAsync(request.Key, cancellationToken);

        return Unit.Value;
    }
}
