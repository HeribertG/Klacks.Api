// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handles bulk saving of client shift preferences by replacing all existing entries. A client outside the
/// caller's group visibility is refused exactly like a client that does not exist; nothing is deleted or written.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may write for the client</param>
using Klacks.Api.Application.Interfaces;
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
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly ClientShiftPreferenceMapper _mapper;

    public SaveClientShiftPreferencesCommandHandler(
        IClientShiftPreferenceRepository repository,
        IClientVisibilityGuard clientVisibilityGuard,
        ClientShiftPreferenceMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<SaveClientShiftPreferencesCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _repository = repository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _mapper = mapper;
    }

    public async Task<List<ClientShiftPreferenceResource>> Handle(
        SaveClientShiftPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithTransactionAsync(async () =>
        {
            if (!await _clientVisibilityGuard.IsVisibleAsync(request.ClientId, cancellationToken))
            {
                throw new KeyNotFoundException($"Client with ID {request.ClientId} not found");
            }

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
