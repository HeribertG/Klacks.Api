// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for <see cref="SetShiftPreferenceCommand"/>. Upserts the single real (client, shift)
/// preference in place — never wipes the client's other preferences.
/// </summary>
/// <param name="repository">Resolves / adds the client-shift-preference row</param>
/// <param name="unitOfWork">Commits the change</param>

using Klacks.Api.Application.Commands.ClientShiftPreferences;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ClientShiftPreferences;

public sealed class SetShiftPreferenceCommandHandler : IRequestHandler<SetShiftPreferenceCommand, Guid>
{
    private readonly IClientShiftPreferenceRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SetShiftPreferenceCommandHandler(
        IClientShiftPreferenceRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(SetShiftPreferenceCommand request, CancellationToken cancellationToken)
    {
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
