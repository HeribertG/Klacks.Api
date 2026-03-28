// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handles bulk saving of client shift preferences by replacing all existing entries.
/// </summary>
using Klacks.Api.Application.Commands.ClientShiftPreferences;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ClientShiftPreferences;

public class SaveClientShiftPreferencesCommandHandler : BaseTransactionHandler,
    IRequestHandler<SaveClientShiftPreferencesCommand, List<ClientShiftPreferenceResource>>
{
    private readonly IClientShiftPreferenceRepository _repository;
    private readonly ClientShiftPreferenceMapper _mapper;

    public SaveClientShiftPreferencesCommandHandler(
        IClientShiftPreferenceRepository repository,
        ClientShiftPreferenceMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<SaveClientShiftPreferencesCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<List<ClientShiftPreferenceResource>> Handle(
        SaveClientShiftPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            await _repository.DeleteAllByClientIdAsync(request.ClientId, cancellationToken);
            await _unitOfWork.CompleteAsync();

            var entities = request.Preferences.Select(p =>
            {
                var entity = _mapper.ToEntity(p);
                entity.Id = Guid.NewGuid();
                entity.ClientId = request.ClientId;
                return entity;
            }).ToList();

            foreach (var entity in entities)
            {
                await _repository.Add(entity);
            }

            await _unitOfWork.CompleteAsync();

            var saved = await _repository.GetByClientIdAsync(request.ClientId, cancellationToken);
            return _mapper.ToResources(saved);
        },
        "saving client shift preferences",
        new { request.ClientId, Count = request.Preferences.Count });
    }
}
