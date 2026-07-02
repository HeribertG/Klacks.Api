// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for storing an uploaded ERP order file under its drop point's bucket prefix. The
/// prefix comes from the trusted drop point record (resolved from the authenticated token's
/// claim, not from client input); the file name is reduced to its bare name component to rule
/// out path traversal into another customer's prefix via a crafted upload file name.
/// </summary>
/// <param name="request">Contains the drop point id, the original file name and the file content stream</param>

using Klacks.Api.Application.Commands.Imports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Imports;

public class UploadErpOrderFileCommandHandler : IRequestHandler<UploadErpOrderFileCommand, string>
{
    private readonly IErpDropPointRepository _dropPointRepository;
    private readonly IObjectStorageService _objectStorageService;

    public UploadErpOrderFileCommandHandler(IErpDropPointRepository dropPointRepository, IObjectStorageService objectStorageService)
    {
        _dropPointRepository = dropPointRepository;
        _objectStorageService = objectStorageService;
    }

    public async Task<string> Handle(UploadErpOrderFileCommand request, CancellationToken cancellationToken)
    {
        var dropPoint = await _dropPointRepository.GetNoTracking(request.DropPointId);
        if (dropPoint == null || !dropPoint.IsEnabled)
        {
            throw new InvalidRequestException("Drop point is not available.");
        }

        var safeFileName = Path.GetFileName(request.FileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            throw new InvalidRequestException("File name must not be empty.");
        }

        var key = $"{dropPoint.BucketPrefix.TrimEnd('/')}/{Guid.NewGuid():N}-{safeFileName}";
        await _objectStorageService.UploadAsync(key, request.Content, cancellationToken);

        return key;
    }
}
