// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for <see cref="DeleteQualificationCommand"/>. Soft-deletes the qualification entry;
/// the soft-delete timestamp and user are applied automatically by the DbContext save pipeline.
/// </summary>
/// <param name="repository">Provides access to the qualification store</param>
/// <param name="unitOfWork">Commits the change</param>

using Klacks.Api.Application.Commands.Qualifications;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Qualifications;

public sealed class DeleteQualificationCommandHandler : IRequestHandler<DeleteQualificationCommand>
{
    private readonly IQualificationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteQualificationCommandHandler(
        IQualificationRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteQualificationCommand request, CancellationToken cancellationToken)
    {
        await _repository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();
        return Unit.Value;
    }
}
