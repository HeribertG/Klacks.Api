// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for <see cref="SetShiftRequiredQualificationCommand"/>. Upserts the active
/// (shift, qualification) row via the repository and persists through the unit of work. When the shift carries no
/// row of its own yet, the rows it inherits (cut ancestor, plannable copy, sealed order) are first written as its own
/// rows, because under the nearest-wins rule the new row would otherwise silently replace them.
/// </summary>
/// <param name="repository">Resolves / adds the shift-required-qualification row</param>
/// <param name="unitOfWork">Commits the change</param>

using Klacks.Api.Application.Commands.Qualifications;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Qualifications;

public sealed class SetShiftRequiredQualificationCommandHandler : IRequestHandler<SetShiftRequiredQualificationCommand, Guid>
{
    private readonly IShiftRequiredQualificationRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SetShiftRequiredQualificationCommandHandler(
        IShiftRequiredQualificationRepository repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(SetShiftRequiredQualificationCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetActiveAsync(request.ShiftId, request.QualificationId, cancellationToken);
        if (existing != null)
        {
            existing.IsMandatory = request.IsMandatory;
            existing.MinLevel = request.MinLevel;
            await _unitOfWork.CompleteAsync();
            return existing.Id;
        }

        var effective = await _repository.GetEffectiveByShiftIdsAsync([request.ShiftId], cancellationToken);
        var inherited = ShiftRequirementMaterializer.InheritedAsOwn(effective, request.ShiftId)
            .Where(row => row.QualificationId != request.QualificationId);
        foreach (var row in inherited)
        {
            await _repository.Add(row);
        }

        var entity = new ShiftRequiredQualification
        {
            Id = Guid.NewGuid(),
            ShiftId = request.ShiftId,
            QualificationId = request.QualificationId,
            IsMandatory = request.IsMandatory,
            MinLevel = request.MinLevel,
        };
        await _repository.Add(entity);
        await _unitOfWork.CompleteAsync();
        return entity.Id;
    }
}
