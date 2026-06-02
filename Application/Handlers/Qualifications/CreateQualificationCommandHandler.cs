// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for <see cref="CreateQualificationCommand"/>. Returns the id of an existing same-named
/// qualification when present (no duplicate), otherwise adds a new one and persists it.
/// </summary>
/// <param name="repository">Resolves an existing qualification by name / adds a new one</param>
/// <param name="unitOfWork">Commits the change</param>

using Klacks.Api.Application.Commands.Qualifications;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Qualifications;

public sealed class CreateQualificationCommandHandler : IRequestHandler<CreateQualificationCommand, Guid>
{
    private readonly IQualificationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateQualificationCommandHandler(
        IQualificationRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateQualificationCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetByNameAsync(request.Name?.De ?? string.Empty, cancellationToken);
        if (existing != null)
        {
            return existing.Id;
        }

        var entity = new Qualification
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
        };
        await _repository.Add(entity);
        await _unitOfWork.CompleteAsync();
        return entity.Id;
    }
}
