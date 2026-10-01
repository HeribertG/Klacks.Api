// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for <see cref="SetShiftPreferenceCommand"/>. Upserts the single real (client, shift)
/// preference in place — never wipes the client's other preferences. A client outside the caller's group
/// visibility is refused exactly like a client that does not exist; nothing is written.
/// </summary>
/// <param name="clientVisibilityGuard">Decides whether the calling user may write for the client</param>
/// <param name="repository">Resolves / adds the client-shift-preference row</param>
/// <param name="unitOfWork">Commits the change</param>

using Klacks.Api.Application.Commands.ClientShiftPreferences;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ClientShiftPreferences;

public sealed class SetShiftPreferenceCommandHandler : IRequestHandler<SetShiftPreferenceCommand, Guid>
{
    private readonly IClientShiftPreferenceRepository _repository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IUnitOfWork _unitOfWork;

    public SetShiftPreferenceCommandHandler(
        IClientShiftPreferenceRepository repository,
        IClientVisibilityGuard clientVisibilityGuard,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _clientVisibilityGuard = clientVisibilityGuard;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(SetShiftPreferenceCommand request, CancellationToken cancellationToken)
    {
        if (!await _clientVisibilityGuard.IsVisibleAsync(request.ClientId, cancellationToken))
        {
            throw new KeyNotFoundException($"Client with ID {request.ClientId} not found");
        }

        var existing = await _repository.GetByClientAndShiftAsync(request.ClientId, request.ShiftId, cancellationToken);
        if (existing != null)
        {
            existing.PreferenceType = request.PreferenceType;
            await _unitOfWork.CompleteAsync();
            return existing.Id;
        }

        var entity = new ClientShiftPreference
        {
            Id = Guid.NewGuid(),
            ClientId = request.ClientId,
            ShiftId = request.ShiftId,
            PreferenceType = request.PreferenceType,
            AnalyseToken = null,
        };
        await _repository.Add(entity);
        await _unitOfWork.CompleteAsync();
        return entity.Id;
    }
}
