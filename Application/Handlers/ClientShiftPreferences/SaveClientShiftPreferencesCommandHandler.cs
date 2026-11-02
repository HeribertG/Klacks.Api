// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handles bulk saving of client shift preferences by replacing all existing real entries; the preference clones of a
/// scenario stay. A client outside the caller's group visibility is refused exactly like a client that does not exist. A
/// shift the client does not prefer yet is refused with the not-found text of a shift when it is missing, hidden or carries
/// a token, and nothing is deleted or written. A stored shift is not checked again, because the dialog sends back what it
/// loaded and a client may prefer a shift the caller cannot see.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may write for the client</param>
/// <param name="shiftVisibilityGuard">Finds a shift a real preference may not point at</param>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Commands.ClientShiftPreferences;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Services.Shifts;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ClientShiftPreferences;

public class SaveClientShiftPreferencesCommandHandler : BaseTransactionHandler,
    IRequestHandler<SaveClientShiftPreferencesCommand, List<ClientShiftPreferenceResource>>
{
    private readonly IClientShiftPreferenceRepository _repository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly ClientShiftPreferenceMapper _mapper;

    public SaveClientShiftPreferencesCommandHandler(
        IClientShiftPreferenceRepository repository,
        IClientVisibilityGuard clientVisibilityGuard,
        IShiftVisibilityGuard shiftVisibilityGuard,
        ClientShiftPreferenceMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<SaveClientShiftPreferencesCommandHandler> logger)
        : base(unitOfWork, logger)
    {
        _repository = repository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _shiftVisibilityGuard = shiftVisibilityGuard;
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

            var stored = await _repository.GetByClientIdAsync(request.ClientId, cancellationToken);
            var newShiftIds = request.Preferences.Select(p => p.ShiftId).Except(stored.Select(p => p.ShiftId)).ToList();
            if (await _shiftVisibilityGuard.FindFirstNotReferableAsync(newShiftIds, cancellationToken) is { } refusedShiftId)
            {
                throw new KeyNotFoundException(string.Format(ShiftVisibilityMessages.ShiftNotFound, refusedShiftId));
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
