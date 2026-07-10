// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Upserts the override row for a format key after validating the patch against the family's key
/// whitelist, and stamps the current app version so a later release can be flagged as potentially
/// making the override obsolete.
/// </summary>
using Klacks.Api.Application.Commands.Exports;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Exports;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Application.Services.Exports;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Exports;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Exports;

public class SaveExportFormatOverrideCommandHandler : IRequestHandler<SaveExportFormatOverrideCommand, ExportFormatOverrideResource>
{
    private readonly IExportFormatFamilyResolver _familyResolver;
    private readonly IExportFormatOverrideRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SaveExportFormatOverrideCommandHandler(
        IExportFormatFamilyResolver familyResolver,
        IExportFormatOverrideRepository repository,
        IUnitOfWork unitOfWork)
    {
        _familyResolver = familyResolver;
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ExportFormatOverrideResource> Handle(SaveExportFormatOverrideCommand request, CancellationToken cancellationToken)
    {
        if (!_familyResolver.TryResolve(request.FormatKey, out var family))
        {
            throw new InvalidRequestException($"Unknown export format: {request.FormatKey}");
        }

        var errors = ExportFormatOverridePatch.Validate(request.PatchJson, family);
        if (errors.Count > 0)
        {
            throw new InvalidRequestException(string.Join(" ", errors));
        }

        var entry = await _repository.GetByFormatKeyAsync(request.FormatKey, cancellationToken);
        if (entry is null)
        {
            entry = new ExportFormatOverride { FormatKey = request.FormatKey };
            await _repository.AddAsync(entry, cancellationToken);
        }

        entry.PatchJson = request.PatchJson;
        entry.IsEnabled = request.IsEnabled;
        entry.Note = request.Note;
        entry.CreatedUnderVersion = $"{MyVersion.Major}.{MyVersion.Minor}.{MyVersion.Patch}";

        await _unitOfWork.CompleteAsync();

        return ListExportFormatOverridesQueryHandler.ToResource(entry);
    }
}
