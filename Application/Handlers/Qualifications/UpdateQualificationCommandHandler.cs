// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for <see cref="UpdateQualificationCommand"/>. Loads the existing qualification by id,
/// applies updated fields, syncs the qualification_country junction rows, and persists the change.
/// </summary>
/// <param name="repository">Provides access to the qualification store (includes QualificationCountries)</param>
/// <param name="unitOfWork">Commits the change</param>

using Klacks.Api.Application.Commands.Qualifications;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Qualifications;

public sealed class UpdateQualificationCommandHandler : IRequestHandler<UpdateQualificationCommand, Qualification>
{
    private readonly IQualificationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateQualificationCommandHandler(
        IQualificationRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Qualification> Handle(UpdateQualificationCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.Get(request.Id)
            ?? throw new KeyNotFoundException($"Qualification {request.Id} not found.");

        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Emoji = request.Emoji;
        entity.IsTimeLimited = request.IsTimeLimited;
        entity.Type = request.Type;
        entity.Category = request.Category;

        entity.QualificationCountries.Clear();
        foreach (var code in request.Countries.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct())
        {
            entity.QualificationCountries.Add(new QualificationCountry
            {
                QualificationId = entity.Id,
                CountryCode = code.Trim().ToUpperInvariant()
            });
        }

        await _unitOfWork.CompleteAsync();
        return entity;
    }
}
