// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for storing an uploaded ERP order file under its drop point's bucket prefix. The
/// prefix comes from the trusted drop point record (resolved from the authenticated token's
/// claim, not from client input); the file name is reduced to its bare name component to rule
/// out path traversal into another customer's prefix via a crafted upload file name. The stored
/// file is registered in the erp_import_file ledger (Received) with the caller's channel and uploader.
/// </summary>
/// <param name="request">Contains the drop point id, the original file name, the file content stream,
/// the entry channel (token push or admin UI upload) and the uploader name</param>

using Klacks.Api.Application.Commands.Imports;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Imports;
using Klacks.Api.Application.Validation.Imports;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Imports;
using Klacks.Api.Domain.Services.Imports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Imports;

public class UploadErpOrderFileCommandHandler : IRequestHandler<UploadErpOrderFileCommand, string>
{
    private readonly IErpDropPointRepository _dropPointRepository;
    private readonly IObjectStorageService _objectStorageService;
    private readonly ErpImportFileRegistrar _registrar;

    public UploadErpOrderFileCommandHandler(
        IErpDropPointRepository dropPointRepository,
        IObjectStorageService objectStorageService,
        ErpImportFileRegistrar registrar)
    {
        _dropPointRepository = dropPointRepository;
        _objectStorageService = objectStorageService;
        _registrar = registrar;
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
            throw new InvalidRequestException(ErpOrderFileUploadValidator.EmptyFileNameError);
        }

        var key = ErpImportStorageKeys.BuildUploadKey(dropPoint.BucketPrefix, safeFileName);
        var storedObject = await _objectStorageService.UploadAsync(key, request.Content, cancellationToken);
        await _registrar.RegisterAsync(
            new ErpImportFileRegistration(dropPoint, key, safeFileName, storedObject, request.Channel, request.UploadedBy),
            cancellationToken);

        return key;
    }
}
