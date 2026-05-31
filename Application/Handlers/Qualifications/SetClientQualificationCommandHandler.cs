// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for <see cref="SetClientQualificationCommand"/>. Upserts the active (client, qualification)
/// row via the repository and persists through the unit of work.
/// </summary>
/// <param name="repository">Resolves / adds the client-qualification row</param>
/// <param name="unitOfWork">Commits the change</param>

using Klacks.Api.Application.Commands.Qualifications;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Qualifications;

public sealed class SetClientQualificationCommandHandler : IRequestHandler<SetClientQualificationCommand, Guid>
{
    private readonly IClientQualificationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SetClientQualificationCommandHandler(
        IClientQualificationRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(SetClientQualificationCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetActiveAsync(request.ClientId, request.QualificationId, cancellationToken);
        if (existing != null)
        {
            existing.Level = request.Level;
            existing.ValidFrom = request.ValidFrom;
            existing.ValidUntil = request.ValidUntil;
            existing.Note = request.Note;
            await _unitOfWork.CompleteAsync();
            return existing.Id;
        }

        var entity = new ClientQualification
        {
            Id = Guid.NewGuid(),
            ClientId = request.ClientId,
            QualificationId = request.QualificationId,
            Level = request.Level,
            ValidFrom = request.ValidFrom,
            ValidUntil = request.ValidUntil,
            Note = request.Note,
        };
        await _repository.Add(entity);
        await _unitOfWork.CompleteAsync();
        return entity.Id;
    }
}
