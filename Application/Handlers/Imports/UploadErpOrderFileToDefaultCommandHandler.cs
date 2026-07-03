// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for storing an uploaded ERP order file in the default drop point's mailbox, creating
/// the drop point on first use. The file name is reduced to its bare name component to rule out
/// path traversal via a crafted upload file name. After a successful upload the import run is
/// triggered immediately, because this handler serves interactive uploads from the settings UI;
/// the token-authenticated ERP push endpoint stays schedule-driven on purpose.
/// </summary>
/// <param name="request">Contains the original file name and the file content stream</param>
using Klacks.Api.Application.Commands.Imports;
using Klacks.Api.Application.Validation.Imports;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Services.Imports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Imports;

public class UploadErpOrderFileToDefaultCommandHandler : IRequestHandler<UploadErpOrderFileToDefaultCommand, string>
{
    private readonly IErpDefaultDropPointProvider _defaultDropPointProvider;
    private readonly IObjectStorageService _objectStorageService;
    private readonly IMediator _mediator;

    public UploadErpOrderFileToDefaultCommandHandler(
        IErpDefaultDropPointProvider defaultDropPointProvider,
        IObjectStorageService objectStorageService,
        IMediator mediator)
    {
        _defaultDropPointProvider = defaultDropPointProvider;
        _objectStorageService = objectStorageService;
        _mediator = mediator;
    }

    public async Task<string> Handle(UploadErpOrderFileToDefaultCommand request, CancellationToken cancellationToken)
    {
        var dropPoint = await _defaultDropPointProvider.GetOrCreateDefaultAsync(cancellationToken);

        var safeFileName = Path.GetFileName(request.FileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            throw new InvalidRequestException(ErpOrderFileUploadValidator.EmptyFileNameError);
        }

        var key = ErpImportStorageKeys.BuildUploadKey(dropPoint.BucketPrefix, safeFileName);
        await _objectStorageService.UploadAsync(key, request.Content, cancellationToken);
        await _mediator.Send(new TriggerErpImportRunCommand(), cancellationToken);

        return key;
    }
}
