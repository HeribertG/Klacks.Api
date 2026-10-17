// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for <see cref="SetShiftRequiredQualificationCommand"/>. Upserts the active
/// (shift, qualification) row via the repository and persists through the unit of work. When the shift carries no
/// row of its own yet, the rows it inherits (cut ancestor, plannable copy, sealed order) are first written as its own
/// rows, because under the nearest-wins rule the new row would otherwise silently replace them. With
/// KeepExistingLevelBinding a new row keeps the override of the row it replaces for that qualification (the inherited,
/// effective one), so "keep" never silently resets an inherited binding to "as the qualification".
/// </summary>
/// <param name="repository">Resolves / adds the shift-required-qualification row</param>
/// <param name="unitOfWork">Commits the change</param>
/// <param name="shiftVisibilityGuard">Refuses a group-restricted caller a shift they may not see, exactly like a missing
/// one (404 "Shift with ID ... not found"); for such a caller a missing shift now also answers 404 instead of a foreign-key
/// error</param>

using Klacks.Api.Application.Commands.Qualifications;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Shifts;
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
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;

    public SetShiftRequiredQualificationCommandHandler(
        IShiftRequiredQualificationRepository repository,
        IUnitOfWork unitOfWork,
        IShiftVisibilityGuard shiftVisibilityGuard)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _shiftVisibilityGuard = shiftVisibilityGuard;
    }

    public async Task<Guid> Handle(SetShiftRequiredQualificationCommand request, CancellationToken cancellationToken)
    {
        if (!await _shiftVisibilityGuard.IsVisibleAsync(request.ShiftId, cancellationToken))
        {
            throw new KeyNotFoundException(string.Format(ShiftVisibilityMessages.ShiftNotFound, request.ShiftId));
        }

        var existing = await _repository.GetActiveAsync(request.ShiftId, request.QualificationId, cancellationToken);
        if (existing != null)
        {
            existing.IsMandatory = request.IsMandatory;
            existing.MinLevel = request.MinLevel;
            if (!request.KeepExistingLevelBinding)
            {
                existing.LevelBindingOverride = request.LevelBindingOverride;
            }

            await _unitOfWork.CompleteAsync();
            return existing.Id;
        }

        var effective = await _repository.GetEffectiveByShiftIdsAsync([request.ShiftId], cancellationToken);
        var inheritedOverride = effective
            .FirstOrDefault(e => e.ShiftId == request.ShiftId && e.Requirement.QualificationId == request.QualificationId)
            ?.Requirement.LevelBindingOverride;
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
            LevelBindingOverride = request.KeepExistingLevelBinding ? inheritedOverride : request.LevelBindingOverride,
        };
        await _repository.Add(entity);
        await _unitOfWork.CompleteAsync();
        return entity.Id;
    }
}
