// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes the override row for a format key (the partial unique index only spans active rows,
/// so a new override can be created for the same key afterwards).
/// </summary>
using Klacks.Api.Application.Commands.Exports;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Exports;

public class DeleteExportFormatOverrideCommandHandler : IRequestHandler<DeleteExportFormatOverrideCommand, bool>
{
    private readonly IExportFormatOverrideRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteExportFormatOverrideCommandHandler(IExportFormatOverrideRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteExportFormatOverrideCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByFormatKeyAsync(request.FormatKey, cancellationToken);
        if (entry is null)
        {
            return false;
        }

        await _repository.DeleteAsync(entry, cancellationToken);
        await _unitOfWork.CompleteAsync();
        return true;
    }
}
